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
    [SerializeField] private float distanciaAtras = 0.32f;
    [Tooltip("Atraso com que ele persegue o alvo. So o respiro para o gato nao parecer preso ao heroi por uma barra.")]
    [SerializeField] private float atrasoParaSeguir = 0.03f;
    [Tooltip("Folga: dentro disso ele considera que ja chegou e para. Pequena, para nao ficar arrancando e parando.")]
    [SerializeField] private float tolerancia = 0.14f;
    [Tooltip("Teto de velocidade. Bem acima da do heroi (8) para ele recuperar terreno depois de um pulo.")]
    [SerializeField] private float velocidade = 12f;
    [Tooltip("Quanto ele acelera e freia. Alto porque o heroi arranca quase instantaneo (SmoothDamp de 0,05 s).")]
    [SerializeField] private float aceleracao = 75f;
    [Tooltip("Forca com que ele corrige a distancia ate o alvo. Maior = cola mais; menor = chegada mais macia.")]
    [SerializeField] private float correcaoDeDistancia = 14f;
    [Tooltip("Fracao da aceleracao que vale no ar. So acelera, nunca freia, entao pode ser generosa.")]
    [SerializeField] [Range(0f, 1f)] private float controleNoAr = 0.5f;

    [Header("Pulo")]
    [Tooltip("Impulso para a frente ao saltar. Separado da velocidade de corrida, que e so para alcancar o heroi.")]
    [SerializeField] private float velocidadeNoPulo = 9f;
    [Tooltip("Pulo minimo, usado para degraus e obstaculos pequenos.")]
    [SerializeField] private float puloMinimo = 13f;
    [Tooltip("Limite do pulo. Acima disto ele nao alcanca e espera o jogador voltar.")]
    [SerializeField] private float puloMaximo = 24f;
    [Tooltip("Se o jogador estiver ao menos isto acima, ele tenta subir.")]
    [SerializeField] private float alturaParaPular = 1.2f;
    [Tooltip("Recarga entre pulos. Curta para ele nao perder o salto seguinte do heroi.")]
    [SerializeField] private float intervaloEntrePulos = 0.28f;
    [Tooltip("Ate esta distancia do heroi, o gato salta NO MESMO INSTANTE que ele, para o mesmo lado e com a mesma altura. " +
             "Mais longe que isso, ele corre ate o ponto do salto e pula la.")]
    [SerializeField] private float distanciaParaEspelhar = 2.5f;

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
    private float ultimoPuloDoHeroi = -10f;
    private bool puloDuploDisponivel = true;   // o gato repete o pulo duplo do heroi uma vez por voo

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

        bool noChao = NoChao();
        if (noChao)
            puloDuploDisponivel = true;

        // O heroi acabou de saltar com o gato colado nele: o gato salta junto,
        // agora e para o mesmo lado. Nada mais a decidir neste quadro.
        if (AnotarPuloDoHeroi(noChao))
        {
            CuidarDoOcio(false);
            CuidarDosPassos(false);
            AtualizarVisual(false);
            return;
        }

        float alvoX = CalcularAlvoX();
        float distancia = alvoX - transform.position.x;
        float direcao = Mathf.Sign(distancia);
        bool querAndar = Mathf.Abs(distancia) > tolerancia;

        // Le o terreno a frente ANTES de decidir se anda ou pula.
        bool temBuraco = querAndar && noChao && BuracoAFrente(direcao);
        bool temParede = querAndar && noChao && ParedeAFrente(direcao);
        float desnivel = jogador.position.y - transform.position.y;
        bool precisaSubir = desnivel > alturaParaPular;

        // Para casar com a marca de pulo vale a direcao do MOVIMENTO, nao a do
        // erro de posicao: colado no alvo, o erro troca de sinal a toda hora, e a
        // marca era descartada por "ir para o outro lado" mesmo com o gato
        // correndo para a frente. Ele so pulava depois, quando o sensor de parede
        // disparava - e era esse o pulo atrasado.
        float direcaoDoMovimento = Mathf.Abs(rb.linearVelocity.x) > 0.5f
            ? Mathf.Sign(rb.linearVelocity.x)
            : direcao;

        // Heroi em cima, mas com laje entre os dois: pular aqui e bater a cabeca
        // na laje e cair de volta. Primeiro sai de baixo dela pelo lado em que o
        // teto acaba mais perto; livre, o pulo normal de subida resolve.
        bool tetoNoCaminho = noChao && precisaSubir && TetoAcima(Mathf.Min(desnivel, 3f) + 0.3f);
        float ladoLivre = tetoNoCaminho ? LadoLivreDoTeto(direcao) : 0f;

        bool podePular = noChao && Time.time >= proximoPulo && !tetoNoCaminho;
        int marca = podePular ? MarcaAlcancada(direcaoDoMovimento) : -1;

        // Repetir o salto do heroi tem prioridade: os sensores de parede e buraco
        // sao a rede de seguranca, e sozinhos so disparam quando ele ja esta
        // encostando no obstaculo - tarde demais para acompanhar o pulo.
        bool vaiPular = podePular && (marca >= 0 || (querAndar && (precisaSubir || temParede || temBuraco)));

        if (vaiPular)
        {
            float alturaPedida = -1f;
            float direcaoDoPulo = direcao;

            // Repetindo o salto do heroi, o lado e o DELE. Antes saia o sinal do
            // erro de posicao, que colado no alvo troca a toda hora: o heroi
            // pulava para a direita e o gato, para a esquerda.
            if (marca >= 0)
            {
                MarcaDePulo m = marcasDePulo[marca];
                alturaPedida = m.altura;
                direcaoDoPulo = m.direcao != 0f ? m.direcao : direcaoDoMovimento;
                marcasDePulo.RemoveAt(marca);
            }

            Pular(direcaoDoPulo, desnivel, temBuraco, alturaPedida);
        }
        else if (tetoNoCaminho && ladoLivre != 0f && !BuracoAFrente(ladoLivre))
            AndarPara(ladoLivre);    // sai de baixo da laje antes de tentar subir
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

        // Heroi parado e gato ja perto: assenta de vez.
        //
        // A janela e a tolerancia inteira, e nao um pedaco dela, porque a
        // correcao e forte: a 0,05 do alvo ela ja pede 0,7 de velocidade. Isso
        // basta para o Animator continuar achando que o gato corre (o limiar la
        // e 0,3) e a animacao de parado nunca aparecer, mesmo com ele
        // praticamente imovel na tela.
        bool heroiParado = Mathf.Abs(velocidadeDoHeroi) < 0.5f;

        if (heroiParado && absoluta < tolerancia)
        {
            alvoVelocidade = 0f;
            seguindo = false;
        }

        // No ar ele SO ACELERA para alcancar o heroi, nunca freia.
        //
        // Deixar a correcao agir inteira no ar o fazia frear no meio do salto
        // quando o heroi ja tinha pousado e parado - e ai ele caia no buraco que
        // estava atravessando. Limitando a um sentido, ele cola no heroi durante
        // o pulo sem nunca perder o impulso que o tirou do chao.
        if (!noChao)
        {
            float atual = rb.linearVelocity.x;
            alvoVelocidade = atual >= 0f
                ? Mathf.Max(alvoVelocidade, atual)
                : Mathf.Min(alvoVelocidade, atual);
        }

        float controle = noChao ? aceleracao : aceleracao * controleNoAr;
        float novaVelocidadeX = Mathf.MoveTowards(rb.linearVelocity.x, alvoVelocidade, controle * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(novaVelocidadeX, rb.linearVelocity.y);
    }

    /// <summary>
    /// Reage ao salto do heroi no instante em que ele bate o pe.
    ///
    /// Gato perto (ate <see cref="distanciaParaEspelhar"/>): salta JUNTO, para o
    /// mesmo lado e com a mesma altura - e o que faz ele acompanhar o heroi por
    /// cima dos buracos em vez de chegar atrasado na beirada. Pulo duplo do
    /// heroi com o gato ainda no ar: o gato ganha o mesmo impulso extra.
    ///
    /// Gato longe: anota onde o heroi saiu do chao e repete o salto quando chegar
    /// la (MarcaAlcancada), porque pular de longe o jogaria contra a quina.
    /// Devolve true quando o gato saltou agora.
    /// </summary>
    private bool AnotarPuloDoHeroi(bool gatoNoChao)
    {
        if (jogadorRb == null)
            return false;

        float vy = jogadorRb.linearVelocity.y;
        bool subindo = vy > 1f;
        bool pulou = false;

        if (subindo && !heroiEstavaSubindo)
        {
            float gravidadeDoHeroi = Mathf.Abs(Physics2D.gravity.y) * jogadorRb.gravityScale;
            float altura = gravidadeDoHeroi > 0.01f ? (vy * vy) / (2f * gravidadeDoHeroi) : 1.5f;

            float vx = jogadorRb.linearVelocity.x;
            float direcaoDoHeroi = Mathf.Abs(vx) > 0.5f ? Mathf.Sign(vx) : 0f;
            bool segundoPulo = Time.time - ultimoPuloDoHeroi < 0.6f;
            float distanciaDoHeroi = Mathf.Abs(jogador.position.x - transform.position.x);
            ultimoPuloDoHeroi = Time.time;

            if (segundoPulo)
            {
                int ultima = marcasDePulo.Count - 1;

                if (!gatoNoChao && puloDuploDisponivel)
                {
                    // Pulo duplo espelhado: so o impulso vertical, mantendo o rumo.
                    float gravidade = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
                    float forca = Mathf.Clamp(Mathf.Sqrt(2f * gravidade * altura), puloMinimo, puloMaximo);
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, forca));
                    puloDuploDisponivel = false;
                    pulou = true;
                }
                else if (ultima >= 0 && Time.time - marcasDePulo[ultima].momento < 0.6f)
                {
                    // Gato ainda a caminho da marca: ela passa a pedir a altura somada.
                    MarcaDePulo anterior = marcasDePulo[ultima];
                    anterior.altura += altura;
                    marcasDePulo[ultima] = anterior;
                }
            }
            else if (gatoNoChao && Time.time >= proximoPulo && distanciaDoHeroi <= distanciaParaEspelhar
                     && !TetoAcima(Mathf.Min(altura, 3f) + 0.3f))
            {
                bool sobreBuraco = direcaoDoHeroi != 0f && BuracoAFrente(direcaoDoHeroi);
                Pular(direcaoDoHeroi, 0f, sobreBuraco, altura);
                pulou = true;
            }
            else
            {
                marcasDePulo.Add(new MarcaDePulo
                {
                    x = jogador.position.x,
                    altura = altura,
                    direcao = direcaoDoHeroi,
                    momento = Time.time
                });

                if (marcasDePulo.Count > 4)
                    marcasDePulo.RemoveAt(0);
            }
        }

        heroiEstavaSubindo = subindo;

        // Marca velha nao serve mais: aquele salto ja passou.
        marcasDePulo.RemoveAll(m => Time.time - m.momento > 2f);
        return pulou;
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
                // A folga para frente e generosa de proposito: pular um pouco
                // antes da marca fica bem melhor do que pular depois dela.
                float avanco = ateAMarca * m.direcao;
                if (avanco > 0.6f || avanco < -1.5f)
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
        // Sai no ritmo do heroi, com uma folga para recuperar o terreno que ele
        // ganhou entre os dois saltos. Um impulso fixo deixava o gato para tras
        // sempre que o heroi pulava correndo.
        float ritmoDoHeroi = jogadorRb != null ? Mathf.Abs(jogadorRb.linearVelocity.x) : 0f;
        float impulsoHorizontal = Mathf.Max(velocidadeNoPulo, ritmoDoHeroi + 1.5f);

        if (atravessandoBuraco)
        {
            forca = Mathf.Max(forca, puloMinimo * 1.25f);
            impulsoHorizontal = Mathf.Max(impulsoHorizontal, velocidadeNoPulo * 1.5f);
        }

        forca = Mathf.Clamp(forca, puloMinimo, puloMaximo);

        rb.linearVelocity = new Vector2(direcao * impulsoHorizontal, forca);
        proximoPulo = Time.time + intervaloEntrePulos;
    }

    /// <summary>Anda para um lado num passo firme, sem a correcao de distancia ate o heroi.</summary>
    private void AndarPara(float lado)
    {
        float novaVelocidadeX = Mathf.MoveTowards(rb.linearVelocity.x, lado * velocidade * 0.7f, aceleracao * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(novaVelocidadeX, rb.linearVelocity.y);
    }

    /// <summary>Ha laje ou teto ate esta altura acima da cabeca?</summary>
    private bool TetoAcima(float altura)
    {
        if (colisor == null)
            return false;

        Vector2 cabeca = new Vector2(colisor.bounds.center.x, colisor.bounds.max.y);
        return Physics2D.Raycast(cabeca, Vector2.up, altura, camadaChao)
            || Physics2D.Raycast(cabeca + Vector2.left * colisor.bounds.extents.x, Vector2.up, altura, camadaChao)
            || Physics2D.Raycast(cabeca + Vector2.right * colisor.bounds.extents.x, Vector2.up, altura, camadaChao);
    }

    /// <summary>
    /// Para que lado o teto acaba mais perto: -1, +1, ou 0 se nao acha saida em
    /// 4 unidades. Empate vai para o lado do heroi (<paramref name="preferido"/>).
    /// </summary>
    private float LadoLivreDoTeto(float preferido)
    {
        if (colisor == null)
            return 0f;

        Vector2 cabeca = new Vector2(colisor.bounds.center.x, colisor.bounds.max.y);

        for (float avanco = 0.5f; avanco <= 4f; avanco += 0.5f)
        {
            bool esquerdaLivre = !Physics2D.Raycast(cabeca + Vector2.left * avanco, Vector2.up, 3f, camadaChao);
            bool direitaLivre = !Physics2D.Raycast(cabeca + Vector2.right * avanco, Vector2.up, 3f, camadaChao);

            if (esquerdaLivre && direitaLivre)
                return preferido != 0f ? preferido : 1f;
            if (esquerdaLivre)
                return -1f;
            if (direitaLivre)
                return 1f;
        }

        return 0f;
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
