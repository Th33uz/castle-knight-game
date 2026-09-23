using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Cutscene: toca o video e carrega a cena seguinte quando termina, ou quando o
/// jogador aperta Espaco / Esc / Enter (ou clica).
///
/// O video e desenhado no plano proximo da camera (CameraNearPlane), entao nao
/// precisa de RenderTexture nem de Canvas.
/// </summary>
public class CutsceneIntro : MonoBehaviour
{
    [SerializeField] private VideoPlayer player;
    [Tooltip("Deixe vazio para usar a primeira fase definida no GameManager.")]
    [SerializeField] private string cenaSeguinte = "";

    [Tooltip("Tempo no inicio em que a tecla nao pula o video (evita pular com o clique que veio do menu).")]
    [SerializeField] private float carenciaParaPular = 0.8f;

    [Tooltip("Se o video travar, sai mesmo assim depois deste tempo.")]
    [SerializeField] private float limiteDeSeguranca = 180f;

    private bool saindo;
    private float tempo;

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

        if (GameInput.ConfirmouAgora || tempo > limiteDeSeguranca)
            Ir();
    }

    private void AoTerminar(VideoPlayer _) => Ir();

    private void AoFalhar(VideoPlayer _, string mensagem)
    {
        Debug.LogWarning("[Cutscene] Falha no video: " + mensagem);
        Ir();
    }

    private void Ir()
    {
        if (saindo)
            return;

        saindo = true;

        if (player != null)
            player.Stop();

        string destino = !string.IsNullOrEmpty(cenaSeguinte)
            ? cenaSeguinte
            : GameManager.Garantir().PrimeiraFase;

        Time.timeScale = 1f;
        SceneManager.LoadScene(destino);
    }
}
