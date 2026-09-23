using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Garante que sempre haja um botao em foco enquanto a tela estiver aberta.
///
/// Um clique no vazio (ou fechar um painel) limpa a selecao do EventSystem, e a
/// partir dai o controle simplesmente para de navegar: nao ha de onde sair. Aqui
/// o foco volta sozinho assim que o jogador mexe no direcional ou no analogico.
/// </summary>
public class FocoDeMenu : MonoBehaviour
{
    [Tooltip("Para onde o foco volta. Vazio = primeiro botao ativo dentro desta tela.")]
    [SerializeField] private Button botaoPadrao;

    private void OnEnable()
    {
        Focar();
    }

    private void Update()
    {
        if (TemFocoValido())
            return;

        // So devolve o foco quando o jogador demonstra que quer navegar; senao,
        // quem esta no mouse veria uma moldura aparecer do nada.
        if (Mathf.Abs(GameInput.Horizontal) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.5f)
            Focar();
    }

    private bool TemFocoValido()
    {
        EventSystem eventos = EventSystem.current;
        if (eventos == null)
            return true;   // sem EventSystem nao ha o que consertar

        GameObject atual = eventos.currentSelectedGameObject;
        return atual != null && atual.activeInHierarchy;
    }

    private void Focar()
    {
        if (EventSystem.current == null)
            return;

        Button alvo = botaoPadrao != null && botaoPadrao.gameObject.activeInHierarchy
            ? botaoPadrao
            : PrimeiroBotaoAtivo();

        if (alvo != null)
            EventSystem.current.SetSelectedGameObject(alvo.gameObject);
    }

    private Button PrimeiroBotaoAtivo()
    {
        foreach (Button b in GetComponentsInChildren<Button>(false))
        {
            if (b.IsInteractable())
                return b;
        }

        return null;
    }
}
