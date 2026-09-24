using UnityEngine;

/// <summary>
/// Faixa invisivel colocada abaixo do cenario. Mata qualquer coisa que caia
/// fora do mapa, para o jogador nao ficar caindo para sempre.
/// Use um BoxCollider2D bem largo com "Is Trigger" marcado.
/// </summary>
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("Player"))
        {
            PlayerHealth vida = outro.GetComponent<PlayerHealth>();
            if (vida != null)
                vida.MatarInstantaneamente();

            return;
        }

        // Inimigos empurrados para fora do mapa somem em vez de cair infinitamente.
        // Pela CAMADA, e nao pela tag: "Enemy" nunca foi criada no projeto (o
        // Unity so traz Player, MainCamera e afins prontas), entao o CompareTag
        // enchia o console de "Tag: Enemy is not defined" e nunca era verdadeiro.
        if (outro.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            Destroy(outro.gameObject);
    }
}
