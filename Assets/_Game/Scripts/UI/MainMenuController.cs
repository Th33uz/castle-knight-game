using UnityEngine;

/// <summary>
/// Botoes do menu principal. Ligue cada metodo no evento OnClick do botao
/// correspondente, pelo Inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelControles;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject painelFases;

    private void Start()
    {
        // O menu tambem precisa de um GameManager, porque e dele que sai o
        // comando para carregar a primeira fase.
        if (GameManager.Instance == null)
        {
            GameObject go = new GameObject("GameManager (criado automaticamente)");
            go.AddComponent<GameManager>();
        }

        MostrarPrincipal();
    }

    public void Jogar()
    {
        GameManager.Instance.ComecarJogo();
    }

    public void MostrarControles()
    {
        Mostrar(painelControles);
    }

    public void MostrarCreditos()
    {
        Mostrar(painelCreditos);
    }

    public void MostrarFases()
    {
        Mostrar(painelFases);
    }

    /// <summary>Ligado nos botoes da selecao de fases, com o nome da cena como argumento.</summary>
    public void IrParaFase(string cena)
    {
        GameManager.Instance.IrParaFase(cena);
    }

    public void MostrarPrincipal()
    {
        Mostrar(painelPrincipal);
    }

    private void Mostrar(GameObject painel)
    {
        if (painelPrincipal != null) painelPrincipal.SetActive(painel == painelPrincipal);
        if (painelControles != null) painelControles.SetActive(painel == painelControles);
        if (painelCreditos != null) painelCreditos.SetActive(painel == painelCreditos);
        if (painelFases != null) painelFases.SetActive(painel == painelFases);
    }

    public void Sair()
    {
        GameManager.Instance.SairDoJogo();
    }
}
