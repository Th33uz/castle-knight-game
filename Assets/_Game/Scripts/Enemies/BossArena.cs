using UnityEngine;

/// <summary>
/// Gatilho na entrada da arena: quando o jogador cruza, o chefe acorda e a
/// parede atras do jogador aparece, para nao dar para fugir.
/// </summary>
public class BossArena : MonoBehaviour
{
    [SerializeField] private Boss chefe;
    [SerializeField] private GameObject parede;

    private bool disparado;

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (disparado || !outro.CompareTag("Player"))
            return;

        disparado = true;

        if (parede != null)
            parede.SetActive(true);

        if (chefe != null)
            chefe.Ativar();
    }
}
