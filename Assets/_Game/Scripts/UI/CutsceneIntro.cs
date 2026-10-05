using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Cutscene: toca o video e carrega a cena seguinte quando termina, ou quando o
/// jogador aperta Espaco / Esc / Enter (ou clica).
///
/// Serve tanto para a abertura (JOGAR -> video -> Tutorial) quanto para o final
/// (depois da Fase 4 -> video -> menu, com <see cref="voltarAoMenu"/> ligado).
///
/// O video e desenhado no plano proximo da camera (CameraNearPlane), entao nao
/// precisa de RenderTexture nem de Canvas.
/// </summary>
public class CutsceneIntro : MonoBehaviour
{
    [SerializeField] private VideoPlayer player;
    [Tooltip("Deixe vazio para usar a primeira fase definida no GameManager.")]
    [SerializeField] private string cenaSeguinte = "";

    [Tooltip("Cutscene final: ao terminar, encerra a partida e volta ao menu (ignora a cena seguinte).")]
    [SerializeField] private bool voltarAoMenu;

    [Tooltip("Tempo no inicio em que a tecla nao pula o video (evita pular com o clique que veio do menu).")]
    [SerializeField] private float carenciaParaPular = 0.8f;

    [Tooltip("Se o video travar, sai mesmo assim depois deste tempo.")]
    [SerializeField] private float limiteDeSeguranca = 180f;

    [Header("Tela ao terminar (opcional)")]
    [Tooltip("Painel que aparece quando o video acaba ou e pulado, antes de trocar de cena. " +
             "Abertura: a pergunta 'quer fazer o tutorial?'. Final: o 'Continua...'. Vazio = troca direto.")]
    [SerializeField] private GameObject telaAoTerminar;
    [Tooltip("Maior que zero: a tela segue sozinha depois destes segundos (ou com a tecla de confirmar). " +
             "Zero: espera o jogador apertar um dos botoes dela.")]
    [SerializeField] private float duracaoDaTela = 0f;

    private bool saindo;
    private bool telaAberta;
    private float tempo;
    private float instanteDaTela;

    private void Start()
    {
        if (player == null)
        {
            Ir();
            return;
        }

        // O AudioManager sobrevive a troca de cena: sem isto a musica do menu
        // continuaria tocando por cima do audio do video.
        if (AudioManager.Instance != null)
            AudioManager.Instance.PararMusica();

        player.loopPointReached += AoTerminar;
        player.errorReceived += AoFalhar;
        player.Play();
    }

    private void OnDestroy()
    {
        if (player == null)
            return;

        player.loopPointReached -= AoTerminar;
        player.errorReceived -= AoFalhar;
    }

    private void Update()
    {
        tempo += Time.unscaledDeltaTime;

        if (tempo < carenciaParaPular)
            return;

        if (telaAberta)
        {
            // Tela com tempo ("Continua..."): confirmar adianta. Tela com botoes:
            // quem trata a tecla e o EventSystem, pelo botao em foco.
            if (duracaoDaTela > 0f && GameInput.ConfirmouAgora && tempo - instanteDaTela > 0.5f)
                Seguir();

            return;
        }

        if (GameInput.ConfirmouAgora || tempo > limiteDeSeguranca)
            Ir();
    }

    private void AoTerminar(VideoPlayer _) => Ir();

    private void AoFalhar(VideoPlayer _, string mensagem)
    {
        Debug.LogWarning("[Cutscene] Falha no video: " + mensagem);
        Ir();
    }

    /// <summary>Video acabou (ou foi pulado): abre a tela de fim, se houver; senao troca de cena.</summary>
    private void Ir()
    {
        if (saindo || telaAberta)
            return;

        if (telaAoTerminar == null)
        {
            Seguir();
            return;
        }

        telaAberta = true;
        if (player != null)
            player.Stop();

        StartCoroutine(AbrirTela());
    }

    private IEnumerator AbrirTela()
    {
        // Um respiro antes de ativar: a tecla que pulou o video nao pode ser a
        // mesma que aperta o primeiro botao da tela.
        yield return new WaitForSecondsRealtime(0.25f);

        instanteDaTela = tempo;
        telaAoTerminar.SetActive(true);

        if (duracaoDaTela > 0f)
            Invoke(nameof(Seguir), duracaoDaTela);
    }

    // ----------------- Destinos -----------------

    /// <summary>Botao SIM da abertura: o tutorial, que depois leva a Fase 1.</summary>
    public void IrParaTutorial() => Carregar(GameManager.Garantir().PrimeiraFase);

    /// <summary>Botao NAO da abertura: direto para a primeira fase de verdade.</summary>
    public void IrParaPrimeiraFase() => Carregar(GameManager.Garantir().PrimeiraFaseDeVerdade);

    /// <summary>Destino padrao: menu (cutscene final) ou a cena seguinte configurada.</summary>
    public void Seguir()
    {
        if (saindo)
            return;

        // Fim de jogo: o GameManager zera vidas, moedas e coracoes extras antes
        // de abrir o menu, como em qualquer volta ao menu.
        if (voltarAoMenu)
        {
            saindo = true;
            if (player != null)
                player.Stop();

            GameManager.Garantir().VoltarAoMenu();
            return;
        }

        Carregar(!string.IsNullOrEmpty(cenaSeguinte) ? cenaSeguinte : GameManager.Garantir().PrimeiraFase);
    }

    private void Carregar(string destino)
    {
        if (saindo)
            return;

        saindo = true;
        CancelInvoke();

        if (player != null)
            player.Stop();

        Time.timeScale = 1f;
        SceneManager.LoadScene(destino);
    }
}
