using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [Header("Tela FASES")]
    [Tooltip("Botao que alterna entrar na fase com a vida normal ou ja com todos os coracoes.")]
    [SerializeField] private Button botaoCoracoes;

    /// <summary>Alterna a opcao de comecar a fase com a vida maxima e atualiza o rotulo do botao.</summary>
    public void AlternarCoracoesNoMaximo()
    {
        GameManager gm = GameManager.Garantir();
        gm.ComecarComCoracoesNoMaximo = !gm.ComecarComCoracoesNoMaximo;
        AtualizarRotuloDosCoracoes();
    }

    private void AtualizarRotuloDosCoracoes()
    {
        if (botaoCoracoes == null)
            return;

        bool maximo = GameManager.Instance != null && GameManager.Instance.ComecarComCoracoesNoMaximo;
        int total = 3 + GameManager.MaximoDeCoracoesExtras;
        string rotulo = maximo ? "CORAÇÕES: MÁXIMO (" + total + ")" : "CORAÇÕES: NORMAL (3)";

        // O botao tem dois textos (o rotulo e o brilho por baixo): os dois mudam.
        foreach (TMP_Text texto in botaoCoracoes.GetComponentsInChildren<TMP_Text>(true))
            texto.text = rotulo;
    }

    private void Start()
    {
        // O menu tambem precisa de um GameManager, porque e dele que sai o
        // comando para carregar a primeira fase.
        if (GameManager.Instance == null)
        {
            GameObject go = new GameObject("GameManager (criado automaticamente)");
            go.AddComponent<GameManager>();
        }

        AtualizarRotuloDosCoracoes();
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

        FocarPrimeiroBotao(painel);
    }

    /// <summary>
    /// Poe o foco no primeiro botao do painel que abriu. Sem isso o controle
    /// fica sem nada selecionado ao trocar de tela e para de navegar.
    /// </summary>
    private static void FocarPrimeiroBotao(GameObject painel)
    {
        if (painel == null || EventSystem.current == null)
            return;

        Button primeiro = painel.GetComponentInChildren<Button>();
        EventSystem.current.SetSelectedGameObject(primeiro != null ? primeiro.gameObject : null);
    }

    public void Sair()
    {
        GameManager.Instance.SairDoJogo();
    }
}
