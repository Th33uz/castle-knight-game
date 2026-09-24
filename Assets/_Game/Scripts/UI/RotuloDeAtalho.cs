using TMPro;
using UnityEngine;

/// <summary>
/// Rotulo de botao que mostra o atalho certo para o dispositivo em uso.
///
/// O botao de fechar a loja dizia "FECHAR  [E]" fixo, entao quem estava no
/// controle via a tecla do teclado e ficava sem saber o que apertar. Agora o
/// mesmo botao diz "[Y]" assim que o jogador pega o gamepad.
/// </summary>
public class RotuloDeAtalho : MonoBehaviour
{
    public enum Atalho
    {
        /// <summary>E no teclado, Y no controle.</summary>
        Interagir,
        /// <summary>Numero do item no teclado; no controle, o A que confirma o foco.</summary>
        Comprar,
    }

    [Tooltip("Os TMP_Text do botao: o texto e o brilho atras dele.")]
    [SerializeField] private TMP_Text[] textos;
    [SerializeField] private string rotulo = "FECHAR";
    [SerializeField] private Atalho atalho = Atalho.Interagir;
    [Tooltip("Indice do item na loja, para o atalho numerico do teclado.")]
    [SerializeField] private int indice;

    private bool? ultimoDispositivo;

    private void OnEnable()
    {
        ultimoDispositivo = null;   // forca reescrever ao reabrir a tela
    }

    private void Update()
    {
        bool controle = GameInput.UsandoControle;

        if (ultimoDispositivo.HasValue && ultimoDispositivo.Value == controle)
            return;

        ultimoDispositivo = controle;

        string texto = atalho == Atalho.Interagir
            ? rotulo + "  [" + GameInput.BotaoInteragir + "]"
            : rotulo + "  [" + (controle ? "A" : (indice + 1).ToString()) + "]";

        foreach (TMP_Text t in textos)
        {
            if (t != null)
                t.text = texto;
        }
    }
}
