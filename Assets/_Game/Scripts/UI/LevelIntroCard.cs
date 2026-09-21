using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cartao de apresentacao da fase ("FASE 1 - FLORESTA" com um sprite do bioma).
/// Congela o jogo por alguns segundos ao carregar; qualquer tecla pula.
/// Roda depois do PauseMenu (ordem 50) para o timeScale nao ser sobrescrito.
/// </summary>
[DefaultExecutionOrder(50)]
public class LevelIntroCard : MonoBehaviour
{
    [Header("Conteudo")]
    [SerializeField] private string titulo = "FASE 1";
    [SerializeField] private string subtitulo = "FLORESTA";
    [SerializeField] private Sprite sprite;
    [SerializeField] private float duracao = 2.5f;

    [Header("Referencias")]
    [SerializeField] private GameObject raiz;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoSubtitulo;
    [SerializeField] private Image imagem;

    private void Start()
    {
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (textoSubtitulo != null) textoSubtitulo.text = subtitulo;
        if (imagem != null)
        {
            imagem.sprite = sprite;
            imagem.enabled = sprite != null;
        }

        if (raiz != null) raiz.SetActive(true);
        if (grupo != null) grupo.alpha = 1f;

        Time.timeScale = 0f;
        StartCoroutine(Mostrar());
    }

    private IEnumerator Mostrar()
    {
        float t = 0f;

        // Meio segundo sem aceitar tecla: evita que o clique em "Jogar" pule o cartao.
        while (t < duracao && (t < 0.5f || !Input.anyKeyDown))
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        float fade = 0f;
        while (fade < 0.35f)
        {
            fade += Time.unscaledDeltaTime;
            if (grupo != null) grupo.alpha = 1f - fade / 0.35f;
            yield return null;
        }

        Time.timeScale = 1f;
        if (raiz != null) raiz.SetActive(false);
    }

    private void OnDestroy()
    {
        // Sair da cena no meio do cartao nao pode deixar o jogo congelado.
        Time.timeScale = 1f;
    }
}
