using UnityEngine;

/// <summary>
/// Movimento e pulo do jogador.
///
/// Inclui dois ajustes classicos de jogo de plataforma:
/// - Coyote time: o jogador ainda consegue pular por alguns milissegundos
///   depois de sair da borda da plataforma.
/// - Jump buffer: se o botao for apertado um pouco antes de tocar o chao,
///   o pulo acontece assim que ele aterrissa.
/// Sem esses dois, o controle "engasga" e parece que o jogo ignorou o comando.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Unidades por segundo. Como um tile mede uma unidade, 8 equivale a 8 tiles por segundo.")]
    [SerializeField] private float velocidade = 8f;
    [Tooltip("Quanto maior, mais 'escorregadio'. 0 = resposta instantanea.")]
    [SerializeField] private float suavizacaoMovimento = 0.05f;

    [Header("Pulo")]
    [Tooltip("Com gravidade 4 no Rigidbody, 17 sobe cerca de 3,7 tiles: quase o dobro da altura do jogador.")]
    [SerializeField] private float forcaPulo = 17f;
    [Tooltip("Tempo extra para pular depois de sair do chao.")]
    [SerializeField] private float coyoteTime = 0.12f;
    [Tooltip("Tempo que o comando de pulo fica guardado antes de tocar o chao.")]
    [SerializeField] private float jumpBuffer = 0.12f;
    [Tooltip("Quanto o pulo e cortado ao soltar o botao (0 = corta tudo, 1 = nao corta).")]
    [Range(0f, 1f)]
    [SerializeField] private float corteDoPulo = 0.45f;
    [Tooltip("Quantidade total de pulos. 2 libera o pulo duplo.")]
    [SerializeField] private int pulosMaximos = 1;

    [Header("Deteccao de chao")]
    [Tooltip("Objeto vazio posicionado nos pes do jogador.")]
    [SerializeField] private Transform checagemChao;
    [SerializeField] private float raioChecagem = 0.15f;
    [SerializeField] private LayerMask camadaChao;

    [Header("Queda (deixa o pulo com peso)")]
    [Tooltip("Multiplica a gravidade enquanto o jogador esta caindo.")]
    [SerializeField] private float multiplicadorQueda = 2.2f;

    private Rigidbody2D rb;
    private Animator animator;

    private float entradaHorizontal;
    private float velocidadeSuavizada;
    private float contadorCoyote;
    private float contadorBuffer;
    private int pulosRestantes;
    private bool soltouBotaoPulo;
    private bool controleAtivo = true;
    private float escalaOriginal;
    private float movimentoTravadoAte;

    public bool NoChao { get; private set; }
    public bool OlhandoParaDireita { get; private set; } = true;
    public bool ControleAtivo => controleAtivo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        // Trava a rotacao: sem isso o jogador tomba ao encostar em quinas.
        rb.freezeRotation = true;
        pulosRestantes = pulosMaximos;
        escalaOriginal = Mathf.Abs(transform.localScale.x);
    }

    private void Update()
    {
        if (!controleAtivo)
        {
            entradaHorizontal = 0f;
            AtualizarAnimacao();
            return;
        }

        entradaHorizontal = Input.GetAxisRaw("Horizontal");

        // Durante o golpe no chao o jogador fica plantado (ainda pode pular).
        if (Time.time < movimentoTravadoAte)
            entradaHorizontal = 0f;

        if (Input.GetButtonDown("Jump"))
            contadorBuffer = jumpBuffer;
        else
            contadorBuffer -= Time.deltaTime;

        // Soltar o botao no meio do pulo faz o salto ser mais curto.
        if (Input.GetButtonUp("Jump"))
            soltouBotaoPulo = true;

        Virar();
        AtualizarAnimacao();
    }

    private void FixedUpdate()
    {
        ChecarChao();
        Mover();
        Pular();
        AplicarPesoNaQueda();
    }

    // ----------------- Movimento -----------------

    private void Mover()
    {
        float alvoX = entradaHorizontal * velocidade;

        // SmoothDamp evita que o jogador saia e pare no mesmo frame.
        float novoX = Mathf.SmoothDamp(
            rb.linearVelocity.x,
            alvoX,
            ref velocidadeSuavizada,
            suavizacaoMovimento);

        rb.linearVelocity = new Vector2(novoX, rb.linearVelocity.y);
    }

    private void ChecarChao()
    {
        NoChao = checagemChao != null
            && Physics2D.OverlapCircle(checagemChao.position, raioChecagem, camadaChao);

        if (NoChao)
        {
            contadorCoyote = coyoteTime;

            // So recarrega os pulos quando NAO esta subindo. Logo apos pular, o
            // circulo de checagem ainda toca o chao por 1 ou 2 frames; sem esta
            // condicao o jogador recuperaria o pulo duplo e voaria.
            if (rb.linearVelocity.y <= 0.01f)
                pulosRestantes = pulosMaximos;

            return;
        }

        contadorCoyote -= Time.fixedDeltaTime;

        // Andou para fora da plataforma sem pular: quando o coyote time acaba,
        // o pulo "do chao" e consumido, senao ele ganharia um salto de graca.
        if (contadorCoyote <= 0f && pulosRestantes == pulosMaximos)
            pulosRestantes = pulosMaximos - 1;
    }

    private void Pular()
    {
        bool podePularDoChao = contadorCoyote > 0f;
        bool podePular = podePularDoChao || pulosRestantes > 0;

        if (contadorBuffer > 0f && podePular)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);

            pulosRestantes = Mathf.Max(0, pulosRestantes - 1);
            contadorBuffer = 0f;
            contadorCoyote = 0f;
            soltouBotaoPulo = false;

            // Pulo no ar ganha a cambalhota; pulo do chao, o salto normal.
            if (podePularDoChao)
            {
                AudioManager.Sfx(RetroSfx.Pulo);
                if (animator != null) animator.SetTrigger("Pular");
            }
            else
            {
                AudioManager.Sfx(RetroSfx.PuloDuplo);
                if (animator != null) animator.SetTrigger("PuloDuplo");
            }

            return;
        }

        if (soltouBotaoPulo)
        {
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * corteDoPulo);

            soltouBotaoPulo = false;
        }
    }

    private void AplicarPesoNaQueda()
    {
        if (rb.linearVelocity.y < 0f)
        {
            // Gravidade extra na descida: o pulo fica menos "flutuante".
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y
                * (multiplicadorQueda - 1f) * Time.fixedDeltaTime;
        }
    }

    // ----------------- Visual -----------------

    private void Virar()
    {
        if (Mathf.Approximately(entradaHorizontal, 0f))
            return;

        OlhandoParaDireita = entradaHorizontal > 0f;

        // Espelhar pela escala (e nao por SpriteRenderer.flipX) vira tambem o
        // colisor, que fica deslocado do centro porque o desenho nao e simetrico.
        Vector3 escala = transform.localScale;
        escala.x = OlhandoParaDireita ? escalaOriginal : -escalaOriginal;
        transform.localScale = escala;
    }

    private void AtualizarAnimacao()
    {
        if (animator == null)
            return;

        animator.SetFloat("Velocidade", Mathf.Abs(entradaHorizontal));
        animator.SetFloat("VelocidadeY", rb.linearVelocity.y);
        animator.SetBool("NoChao", NoChao);
    }

    // ----------------- API usada por outros scripts -----------------

    /// <summary>Empurra o jogador, usado no knockback ao tomar dano.</summary>
    public void Empurrar(Vector2 forca)
    {
        rb.linearVelocity = forca;
        velocidadeSuavizada = 0f;
    }

    /// <summary>Faz o jogador quicar, usado ao pisar num inimigo ou no trampolim.</summary>
    public void Quicar(float forca)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, forca);
        pulosRestantes = pulosMaximos;
    }

    /// <summary>Segura o movimento horizontal por alguns instantes (golpe de espada no chao).</summary>
    public void TravarMovimento(float segundos)
    {
        movimentoTravadoAte = Mathf.Max(movimentoTravadoAte, Time.time + segundos);
    }

    public void DefinirControle(bool ativo)
    {
        controleAtivo = ativo;

        if (!ativo)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (checagemChao == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checagemChao.position, raioChecagem);
    }
}
