using UnityEngine;

public enum TipoDeItem
{
    Moeda,  // diamante: soma no contador
    Cura    // fruta: recupera vida; so pode ser pega se o jogador estiver machucado
}

/// <summary>
/// Item que o jogador pega ao encostar. O colisor precisa estar marcado como
/// "Is Trigger".
/// </summary>
public class Collectible : MonoBehaviour
{
    [SerializeField] private TipoDeItem tipo = TipoDeItem.Moeda;
    [Tooltip("Moeda: quantas moedas vale. Cura: quantos pontos de vida recupera.")]
    [SerializeField] private int valor = 1;
    [SerializeField] private AudioClip somAoPegar;
    [Tooltip("Efeito opcional que aparece no lugar do item ao pega-lo.")]
    [SerializeField] private GameObject efeitoVisual;

    private bool jaPego;

    private void OnTriggerEnter2D(Collider2D outro)
    {
        // A trava evita contar duas vezes se o jogador tiver mais de um colisor.
        if (jaPego || !outro.CompareTag("Player"))
            return;

        if (tipo == TipoDeItem.Cura)
        {
            PlayerHealth vida = outro.GetComponent<PlayerHealth>();

            // Com a vida cheia a fruta fica no lugar, esperando ser util.
            if (vida == null || vida.VidaAtual >= vida.VidaMaxima)
                return;

            vida.Curar(valor);
            AudioManager.Sfx(somAoPegar != null ? somAoPegar : RetroSfx.Cura);
        }
        else
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AdicionarMoeda(valor);

            AudioManager.Sfx(somAoPegar != null ? somAoPegar : RetroSfx.Moeda);
        }

        jaPego = true;

        if (efeitoVisual != null)
            Instantiate(efeitoVisual, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
