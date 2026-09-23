using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Destaque do botao em foco: moldura dourada em volta, seta ao lado e um leve
/// aumento de escala.
///
/// Sem isto, quem joga no controle nao enxerga onde esta: o tint do ColorBlock
/// do Button so clareia o sprite alguns por cento, o que some na pixel art. A
/// moldura e dourada de proposito, para destacar do verde dos botoes.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class RealceDeSelecao : MonoBehaviour
{
    [SerializeField] private GameObject moldura;
    [SerializeField] private GameObject seta;
    [Tooltip("Quanto o botao cresce ao ficar em foco.")]
    [SerializeField] private float escalaEmFoco = 1.06f;
    [Tooltip("Vai e volta do brilho da moldura, em ciclos por segundo.")]
    [SerializeField] private float pulsosPorSegundo = 1.6f;
    [SerializeField] [Range(0f, 1f)] private float volumeDoSom = 0.3f;

    private Selectable alvo;
    private Image molduraImagem;
    private Vector3 escalaOriginal;
    private bool emFoco;

    private void Awake()
    {
        alvo = GetComponent<Selectable>();
        escalaOriginal = transform.localScale;

        if (moldura != null)
            molduraImagem = moldura.GetComponent<Image>();

        Aplicar(false, silencioso: true);
    }

    private void OnEnable()
    {
        // A tela pode ter sido fechada com este botao em foco; recomeca neutro.
        Aplicar(false, silencioso: true);
    }

    private void Update()
    {
        bool agora = EstaEmFoco();

        if (agora != emFoco)
            Aplicar(agora, silencioso: false);

        if (!emFoco || molduraImagem == null)
            return;

        // Respiro do brilho: deixa claro qual e o botao ativo mesmo parado.
        // unscaledTime porque a loja e a pausa rodam com Time.timeScale = 0.
        float onda = (Mathf.Sin(Time.unscaledTime * pulsosPorSegundo * Mathf.PI * 2f) + 1f) * 0.5f;
        Color cor = molduraImagem.color;
        cor.a = Mathf.Lerp(0.6f, 1f, onda);
        molduraImagem.color = cor;
    }

    private bool EstaEmFoco()
    {
        if (alvo == null || !alvo.IsInteractable())
            return false;

        EventSystem eventos = EventSystem.current;
        return eventos != null && eventos.currentSelectedGameObject == gameObject;
    }

    private void Aplicar(bool foco, bool silencioso)
    {
        emFoco = foco;

        if (moldura != null) moldura.SetActive(foco);
        if (seta != null) seta.SetActive(foco);

        transform.localScale = foco ? escalaOriginal * escalaEmFoco : escalaOriginal;

        if (foco && !silencioso)
            AudioManager.Sfx(RetroSfx.Navegar, volumeDoSom);
    }
}
