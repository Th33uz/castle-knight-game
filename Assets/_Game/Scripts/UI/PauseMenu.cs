using UnityEngine;

/// <summary>
/// Menu de pausa acionado pelo Esc. Congela o jogo com Time.timeScale = 0.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject painelPausa;
    [SerializeField] private KeyCode teclaDePausa = KeyCode.Escape;

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

        if (Input.GetKeyDown(teclaDePausa))
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
    }

    public void Continuar()
    {
        Pausado = false;
        Time.timeScale = 1f;

        if (painelPausa != null)
            painelPausa.SetActive(false);
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
