using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rotulo de um botao na interface que se ajusta ao dispositivo em uso: mostra
/// a tecla quando o jogador esta no teclado e o botao quando esta no controle.
/// Usado no aviso de pular a cutscene.
/// </summary>
public class AvisoDeBotao : MonoBehaviour
{
    public enum Acao { Pular, Atacar, Interagir, Pausar, Andar }

    [SerializeField] private Acao acao = Acao.Pular;
    [Tooltip("Texto onde o nome do botao e escrito.")]
    [SerializeField] private TMP_Text rotulo;
    [Tooltip("Fundo do keycap: fica largo para caber ESPACO e estreito para A.")]
    [SerializeField] private RectTransform fundo;
    [SerializeField] private float larguraTeclado = 200f;
    [SerializeField] private float larguraControle = 90f;

    private string ultimoNome;

    private void Update()
    {
        string nome = NomeAtual();
        if (nome == ultimoNome)
            return;

        ultimoNome = nome;

        if (rotulo != null)
            rotulo.text = nome;

        if (fundo != null)
        {
            float largura = GameInput.UsandoControle ? larguraControle : larguraTeclado;
            fundo.sizeDelta = new Vector2(largura, fundo.sizeDelta.y);
        }
    }

    private string NomeAtual()
    {
        switch (acao)
        {
            case Acao.Atacar: return GameInput.BotaoAtacar;
            case Acao.Interagir: return GameInput.BotaoInteragir;
            case Acao.Pausar: return GameInput.BotaoPausar;
            case Acao.Andar: return GameInput.BotaoAndar;
            default: return GameInput.BotaoPular;
        }
    }
}
