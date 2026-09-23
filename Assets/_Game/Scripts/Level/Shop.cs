using UnityEngine;

/// <summary>
/// Loja no mundo: o raposo vendedor. Quando o jogador entra na area, mostra o
/// aviso da tecla E flutuando; ao apertar, abre a ShopUI.
/// </summary>
public class Shop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer vendedor;
    [SerializeField] private SpriteRenderer aviso;
    [Tooltip("Keycap mostrado quando o jogador esta no teclado.")]
    [SerializeField] private Sprite avisoTeclado;
    [Tooltip("Botao mostrado quando o jogador esta no controle.")]
    [SerializeField] private Sprite avisoControle;
    [Tooltip("O desenho original do vendedor olha para a direita?")]
    [SerializeField] private bool vendedorOlhaParaDireita = true;

    private Transform jogador;
    private Vector3 posicaoBaseDoAviso;

    private void Awake()
    {
        if (aviso != null)
        {
            posicaoBaseDoAviso = aviso.transform.localPosition;
            aviso.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        jogador = outro.transform;
        if (aviso != null) aviso.enabled = true;
    }

    private void OnTriggerExit2D(Collider2D outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        jogador = null;
        if (aviso != null) aviso.enabled = false;
    }

    private void Update()
    {
        if (jogador == null)
            return;

        // O vendedor vira para o jogador.
        if (vendedor != null)
        {
            bool jogadorADireita = jogador.position.x > transform.position.x;
            vendedor.flipX = vendedorOlhaParaDireita ? !jogadorADireita : jogadorADireita;
        }

        if (aviso != null)
        {
            aviso.transform.localPosition = posicaoBaseDoAviso + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.12f;

            // Mostra a tecla ou o botao do controle, conforme o que o jogador usa.
            Sprite certo = GameInput.UsandoControle ? avisoControle : avisoTeclado;
            if (certo != null && aviso.sprite != certo)
                aviso.sprite = certo;
        }

        if (!ShopUI.Aberta && ApertouInteragir())
            ShopUI.Abrir();
    }

    /// <summary>E / Enter no teclado, Y no controle.</summary>
    public static bool ApertouInteragir() => GameInput.InteragiuAgora;
}
