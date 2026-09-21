using System.Collections;
using UnityEngine;

/// <summary>
/// Chefe de fim de fase. Fica dormindo ate a BossArena ativa-lo; entao persegue
/// o jogador e alterna entre investir, pular e (se tiver projetil) atirar.
/// Morre com alguns pisoes ou golpes de espada e libera o fim da fase.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Boss : MonoBehaviour
{
    [Header("Identidade")]
    [SerializeField] private string nome = "Chefe";

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 3;
    [Tooltip("Depois de levar um golpe, fica um tempo sem poder ser atingido de novo.")]
    [SerializeField] private float tempoInvencivel = 1f;

    [Header("Movimento")]
    [SerializeField] private float velocidade = 3f;
    [SerializeField] private float velocidadeInvestida = 9f;
    [SerializeField] private float duracaoInvestida = 1.1f;
    [SerializeField] private float forcaPulo = 16f;
    [Tooltip("Segundos entre uma acao especial e a proxima, com a vida cheia. Diminui conforme apanha.")]
    [SerializeField] private float intervaloEntreAcoes = 2.4f;
    [SerializeField] private bool spriteOlhaParaEsquerda = true;
    [SerializeField] private LayerMask camadaChao;

    [Header("Ataque a distancia (opcional)")]
    [SerializeField] private GameObject projetil;
    [SerializeField] private float velocidadeDoProjetil = 9f;
    [Tooltip("De onde o tiro sai, em relacao ao centro, olhando para a direita.")]
    [SerializeField] private Vector2 bocaDoTiro = new Vector2(1.4f, 0.2f);

    [Header("Dano ao jogador")]
    [SerializeField] private int dano = 1;
    [Tooltip("O quanto o jogador precisa estar acima do centro do chefe para valer como pisao.")]
    [SerializeField] private float alturaMinimaDoPisao = 0.8f;
    [SerializeField] private float forcaDoQuique = 16f;

    [Header("Ao morrer")]
    [SerializeField] private GameObject efeitoMorte;
    [Tooltip("Objeto que aparece quando o chefe morre: o trofeu de fim de fase.")]
    [SerializeField] private GameObject liberarAoMorrer;
    [Tooltip("Objeto que some quando o chefe morre: a parede que fecha a arena.")]
    [SerializeField] private GameObject desativarAoMorrer;

    private Rigidbody2D rb;
    private Collider2D colisor;
    private SpriteRenderer sprite;
    private Animator animator;
    private Transform jogador;

    private bool ativo;
    private bool morto;
    private bool invencivel;
    private bool investindo;
    private int direcao = -1;
    private float proximaAcao;
    private float fimDaInvestida;

    public string Nome => nome;
    public int VidaAtual { get; private set; }
    public int VidaMaxima => vidaMaxima;

    /// <summary>(vida atual, vida maxima) - a barra da HUD escuta isto.</summary>
    public event System.Action<int, int> OnVidaMudou;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        rb.freezeRotation = true;
        VidaAtual = vidaMaxima;
        AtualizarSprite();
    }

    /// <summary>Chamado pela BossArena quando o jogador entra na arena.</summary>
    public void Ativar()
    {
        if (ativo || morto)
            return;

        ativo = true;
        proximaAcao = Time.time + 1.2f; // um respiro antes da primeira investida
        BossHealthBar.Mostrar(this);
    }

    private void FixedUpdate()
    {
        if (!ativo || morto)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (jogador == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            jogador = go.transform;
        }

        bool noChao = NoChao();

        if (investindo)
        {
            // Termina a investida no tempo ou ao bater numa parede.
            if (Time.time >= fimDaInvestida || ParedeAFrente())
                investindo = false;
            else
                rb.linearVelocity = new Vector2(direcao * velocidadeInvestida, rb.linearVelocity.y);

            return;
        }

        direcao = jogador.position.x >= transform.position.x ? 1 : -1;
        AtualizarSprite();

        if (!noChao)
            return;

        if (Time.time >= proximaAcao)
        {
            EscolherAcao();
            return;
        }

        // Entre acoes, caminha na direcao do jogador, mas para bem perto para
        // nao ficar "colado" empurrando.
        float distancia = Mathf.Abs(jogador.position.x - transform.position.x);
        float vx = distancia > 1.5f ? direcao * velocidade : 0f;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        if (animator != null)
            animator.SetFloat("Velocidade", Mathf.Abs(vx));
    }

    private void EscolherAcao()
    {
        // Quanto menos vida, mais frequente: 100% do intervalo com vida cheia,
        // 55% com um ponto de vida.
        float fator = Mathf.Lerp(0.55f, 1f, (VidaAtual - 1f) / Mathf.Max(1, vidaMaxima - 1));
        proximaAcao = Time.time + intervaloEntreAcoes * fator;

        int opcoes = projetil != null ? 3 : 2;
        int sorteio = Random.Range(0, opcoes);

        switch (sorteio)
        {
            case 0: Investir(); break;
            case 1: Pular(); break;
            default: Atirar(); break;
        }
    }

    private void Investir()
    {
        investindo = true;
        fimDaInvestida = Time.time + duracaoInvestida;
        rb.linearVelocity = new Vector2(direcao * velocidadeInvestida, rb.linearVelocity.y);

        if (animator != null)
            animator.SetFloat("Velocidade", velocidadeInvestida);
    }

    private void Pular()
    {
        rb.linearVelocity = new Vector2(direcao * velocidade * 1.6f, forcaPulo);
    }

    private void Atirar()
    {
        if (projetil == null || jogador == null)
        {
            Investir();
            return;
        }

        Vector3 origem = transform.position + new Vector3(bocaDoTiro.x * direcao, bocaDoTiro.y, 0f);
        GameObject tiro = Instantiate(projetil, origem, Quaternion.identity);

        Vector2 alvo = (Vector2)jogador.position + Vector2.up * 0.5f;
        Vector2 rumo = (alvo - (Vector2)origem).normalized;

        Projetil script = tiro.GetComponent<Projetil>();
        if (script != null)
            script.Lancar(rumo * velocidadeDoProjetil);

        if (animator != null)
            animator.SetTrigger("Atacar");
    }

    // ----------------- Contato com o jogador -----------------

    private void OnCollisionEnter2D(Collision2D colisao) => TratarContato(colisao.collider);
    private void OnCollisionStay2D(Collision2D colisao) => TratarContato(colisao.collider);

    private void TratarContato(Collider2D outro)
    {
        if (morto || !outro.CompareTag("Player"))
            return;

        PlayerHealth vida = outro.GetComponent<PlayerHealth>();
        if (vida == null)
            return;

        Rigidbody2D rbJogador = outro.attachedRigidbody;
        bool vindoDeCima = rbJogador != null && rbJogador.linearVelocity.y < 0f;
        bool acimaDoChefe = outro.transform.position.y > transform.position.y + alturaMinimaDoPisao;

        if (vindoDeCima && acimaDoChefe)
        {
            PlayerController2D controlador = outro.GetComponent<PlayerController2D>();
            if (controlador != null)
                controlador.Quicar(forcaDoQuique);

            LevarGolpe(1, outro.transform.position);
            return;
        }

        vida.TomarDano(dano, transform.position);
    }

    /// <summary>Tira vida do chefe. Devolve false se o golpe nao contou (invencivel ou ja morto).</summary>
    public bool LevarGolpe(int quantidade, Vector3 origem)
    {
        if (morto || invencivel || !ativo)
            return false;

        VidaAtual = Mathf.Max(0, VidaAtual - quantidade);
        OnVidaMudou?.Invoke(VidaAtual, vidaMaxima);
        AudioManager.Sfx(RetroSfx.ChefeDano);

        if (VidaAtual <= 0)
        {
            Morrer();
            return true;
        }

        // Recua um pouco para o lado oposto de quem bateu e fica invencivel.
        float lado = transform.position.x < origem.x ? -1f : 1f;
        rb.linearVelocity = new Vector2(lado * 5f, 6f);
        investindo = false;
        proximaAcao = Time.time + 0.8f;

        StartCoroutine(FicarInvencivel());
        return true;
    }

    private IEnumerator FicarInvencivel()
    {
        invencivel = true;
        float restante = tempoInvencivel;

        while (restante > 0f)
        {
            if (sprite != null) sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(0.08f);
            restante -= 0.08f;
        }

        if (sprite != null) sprite.enabled = true;
        invencivel = false;
    }

    private void Morrer()
    {
        morto = true;
        AudioManager.Sfx(RetroSfx.ChefeMorte);

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (colisor != null) colisor.enabled = false;

        BossHealthBar.Esconder();

        if (desativarAoMorrer != null)
            desativarAoMorrer.SetActive(false);

        StartCoroutine(Explodir());
    }

    private IEnumerator Explodir()
    {
        // Varias explosoes espalhadas pelo corpo, e so depois o trofeu aparece.
        Bounds b = sprite != null ? sprite.bounds : new Bounds(transform.position, Vector3.one);

        for (int i = 0; i < 6; i++)
        {
            if (efeitoMorte != null)
            {
                Vector3 pos = new Vector3(
                    Random.Range(b.min.x, b.max.x),
                    Random.Range(b.min.y, b.max.y), 0f);
                Instantiate(efeitoMorte, pos, Quaternion.identity);
            }

            if (sprite != null) sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(0.15f);
        }

        if (sprite != null) sprite.enabled = false;

        if (liberarAoMorrer != null)
            liberarAoMorrer.SetActive(true);

        AudioManager.Sfx(RetroSfx.VidaExtra);
        Destroy(gameObject, 0.2f);
    }

    // ----------------- Sensores -----------------

    private bool NoChao()
    {
        if (colisor == null) return true;
        Vector2 pe = new Vector2(colisor.bounds.center.x, colisor.bounds.min.y);
        return Physics2D.OverlapCircle(pe + Vector2.down * 0.05f, 0.2f, camadaChao);
    }

    private bool ParedeAFrente()
    {
        if (colisor == null) return false;
        Vector2 origem = new Vector2(colisor.bounds.center.x, colisor.bounds.center.y);
        float alcance = colisor.bounds.extents.x + 0.2f;
        return Physics2D.Raycast(origem, Vector2.right * direcao, alcance, camadaChao);
    }

    private void AtualizarSprite()
    {
        if (sprite == null) return;
        sprite.flipX = spriteOlhaParaEsquerda ? direcao > 0 : direcao < 0;
    }
}
