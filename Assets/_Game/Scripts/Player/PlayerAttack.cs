using System.Collections;
using UnityEngine;

/// <summary>
/// Golpe de espada. L (ou J / botao esquerdo do mouse). No chao trava o
/// movimento por um instante (o golpe tem peso); no ar usa o ataque aereo.
///
/// O acerto nao usa um colisor: no meio da animacao, procura inimigos numa
/// caixa na frente do jogador. E mais previsivel que colisor de espada e nao
/// depende do formato de cada quadro.
/// </summary>
[RequireComponent(typeof(PlayerController2D))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Tempo")]
    [Tooltip("Intervalo minimo entre dois golpes.")]
    [SerializeField] private float duracao = 0.42f;
    [Tooltip("Quanto tempo depois de apertar o golpe realmente acerta (a espada precisa 'chegar').")]
    [SerializeField] private float atrasoDoGolpe = 0.12f;

    [Header("Alcance")]
    [SerializeField] private Vector2 tamanhoDaCaixa = new Vector2(1.6f, 1.5f);
    [Tooltip("Centro da caixa em relacao ao jogador, olhando para a direita.")]
    [SerializeField] private Vector2 deslocamentoDaCaixa = new Vector2(1.0f, -0.1f);
    [SerializeField] private LayerMask camadasAtingiveis;
    [SerializeField] private int dano = 1;

    private PlayerController2D controlador;
    private Animator animator;
    private float proximoGolpe;

    private void Awake()
    {
        controlador = GetComponent<PlayerController2D>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (!controlador.ControleAtivo || Time.time < proximoGolpe)
            return;

        if (ApertouAtaque())
            Atacar();
    }

    /// <summary>L / J no teclado, X ou Y no controle, ou o botao esquerdo do mouse.</summary>
    public static bool ApertouAtaque() => GameInput.AtacouAgora;

    private void Atacar()
    {
        proximoGolpe = Time.time + duracao;
        bool noChao = controlador.NoChao;

        if (animator != null)
            animator.SetTrigger(noChao ? "Atacar" : "AtaqueAereo");

        // Parado enquanto golpeia no chao; no ar mantem o controle.
        if (noChao)
            controlador.TravarMovimento(duracao * 0.75f);

        AudioManager.Sfx(RetroSfx.Espada);
        StartCoroutine(Golpear());
    }

    private IEnumerator Golpear()
    {
        yield return new WaitForSeconds(atrasoDoGolpe);

        Vector2 centro = CentroDaCaixa();
        Collider2D[] atingidos = Physics2D.OverlapBoxAll(centro, tamanhoDaCaixa, 0f, camadasAtingiveis);
        bool acertouAlguem = false;

        foreach (Collider2D col in atingidos)
        {
            EnemyDamage inimigo = col.GetComponentInParent<EnemyDamage>();
            if (inimigo != null)
            {
                acertouAlguem |= inimigo.LevarGolpe();
                continue;
            }

            Boss chefe = col.GetComponentInParent<Boss>();
            if (chefe != null)
            {
                acertouAlguem |= chefe.LevarGolpe(dano, transform.position);
                continue;
            }

            // A espada tambem rebate a bola de fogo do chefe.
            Projetil projetil = col.GetComponentInParent<Projetil>();
            if (projetil != null)
            {
                Destroy(projetil.gameObject);
                acertouAlguem = true;
            }
        }

        if (acertouAlguem)
            AudioManager.Sfx(RetroSfx.Golpe);
    }

    private Vector2 CentroDaCaixa()
    {
        float lado = controlador.OlhandoParaDireita ? 1f : -1f;
        return (Vector2)transform.position + new Vector2(deslocamentoDaCaixa.x * lado, deslocamentoDaCaixa.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (controlador == null)
            controlador = GetComponent<PlayerController2D>();

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(CentroDaCaixa(), tamanhoDaCaixa);
    }
}
