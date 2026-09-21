using UnityEngine;

/// <summary>
/// Loja no mundo: o raposo vendedor. Quando o jogador entra na area, mostra o
/// aviso da tecla E flutuando; ao apertar, abre a ShopUI.
/// </summary>
public class Shop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer vendedor;
    [SerializeField] private SpriteRenderer aviso;
    [Tooltip("O desenho original do vendedor olha para a direita?")]
    [SerializeField] private bool vendedorOlhaParaDireita = true;

    private Transform jogador;
    private Vector3 posicaoBaseDoAviso;

    private void Awake()
    {
        if (aviso != null)
        {
            posicaoBaseDoAviso = aviso.transform.localPosition;
            aviso.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        jogador = outro.transform;
        if (aviso != null) aviso.enabled = true;
    }

    private void OnTriggerExit2D(Collider2D outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        jogador = null;
        if (aviso != null) aviso.enabled = false;
    }

    private void Update()
    {
        if (jogador == null)
            return;

        // O vendedor vira para o jogador.
        if (vendedor != null)
        {
            bool jogadorADireita = jogador.position.x > transform.position.x;
            vendedor.flipX = vendedorOlhaParaDireita ? !jogadorADireita : jogadorADireita;
        }

        if (aviso != null)
            aviso.transform.localPosition = posicaoBaseDoAviso + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.12f;

        if (!ShopUI.Aberta && ApertouInteragir())
            ShopUI.Abrir();
    }

    public static bool ApertouInteragir()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.UpArrow);
    }
}
