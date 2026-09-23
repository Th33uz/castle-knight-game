using UnityEngine;

/// <summary>
/// Gato companheiro com fisica propria: anda, cai, pula buracos e sobe
/// plataformas atras do jogador.
///
/// A ideia e que ele CONSIGA chegar sozinho: calcula a forca do pulo pela altura
/// que precisa vencer e enxerga o buraco antes de cair nele. O teleporte existe
/// so como ultimo recurso, e so acontece fora da tela, para nunca se ver o gato
/// "piscando" de um lugar para outro.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Companion : MonoBehaviour
{
    [Header("Seguir")]
    [Tooltip("Distancia que ele tenta manter atras do jogador.")]
    [SerializeField] private float distanciaAtras = 1.6f;
    [Tooltip("Folga: dentro disso ele considera que ja chegou e para.")]
    [SerializeField] private float tolerancia = 0.6f;
    [SerializeField] private float velocidade = 7.5f;
    [SerializeField] private float aceleracao = 30f;

    [Header("Pulo")]
    [Tooltip("Pulo minimo, usado para degraus e obstaculos pequenos.")]
    [SerializeField] private float puloMinimo = 13f;
    [Tooltip("Limite do pulo. Acima disto ele nao alcanca e espera o jogador voltar.")]
    [SerializeField] private float puloMaximo = 24f;
    [Tooltip("Se o jogador estiver ao menos isto acima, ele tenta subir.")]
    [SerializeField] private float alturaParaPular = 1.2f;
    [SerializeField] private float intervaloEntrePulos = 0.45f;

    [Header("Chao e obstaculos")]
    [SerializeField] private LayerMask camadaChao;
    [SerializeField] private float raioChecagemChao = 0.18f;
    [Tooltip("Distancia a frente em que ele procura o chao para saber se vem buraco.")]
    [SerializeField] private float alcanceDoDetectorDeBorda = 0.9f;

    [Header("Recuperacao (so fora da tela)")]
    [SerializeField] private float distanciaDeTeleporte = 16f;
    [SerializeField] private float tempoLongeParaTeleportar = 1.5f;
    [Tooltip("Altura absoluta abaixo da qual ele caiu do mapa.")]
    [SerializeField] private float alturaDeQueda = -4f;

    [Header("Sprite")]
    [SerializeField] private bool spriteOlhaParaDireita = true;

    private Rigidbody2D rb;
    private Collider2D colisor;
    private SpriteRenderer sprite;
    private Animator animator;

    private Transform jogador;
    private PlayerController2D controlador;
    private float proximoPulo;
    private float tempoLonge;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        EncontrarJogador();
        if (jogador != null)
            Teleportar();
    }

    private void EncontrarJogador()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null)
            return;

        jogador = go.transform;
        controlador = go.GetComponent<PlayerController2D>();
    }

    private void FixedUpdate()
    {
        if (jogador == null)
        {
            EncontrarJogador();
            return;
        }

        if (CuidarDaRecuperacao())
            return;

        float alvoX = CalcularAlvoX();
        float distancia = alvoX - transform.position.x;
        bool noChao = NoChao();
        float direcao = Mathf.Sign(distancia);
        bool querAndar = Mathf.Abs(distancia) > tolerancia;

        // Le o terreno a frente ANTES de decidir se anda ou pula.
        bool temBuraco = querAndar && noChao && BuracoAFrente(direcao);
        bool temParede = querAndar && noChao && ParedeAFrente(direcao);
        float desnivel = jogador.position.y - transform.position.y;
        bool precisaSubir = desnivel > alturaParaPular;

        bool podePular = noChao && querAndar && Time.time >= proximoPulo;
        bool vaiPular = podePular && (precisaSubir || temParede || temBuraco);

        if (vaiPular)
            Pular(direcao, desnivel, temBuraco);
        else if (temBuraco)
            PararNaBorda();          // pulo em recarga: espera, nao anda para dentro
        else
            Mover(distancia, noChao);

        AtualizarVisual();
    }

    /// <summary>Devolve true se teleportou (e o resto do frame deve ser ignorado).</summary>
    private bool CuidarDaRecuperacao()
    {
        // Caiu do mapa: volta na hora, esteja onde estiver.
        if (transform.position.y < alturaDeQueda)
        {
            Teleportar();
            return true;
        }

        if (Vector2.Distance(transform.position, jogador.position) > distanciaDeTeleporte)
            tempoLonge += Time.fixedDeltaTime;
        else
            tempoLonge = 0f;

        // Longe ha tempo demais: so teleporta se estiver fora da tela, para o
        // jogador nunca ver o gato aparecer do nada na frente dele.
        if (tempoLonge >= tempoLongeParaTeleportar && ForaDaTela())
        {
            Teleportar();
            return true;
        }

        return false;
    }

    private bool ForaDaTela()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return true;

        Vector3 v = cam.WorldToViewportPoint(transform.position);
        return v.z < 0f || v.x < -0.05f || v.x > 1.05f || v.y < -0.05f || v.y > 1.05f;
    }

    /// <summary>Um passo atras do jogador, do lado oposto ao que ele esta olhando.</summary>
    private float CalcularAlvoX()
    {
        bool jogadorOlhaDireita = controlador == null || controlador.OlhandoParaDireita;
        float lado = jogadorOlhaDireita ? -1f : 1f;
        return jogador.position.x + lado * distanciaAtras;
    }

    private void Mover(float distancia, bool noChao)
    {
        float alvoVelocidade = Mathf.Abs(distancia) > tolerancia
            ? Mathf.Sign(distancia) * velocidade
            : 0f;

        // No ar ele quase nao corrige a direcao: o pulo ja foi dado, deixa a
        // fisica levar (senao ele "nada" no ar e erra o salto).
        float controle = noChao ? aceleracao : aceleracao * 0.3f;
        float novaVelocidadeX = Mathf.MoveTowards(rb.linearVelocity.x, alvoVelocidade, controle * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(novaVelocidadeX, rb.linearVelocity.y);
    }

    private void Pular(float direcao, float desnivel, bool atravessandoBuraco)
    {
        // Forca pela altura a vencer: v = raiz(2 * g * h), com folga de 1,3 tile.
        // Assim nao pula de menos numa plataforma alta nem exagera num degrau.
        float alturaAlvo = Mathf.Max(desnivel, 0f) + 1.3f;
        float gravidade = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float forca = Mathf.Sqrt(2f * gravidade * alturaAlvo);

        // Sobre buraco o salto precisa de altura E de impulso para a frente:
        // quanto mais tempo no ar, mais longe ele chega.
        float impulsoHorizontal = velocidade;

        if (atravessandoBuraco)
        {
            forca = Mathf.Max(forca, puloMinimo * 1.25f);
            impulsoHorizontal = velocidade * 1.5f;
        }

        forca = Mathf.Clamp(forca, puloMinimo, puloMaximo);

        rb.linearVelocity = new Vector2(direcao * impulsoHorizontal, forca);
        proximoPulo = Time.time + intervaloEntrePulos;
    }

    /// <summary>Freia rapido para nao passar da beirada enquanto o pulo recarrega.</summary>
    private void PararNaBorda()
    {
        float novaVelocidadeX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, aceleracao * 2f * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(novaVelocidadeX, rb.linearVelocity.y);
    }

    private bool NoChao()
    {
        if (colisor == null)
            return false;

        Vector2 pe = new Vector2(colisor.bounds.center.x, colisor.bounds.min.y);
        return Physics2D.OverlapCircle(pe, raioChecagemChao, camadaChao);
    }

    private bool ParedeAFrente(float direcao)
    {
        if (colisor == null)
            return false;

        Vector2 origem = new Vector2(colisor.bounds.center.x, colisor.bounds.min.y + 0.15f);
        float alcance = colisor.bounds.extents.x + 0.35f;
        return Physics2D.Raycast(origem, Vector2.right * direcao, alcance, camadaChao);
    }

    /// <summary>
    /// Olha o chao logo a frente. Sem chao ali, e buraco: e hora de pular, e nao
    /// de andar ate cair dentro dele.
    /// </summary>
    private bool BuracoAFrente(float direcao)
    {
        if (colisor == null)
            return false;

        // Dois sensores: um logo na beirada e outro um passo adiante. O de perto
        // freia a tempo; o de longe evita pular quando e so um degrauzinho.
        float borda = colisor.bounds.extents.x;

        for (float avanco = 0.35f; avanco <= alcanceDoDetectorDeBorda; avanco += 0.55f)
        {
            Vector2 origem = new Vector2(
                colisor.bounds.center.x + direcao * (borda + avanco),
                colisor.bounds.min.y + 0.1f);

            // Sem chao ate 1,5 abaixo, o degrau e fundo demais: e buraco.
            if (!Physics2D.Raycast(origem, Vector2.down, 1.5f, camadaChao))
                return true;
        }

        return false;
    }

    private void Teleportar()
    {
        bool jogadorOlhaDireita = controlador == null || controlador.OlhandoParaDireita;
        float lado = jogadorOlhaDireita ? -1f : 1f;

        transform.position = jogador.position + new Vector3(lado * distanciaAtras, 0.5f, 0f);
        rb.linearVelocity = Vector2.zero;
        tempoLonge = 0f;
    }

    private void AtualizarVisual()
    {
        if (animator != null)
            animator.SetFloat("Velocidade", Mathf.Abs(rb.linearVelocity.x));

        if (sprite != null && Mathf.Abs(rb.linearVelocity.x) > 0.3f)
        {
            bool indoParaDireita = rb.linearVelocity.x > 0f;
            sprite.flipX = spriteOlhaParaDireita ? !indoParaDireita : indoParaDireita;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (colisor == null)
            colisor = GetComponent<Collider2D>();

        if (colisor == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector2(colisor.bounds.center.x, colisor.bounds.min.y), raioChecagemChao);

        // Detector de borda dos dois lados.
        Gizmos.color = Color.red;
        foreach (float d in new[] { -1f, 1f })
        {
            Vector2 origem = new Vector2(
                colisor.bounds.center.x + d * (colisor.bounds.extents.x + alcanceDoDetectorDeBorda),
                colisor.bounds.min.y + 0.1f);
            Gizmos.DrawLine(origem, origem + Vector2.down * 1.5f);
        }
    }
}
