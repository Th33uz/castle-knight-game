using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida do chefe no topo da tela. Fica escondida ate o chefe acordar.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance { get; private set; }

    [SerializeField] private GameObject raiz;
    [SerializeField] private Image preenchimento;
    [SerializeField] private TMP_Text textoNome;

    private Boss chefeAtual;

    private void Awake()
    {
        Instance = this;
        if (raiz != null)
            raiz.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (chefeAtual != null)
            chefeAtual.OnVidaMudou -= Atualizar;
    }

    public static void Mostrar(Boss chefe)
    {
        if (Instance == null || chefe == null)
            return;

        Instance.chefeAtual = chefe;
        chefe.OnVidaMudou += Instance.Atualizar;

        if (Instance.textoNome != null)
            Instance.textoNome.text = chefe.Nome;

        Instance.Atualizar(chefe.VidaAtual, chefe.VidaMaxima);

        if (Instance.raiz != null)
            Instance.raiz.SetActive(true);
    }

    public static void Esconder()
    {
        if (Instance == null)
            return;

        if (Instance.chefeAtual != null)
            Instance.chefeAtual.OnVidaMudou -= Instance.Atualizar;

        Instance.chefeAtual = null;

        if (Instance.raiz != null)
            Instance.raiz.SetActive(false);
    }

    private void Atualizar(int atual, int maximo)
    {
        if (preenchimento != null)
            preenchimento.fillAmount = maximo > 0 ? (float)atual / maximo : 0f;
    }
}
