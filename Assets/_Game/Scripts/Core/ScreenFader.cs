using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Escurece a tela na morte e na troca de fase, e clareia ao entrar numa cena.
/// Cria o proprio Canvas em runtime e vive junto do GameManager, entao nao
/// precisa existir em nenhuma cena.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    private Image cortina;
    private Coroutine animacao;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        CriarCortina();

        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
    }

    private void CriarCortina()
    {
        GameObject canvasGo = new GameObject("Canvas Fade");
        canvasGo.transform.SetParent(transform);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // por cima de HUD e menus

        GameObject imagemGo = new GameObject("Cortina", typeof(RectTransform));
        imagemGo.transform.SetParent(canvasGo.transform, false);

        cortina = imagemGo.AddComponent<Image>();
        cortina.color = new Color(0f, 0f, 0f, 0f);
        cortina.raycastTarget = false; // nao bloqueia os botoes

        RectTransform rect = imagemGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        // Toda cena comeca preta e clareia: esconde o "pulo" de posicao do respawn.
        DefinirAlpha(1f);
        Fade(0f, 0.45f);
    }

    public static void Escurecer(float duracao = 0.5f)
    {
        Garantir().Fade(1f, duracao);
    }

    public static void Clarear(float duracao = 0.45f)
    {
        Garantir().Fade(0f, duracao);
    }

    private static ScreenFader Garantir()
    {
        if (Instance != null)
            return Instance;

        GameObject dono = GameManager.Instance != null ? GameManager.Instance.gameObject : new GameObject("ScreenFader");
        if (GameManager.Instance == null)
            DontDestroyOnLoad(dono);

        return dono.AddComponent<ScreenFader>();
    }

    private void Fade(float alphaFinal, float duracao)
    {
        if (animacao != null)
            StopCoroutine(animacao);

        animacao = StartCoroutine(Animar(alphaFinal, duracao));
    }

    private IEnumerator Animar(float alphaFinal, float duracao)
    {
        float inicial = cortina.color.a;
        float t = 0f;

        // Tempo nao escalado: funciona mesmo com o jogo pausado (timeScale 0).
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            DefinirAlpha(Mathf.Lerp(inicial, alphaFinal, t / duracao));
            yield return null;
        }

        DefinirAlpha(alphaFinal);
        animacao = null;
    }

    private void DefinirAlpha(float a)
    {
        Color c = cortina.color;
        c.a = a;
        cortina.color = c;
    }
}
