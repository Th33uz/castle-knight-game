using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gato companheiro com fisica propria: anda, cai, pula buracos e sobe
/// plataformas atras do jogador.
///
/// A ideia e que ele CONSIGA chegar sozinho: calcula a forca do pulo pela altura
/// que precisa vencer e enxerga o buraco antes de cair nele. O teleporte existe
/// so como ultimo recurso, e so acontece fora da tela, para nunca se ver o gato
/// "piscando" de um lugar para outro.
///
/// Parado, ele tem vida propria: mia, depois senta e por fim dorme.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Companion : MonoBehaviour
{
    [Header("Seguir")]
    [Tooltip("Distancia que ele tenta manter atras do jogador. Bem curta: ele atravessa o heroi, entao pode colar sem empurrar.")]
    [SerializeField] private float distanciaAtras = 0.5f;
    [Tooltip("Atraso com que ele persegue o alvo. E so o respiro para nao ficar sincronizado demais - nao e a lentidao dele.")]
    [SerializeField] private float atrasoParaSeguir = 0.05f;
    [Tooltip("Folga: dentro disso ele considera que ja chegou e para. Pequena, para nao ficar arrancando e parando.")]
    [SerializeField] private float tolerancia = 0.2f;
    [Tooltip("Teto de velocidade. Bem acima da do heroi (8) para ele recuperar terreno depois de um pulo.")]
    [SerializeField] private float velocidade = 12f;
    [Tooltip("Quanto ele acelera e freia. Alto porque o heroi arranca quase instantaneo (SmoothDamp de 0,05 s).")]
    [SerializeField] private float aceleracao = 75f;
    [Tooltip("Forca com que ele corrige a distancia ate o alvo. Maior = cola mais; menor = chegada mais macia.")]
    [SerializeField] private float correcaoDeDistancia = 14f;

    [Header("Pulo")]
    [Tooltip("Impulso para a frente ao saltar. Separado da velocidade de corrida, que e so para alcancar o heroi.")]
    [SerializeField] private float velocidadeNoPulo = 9f;
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

    [Header("Ocio (quando o jogador para)")]
    [Tooltip("Segundos parado antes de dar um miadinho.")]
    [SerializeField] private float segundosParaMiar = 7f;
    [Tooltip("Continuando parado, ele senta.")]
    [SerializeField] private float segundosParaSentar = 12f;
    [Tooltip("E por fim dorme, com os Z subindo.")]
    [SerializeField] private float segundosParaDormir = 26f;
    [Tooltip("Ainda em pe depois do primeiro miado, solta outro de vez em quando.")]
    [SerializeField] private float intervaloEntreMiados = 14f;
    [SerializeField] [Range(0f, 1f)] private float volumeDoMiado = 0.5f;

    [Header("Passinhos")]
    [Tooltip("Patadas por segundo andando devagar e correndo. O ciclo de corrida tem 2 apoios.")]
    [SerializeField] private float passosDevagar = 3f;
    [SerializeField] private float passosCorrendo = 5.5f;
    [Tooltip("Baixinho de proposito: e patinha de gato, nao bota.")]
    [SerializeField] [Range(0f, 1f)] private float volumeDosPassos = 0.16f;

    [Header("Sprite")]
    [SerializeField] private bool spriteOlhaParaDireita = true;

    private Rigidbody2D rb;
    private Collider2D colisor;
    private SpriteRenderer sprite;
    private Animator animator;

    private Transform jogador;
    private PlayerController2D controlador;
    private Rigidbody2D jogadorRb;
    private float proximoPulo;
    private float tempoLonge;
    private bool seguindo;
    private float[] historicoDoAlvo;
    private int posicaoNoHistorico;
    private float tempoAteOProximoPasso;
    private int passoAtual;

    /// <summary>Onde o heroi bateu o pe para pular, e que altura ele venceu.</summary>
    private struct MarcaDePulo
    {
        public float x;
        public float altura;
        public float direcao;   // 0 = pulou parado
        public float momento;
    }

    private readonly List<MarcaDePulo> marcasDePulo = new List<MarcaDePulo>();
    private bool heroiEstavaSubindo;

    // Ocio: 0 = acordado, 1 = ja miou, 2 = sentado, 3 = dormindo.
    private float tempoParado;
    private int etapaDeOcio;
    private float proximoMiado;

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
        jogadorRb = go.GetComponent<Rigidbody2D>();
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

        AnotarPuloDoHeroi();

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

        bool podePular = noChao && Time.time >= proximoPulo;
        int marca = podePular ? MarcaAlcancada(direcao) : -1;

        // Repetir o salto do heroi tem prioridade: os sensores de parede e buraco
        // sao a rede de seguranca, e sozinhos so disparam quando ele ja esta
        // encostando no obstaculo - tarde demais para acompanhar o pulo.
        bool vaiPular = podePular && (marca >= 0 || (querAndar && (precisaSubir || temParede || temBuraco)));

        if (vaiPular)
        {
            float alturaPedida = -1f;

            if (marca >= 0)
            {
                alturaPedida = marcasDePulo[marca].altura;
                marcasDePulo.RemoveAt(marca);
            }

            Pular(direcao, desnivel, temBuraco, alturaPedida);
        }
        else if (temBuraco)
            PararNaBorda();          // pulo em recarga: espera, nao anda para dentro
        else
            Mover(distancia, noChao);

        CuidarDoOcio(noChao);
        CuidarDosPassos(noChao);
        AtualizarVisual(noChao);
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
        return AlvoAtrasado(jogador.position.x + lado * distanciaAtras);
    }

    /// <summary>
    /// Guarda o alvo de cada passo e devolve o de alguns quadros atras. E esse
    /// respiro que faz o gato parecer que segue o heroi, e nao que esta preso
    /// nele: ele pode andar colado sem ficar sincronizado demais.
    /// </summary>
    private float AlvoAtrasado(float alvoAgora)
    {
        int quadros = Mathf.Max(1, Mathf.RoundToInt(atrasoParaSeguir / Time.fixedDeltaTime));

        if (historicoDoAlvo == null || historicoDoAlvo.Length != quadros)
        {
            historicoDoAlvo = new float[quadros];
            for (int i = 0; i < quadros; i++)
                historicoDoAlvo[i] = alvoAgora;
            posicaoNoHistorico = 0;
        }

        float antigo = historicoDoAlvo[posicaoNoHistorico];
        historicoDoAlvo[posicaoNoHistorico] = alvoAgora;
        posicaoNoHistorico = (posicaoNoHistorico + 1) % quadros;
        return antigo;
    }

    private void Mover(float distancia, bool noChao)
    {
        float absoluta = Mathf.Abs(distancia);

        // Histerese: so sai andando quando passa da tolerancia e so considera que
        // chegou bem mais perto. Sem essa folga ele fica ligando e desligando em
        // cima do limite.
        if (absoluta > tolerancia)
            seguindo = true;
        else if (absoluta < tolerancia * 0.4f)
            seguindo = false;

        // Ele copia a velocidade do heroi e so CORRIGE a distancia por cima disso.
        // Corrigindo sozinha, a correcao precisaria de um erro grande para dar
        // velocidade de corrida, e o gato viveria mais de um metro atrasado.
        float velocidadeDoHeroi = jogadorRb != null ? jogadorRb.linearVelocity.x : 0f;
        float alvoVelocidade = Mathf.Clamp(
            velocidadeDoHeroi + distancia * correcaoDeDistancia, -velocidade, velocidade);

        // Chegou, e o heroi tambem parou: para de vez, em vez de ficar se
        // ajustando em cima do ponto.
        if (!seguindo && Mathf.Abs(velocidadeDoHeroi) < 0.2f)
            alvoVelocidade = 0f;

        // No ar ele quase nao corrige a direcao: o pulo ja foi dado, deixa a
        // fisica levar (senao ele "nada" no ar e erra o salto). A fracao e baixa
        // justamente porque a aceleracao de chao e alta.
        float controle = noChao ? aceleracao : aceleracao * 0.15f;
        float novaVelocidadeX = Mathf.MoveTowards(rb.linearVelocity.x, alvoVelocidade, controle * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(novaVelocidadeX, rb.linearVelocity.y);
    }

    /// <summary>
    /// Anota onde o heroi bateu o pe para pular. O gato repete o salto no MESMO
    /// PONTO, e nao no mesmo instante: como ele anda um passo atras, pular junto
    /// o faria saltar cedo demais e bater na quina da plataforma.
    /// </summary>
    private void AnotarPuloDoHeroi()
    {
        if (jogadorRb == null)
            return;

        float vy = jogadorRb.linearVelocity.y;
        bool subindo = vy > 1f;

        if (subindo && !heroiEstavaSubindo)
        {
            float gravidadeDoHeroi = Mathf.Abs(Physics2D.gravity.y) * jogadorRb.gravityScale;
            float altura = gravidadeDoHeroi > 0.01f ? (vy * vy) / (2f * gravidadeDoHeroi) : 1.5f;

            int ultima = marcasDePulo.Count - 1;

            // Pulo duplo: o gato so tem um pulo, entao em vez de anotar um segundo
            // salto ele soma a altura na marca de onde o heroi saiu do chao.
            if (ultima >= 0 && Time.time - marcasDePulo[ultima].momento < 0.6f)
            {
                MarcaDePulo anterior = marcasDePulo[ultima];
                anterior.altura += altura;
                marcasDePulo[ultima] = anterior;
            }
            else
            {
                float vx = jogadorRb.linearVelocity.x;

                marcasDePulo.Add(new MarcaDePulo
                {
                    x = jogador.position.x,
                    altura = altura,
                    direcao = Mathf.Abs(vx) > 0.5f ? Mathf.Sign(vx) : 0f,
                    momento = Time.time
                });

                if (marcasDePulo.Count > 4)
                    marcasDePulo.RemoveAt(0);
            }
        }

        heroiEstavaSubindo = subindo;

        // Marca velha nao serve mais: aquele salto ja passou.
        marcasDePulo.RemoveAll(m => Time.time - m.momento > 2f);
    }

    /// <summary>Indice da marca que o gato acabou de alcancar, ou -1 se nenhuma.</summary>
    private int MarcaAlcancada(float direcao)
    {
        for (int i = 0; i < marcasDePulo.Count; i++)
        {
            MarcaDePulo m = marcasDePulo[i];
            float ateAMarca = m.x - transform.position.x;

            if (m.direcao != 0f)
            {
                if (Mathf.Sign(direcao) != m.direcao)
                    continue;

                // Projetado no sentido da corrida: positivo = ainda nao chegou.
                float avanco = ateAMarca * m.direcao;
                if (avanco > 0.35f || avanco < -1.5f)
                    continue;
            }
            else if (Mathf.Abs(ateAMarca) > 0.5f)
            {
                continue;   // heroi pulou parado: so vale bem em cima do ponto
            }

            return i;
        }

        return -1;
    }

    private void Pular(float direcao, float desnivel, bool atravessandoBuraco, float alturaPedida = -1f)
    {
        // Forca pela altura a vencer: v = raiz(2 * g * h), com folga de 1,3 tile.
        // Assim nao pula de menos numa plataforma alta nem exagera num degrau.
        // Repetindo o salto do heroi, a altura ja vem pronta.
        float alturaAlvo = alturaPedida > 0f
            ? alturaPedida
            : Mathf.Max(desnivel, 0f) + 1.3f;

        float gravidade = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float forca = Mathf.Sqrt(2f * gravidade * alturaAlvo);

        // Sobre buraco o salto precisa de altura E de impulso para a frente:
        // quanto mais tempo no ar, mais longe ele chega.
        float impulsoHorizontal = velocidadeNoPulo;

        if (atravessandoBuraco)
        {
            forca = Mathf.Max(forca, puloMinimo * 1.25f);
            impulsoHorizontal = velocidadeNoPulo * 1.5f;
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
        seguindo = false;
        historicoDoAlvo = null;   // recomeca o rastro do zero no novo lugar
        marcasDePulo.Clear();     // os saltos anotados eram de outro trecho da fase
        AcordarDoOcio();
    }

    /// <summary>
    /// Enquanto o jogador fica parado, o gato vai relaxando: primeiro um miado,
    /// depois senta e por fim dorme. Qualquer movimento zera tudo e ele levanta.
    /// </summary>
    private void CuidarDoOcio(bool noChao)
    {
        bool parado = noChao && Mathf.Abs(rb.linearVelocity.x) < 0.2f;

        if (!parado)
        {
            AcordarDoOcio();
            return;
        }

        tempoParado += Time.fixedDeltaTime;

        if (etapaDeOcio == 0 && tempoParado >= segundosParaMiar)
        {
            etapaDeOcio = 1;
            Miar();
        }
        else if (etapaDeOcio == 1 && tempoParado >= segundosParaSentar)
        {
            etapaDeOcio = 2;
            Disparar("Sentar");
        }
        else if (etapaDeOcio == 2 && tempoParado >= segundosParaDormir)
        {
            etapaDeOcio = 3;
            Disparar("Dormir");
        }
        else if (etapaDeOcio == 1 && tempoParado >= proximoMiado)
        {
            // Ainda em pe: mia de novo antes de resolver sentar.
            Miar();
        }
    }

    /// <summary>
    /// Patinhas no chao enquanto ele anda. A cadencia acompanha a velocidade,
    /// para o som bater com a animacao em vez de ficar solto.
    /// </summary>
    private void CuidarDosPassos(bool noChao)
    {
        float rapidez = Mathf.Abs(rb.linearVelocity.x);

        if (!noChao || rapidez < 0.8f)
        {
            tempoAteOProximoPasso = 0f;
            return;
        }

        tempoAteOProximoPasso -= Time.fixedDeltaTime;
        if (tempoAteOProximoPasso > 0f)
            return;

        // A referencia e a velocidade de passeio (8), nao o teto: correndo acima
        // disso ele ja esta na cadencia maxima.
        float cadencia = Mathf.Lerp(passosDevagar, passosCorrendo, Mathf.InverseLerp(0.8f, 8f, rapidez));
        tempoAteOProximoPasso = 1f / cadencia;

        AudioManager.Sfx(RetroSfx.Passinho(passoAtual), volumeDosPassos);
        passoAtual++;
    }

    private void Miar()
    {
        AudioManager.Sfx(RetroSfx.Miado, volumeDoMiado);
        Disparar("Miar");
        proximoMiado = tempoParado + intervaloEntreMiados;
    }

    private void AcordarDoOcio()
    {
        tempoParado = 0f;
        etapaDeOcio = 0;
        proximoMiado = 0f;
    }

    private void Disparar(string trigger)
    {
        if (animator != null && TemParametro(trigger))
            animator.SetTrigger(trigger);
    }

    private bool TemParametro(string nome)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == nome)
                return true;

        return false;
    }

    private void AtualizarVisual(bool noChao)
    {
        if (animator != null)
        {
            animator.SetFloat("Velocidade", Mathf.Abs(rb.linearVelocity.x));

            if (TemParametro("NoChao"))
                animator.SetBool("NoChao", noChao);
        }

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
