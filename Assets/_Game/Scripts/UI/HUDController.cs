using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mostra na tela a vida e as moedas do jogador.
///
/// A vida pode ser exibida de duas formas: como fileira de coracoes, se o
/// array de imagens estiver preenchido, ou como texto, caso contrario.
/// A HUD se atualiza por eventos, sem checar nada no Update.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Vida em coracoes (opcional)")]
    [Tooltip("Uma Image por ponto de vida, na ordem da esquerda para a direita.")]
    [SerializeField] private Image[] imagensCoracoes;
    [SerializeField] private Sprite coracaoCheio;
    [SerializeField] private Sprite coracaoVazio;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoVida;
    [SerializeField] private TMP_Text textoMoedas;
    [SerializeField] private TMP_Text textoTentativas;

    private PlayerHealth vidaDoJogador;

    private void Start()
    {
        ConectarNoJogador();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEstadoMudou += AtualizarContadores;
            AtualizarContadores();
        }
    }

    private void OnDestroy()
    {
        // Sempre desinscrever: o GameManager sobrevive a troca de cena e
        // continuaria chamando uma HUD que ja foi destruida.
        if (GameManager.Instance != null)
            GameManager.Instance.OnEstadoMudou -= AtualizarContadores;

        if (vidaDoJogador != null)
            vidaDoJogador.OnVidaMudou -= AtualizarVida;
    }

    private void ConectarNoJogador()
    {
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador == null)
            return;

        vidaDoJogador = jogador.GetComponent<PlayerHealth>();
        if (vidaDoJogador == null)
            return;

        vidaDoJogador.OnVidaMudou += AtualizarVida;
        AtualizarVida();
    }

    private void AtualizarVida()
    {
        if (vidaDoJogador == null)
            return;

        if (imagensCoracoes != null && imagensCoracoes.Length > 0)
        {
            for (int i = 0; i < imagensCoracoes.Length; i++)
            {
                if (imagensCoracoes[i] == null)
                    continue;

                // Esconde os coracoes que passam do maximo de vida configurado.
                bool existeEsteCoracao = i < vidaDoJogador.VidaMaxima;
                imagensCoracoes[i].enabled = existeEsteCoracao;

                if (existeEsteCoracao)
                    imagensCoracoes[i].sprite = i < vidaDoJogador.VidaAtual ? coracaoCheio : coracaoVazio;
            }
        }

        if (textoVida != null)
            textoVida.text = vidaDoJogador.VidaAtual + "/" + vidaDoJogador.VidaMaxima;
    }

    private void AtualizarContadores()
    {
        if (GameManager.Instance == null)
            return;

        if (textoMoedas != null)
            textoMoedas.text = GameManager.Instance.Moedas.ToString();

        if (textoTentativas != null)
            textoTentativas.text = "x" + GameManager.Instance.Vidas;
    }
}
