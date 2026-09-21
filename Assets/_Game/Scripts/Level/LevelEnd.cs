using System.Collections;
using UnityEngine;

/// <summary>
/// Fim da fase (bandeira ou porta). Ao encostar, trava o jogador e carrega
/// a proxima cena da lista do Build Settings.
/// </summary>
public class LevelEnd : MonoBehaviour
{
    [Tooltip("Espera antes de trocar de cena, para dar tempo da animacao rodar.")]
    [SerializeField] private float atrasoParaTrocarDeFase = 1.2f;
    [SerializeField] private AudioClip somDeVitoria;

    private bool concluida;

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (concluida || !outro.CompareTag("Player"))
            return;

        concluida = true;

        // Tira o controle para o jogador nao andar durante a transicao.
        PlayerController2D controlador = outro.GetComponent<PlayerController2D>();
        if (controlador != null)
            controlador.DefinirControle(false);

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.SetTrigger("Ativar");

        AudioManager.Sfx(somDeVitoria != null ? somDeVitoria : RetroSfx.Vitoria);

        StartCoroutine(IrParaProximaFase());
    }

    private IEnumerator IrParaProximaFase()
    {
        // Metade do tempo para a animacao do trofeu, metade escurecendo a tela.
        yield return new WaitForSeconds(atrasoParaTrocarDeFase * 0.5f);
        ScreenFader.Escurecer(atrasoParaTrocarDeFase * 0.5f);
        yield return new WaitForSeconds(atrasoParaTrocarDeFase * 0.5f);

        if (GameManager.Instance != null)
            GameManager.Instance.ProximaFase();
    }
}
