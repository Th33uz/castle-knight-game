using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Menu de pausa acionado por Esc (ou Start no controle). Congela o jogo com
/// Time.timeScale = 0.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject painelPausa;
    [Tooltip("Botao que ja nasce com o foco ao pausar, para o controle navegar.")]
    [SerializeField] private GameObject primeiroBotao;

    public bool Pausado { get; private set; }

    private void Start()
    {
        if (painelPausa != null)
            painelPausa.SetActive(false);

        // Garante velocidade normal ao entrar na cena, mesmo que a anterior
        // tenha sido deixada pausada.
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Com a loja aberta, o Esc e dela (fecha a loja).
        if (ShopUI.Aberta)
            return;

        if (GameInput.PausouAgora)
        {
            if (Pausado)
                Continuar();
            else
                Pausar();
        }
    }

    public void Pausar()
    {
        Pausado = true;
        Time.timeScale = 0f;

        if (painelPausa != null)
            painelPausa.SetActive(true);

        if (EventSystem.current != null && primeiroBotao != null)
            EventSystem.current.SetSelectedGameObject(primeiroBotao);
    }

    public void Continuar()
    {
        Pausado = false;
        Time.timeScale = 1f;

        if (painelPausa != null)
            painelPausa.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void ReiniciarFase()
    {
        Time.timeScale = 1f;
        Pausado = false;
        GameManager.Instance.ReiniciarFase();
    }

    public void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        Pausado = false;
        GameManager.Instance.VoltarAoMenu();
    }

    private void OnDestroy()
    {
        // Evita sair da cena com o jogo congelado.
        Time.timeScale = 1f;
    }
}
