using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guarda o estado global do jogo (vidas, moedas, checkpoint atual) e cuida da
/// troca de fases. Existe uma unica instancia, que sobrevive ao carregar cenas.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuracao")]
    [SerializeField] private int vidasIniciais = 3;
    [SerializeField] private string cenaMenu = "MainMenu";
    [SerializeField] private string cenaPrimeiraFase = "Tutorial";
    [Tooltip("Segundos entre a morte e o reinicio da fase: da tempo da animacao e do fade.")]
    [SerializeField] private float atrasoParaReiniciar = 1.4f;

    public int Vidas { get; private set; }
    public int Moedas { get; private set; }

    /// <summary>Coracoes comprados na loja; somam a vida maxima do jogador ate o fim da partida.</summary>
    public int CoracoesExtras { get; private set; }
    public const int MaximoDeCoracoesExtras = 2; // 3 base + 2 = 5 coracoes

    /// <summary>Devolve o GameManager, criando um se a cena foi aberta direto no Editor.</summary>
    public static GameManager Garantir()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("GameManager (criado automaticamente)");
        return go.AddComponent<GameManager>();
    }

    // Posicao do ultimo checkpoint tocado, para o jogador reaparecer nela.
    private Vector3 posicaoCheckpoint;
    private bool temCheckpoint;
    private string cenaDoCheckpoint;

    /// <summary>Disparado sempre que vidas ou moedas mudam, para a HUD se atualizar.</summary>
    public event System.Action OnEstadoMudou;

    private void Awake()
    {
        // Padrao singleton: se ja existe um GameManager, este aqui e duplicado.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Vidas = vidasIniciais;

        // Audio e fade vivem no mesmo objeto persistente.
        if (GetComponent<AudioManager>() == null) gameObject.AddComponent<AudioManager>();
        if (GetComponent<ScreenFader>() == null) gameObject.AddComponent<ScreenFader>();
    }

    // ----------------- Vidas e moedas -----------------

    [Tooltip("A cada tantas moedas o jogador ganha uma tentativa extra. 0 desliga.")]
    [SerializeField] private int moedasPorVidaExtra = 50;

    public void AdicionarMoeda(int quantidade = 1)
    {
        int antes = Moedas;
        Moedas += quantidade;

        // Cruzou um multiplo de 50: tentativa extra, como nas moedas do Mario.
        if (moedasPorVidaExtra > 0 && Moedas / moedasPorVidaExtra > antes / moedasPorVidaExtra)
        {
            Vidas++;
            AudioManager.Sfx(RetroSfx.VidaExtra);
        }

        OnEstadoMudou?.Invoke();
    }

    /// <summary>Tenta gastar moedas na loja. Devolve false se nao tem saldo.</summary>
    public bool GastarMoedas(int quantidade)
    {
        if (Moedas < quantidade)
            return false;

        Moedas -= quantidade;
        OnEstadoMudou?.Invoke();
        return true;
    }

    public void AdicionarVida(int quantidade = 1)
    {
        Vidas += quantidade;
        OnEstadoMudou?.Invoke();
    }

    public bool AdicionarCoracaoExtra()
    {
        if (CoracoesExtras >= MaximoDeCoracoesExtras)
            return false;

        CoracoesExtras++;
        OnEstadoMudou?.Invoke();
        return true;
    }

    /// <summary>Tira uma vida. Reinicia a fase, ou volta ao menu se acabaram as vidas.</summary>
    public void PerderVida()
    {
        Vidas--;
        OnEstadoMudou?.Invoke();

        if (Vidas > 0)
            Invoke(nameof(ReiniciarFase), atrasoParaReiniciar);
        else
            Invoke(nameof(GameOver), atrasoParaReiniciar);
    }

    // ----------------- Checkpoints -----------------

    public void SalvarCheckpoint(Vector3 posicao)
    {
        posicaoCheckpoint = posicao;
        temCheckpoint = true;
        cenaDoCheckpoint = SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Devolve o ponto onde o jogador deve nascer nesta cena.
    /// Se nao houver checkpoint valido, devolve <paramref name="padrao"/>.
    /// </summary>
    public Vector3 PontoDeNascimento(Vector3 padrao)
    {
        bool checkpointEhDestaCena = temCheckpoint && cenaDoCheckpoint == SceneManager.GetActiveScene().name;
        return checkpointEhDestaCena ? posicaoCheckpoint : padrao;
    }

    private void LimparCheckpoint()
    {
        temCheckpoint = false;
        cenaDoCheckpoint = null;
    }

    // ----------------- Troca de cenas -----------------

    public void ReiniciarFase()
    {
        CarregarCena(SceneManager.GetActiveScene().name, limparCheckpoint: false);
    }

    /// <summary>Carrega a proxima cena do Build Settings. Na ultima, volta ao menu.</summary>
    public void ProximaFase()
    {
        int proximoIndice = SceneManager.GetActiveScene().buildIndex + 1;

        if (proximoIndice < SceneManager.sceneCountInBuildSettings)
        {
            LimparCheckpoint();
            Time.timeScale = 1f;
            SceneManager.LoadScene(proximoIndice);
        }
        else
        {
            VoltarAoMenu();
        }
    }

    public void ComecarJogo()
    {
        IrParaFase(cenaPrimeiraFase);
    }

    /// <summary>Comeca um jogo novo direto numa fase (menu de selecao de fases).</summary>
    public void IrParaFase(string cena)
    {
        ReiniciarPartida();
        CarregarCena(cena, limparCheckpoint: true);
    }

    public void VoltarAoMenu()
    {
        ReiniciarPartida();
        CarregarCena(cenaMenu, limparCheckpoint: true);
    }

    private void ReiniciarPartida()
    {
        Vidas = vidasIniciais;
        Moedas = 0;
        CoracoesExtras = 0;
        OnEstadoMudou?.Invoke();
    }

    private void GameOver()
    {
        VoltarAoMenu();
    }

    private void CarregarCena(string nome, bool limparCheckpoint)
    {
        if (limparCheckpoint)
            LimparCheckpoint();

        Time.timeScale = 1f; // garante que o jogo nao fique pausado apos o menu de pausa
        SceneManager.LoadScene(nome);
    }

    public void SairDoJogo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
