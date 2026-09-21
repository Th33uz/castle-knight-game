using UnityEngine;

/// <summary>
/// Bola de fogo do chefe: voa reto, machuca o jogador e some ao bater no chao.
/// Colisor em modo trigger, Rigidbody2D sem gravidade.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projetil : MonoBehaviour
{
    [SerializeField] private int dano = 1;
    [SerializeField] private float vidaUtil = 4f;
    [SerializeField] private LayerMask camadasQueDestroem; // chao
    [SerializeField] private GameObject efeitoImpacto;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        rb.gravityScale = 0f;
        Destroy(gameObject, vidaUtil);
    }

    public void Lancar(Vector2 velocidade)
    {
        rb.linearVelocity = velocidade;

        if (sprite != null)
            sprite.flipX = velocidade.x < 0f;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("Player"))
        {
            PlayerHealth vida = outro.GetComponent<PlayerHealth>();
            if (vida != null)
                vida.TomarDano(dano, transform.position);

            Sumir();
            return;
        }

        if ((camadasQueDestroem.value & (1 << outro.gameObject.layer)) != 0)
            Sumir();
    }

    private void Sumir()
    {
        if (efeitoImpacto != null)
            Instantiate(efeitoImpacto, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
