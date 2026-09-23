using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso de um comando na tela que se ajusta ao dispositivo em uso: mostra a
/// tecla enquanto o jogador esta no teclado e o botao do controle assim que ele
/// pega o gamepad.
///
/// Usa os icones do pack Input Prompts (Kenney). Antes isto escrevia o nome do
/// botao ("ESPACO", "A") num keycap desenhado; o desenho do teclado e do Xbox
/// se reconhece de imediato, sem ler.
/// </summary>
public class AvisoDeBotao : MonoBehaviour
{
    public enum Acao { Pular, Atacar, Interagir, Pausar, Andar }

    [SerializeField] private Acao acao = Acao.Pular;

    [Header("Icone")]
    [SerializeField] private Image icone;
    [SerializeField] private Sprite spriteTeclado;
    [SerializeField] private Sprite spriteControle;
    [Tooltip("Largura do icone. A tecla ESPACO e bem mais larga que o botao A.")]
    [SerializeField] private float larguraTeclado = 160f;
    [SerializeField] private float larguraControle = 76f;
    [SerializeField] private float altura = 76f;

    [Header("Texto (opcional, para quem nao tem icone)")]
    [SerializeField] private TMP_Text rotulo;
    [SerializeField] private RectTransform fundo;

    private bool? ultimoDispositivo;

    private void Update()
    {
        bool controle = GameInput.UsandoControle;

        // So mexe na interface quando o dispositivo realmente muda.
        if (ultimoDispositivo.HasValue && ultimoDispositivo.Value == controle)
            return;

        ultimoDispositivo = controle;
        Aplicar(controle);
    }

    private void Aplicar(bool controle)
    {
        if (icone != null)
        {
            Sprite sprite = controle ? spriteControle : spriteTeclado;
            if (sprite != null)
                icone.sprite = sprite;

            float largura = controle ? larguraControle : larguraTeclado;
            icone.rectTransform.sizeDelta = new Vector2(largura, altura);
        }

        if (rotulo != null)
            rotulo.text = NomeAtual();

        if (fundo != null)
        {
            float largura = controle ? larguraControle : larguraTeclado;
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
