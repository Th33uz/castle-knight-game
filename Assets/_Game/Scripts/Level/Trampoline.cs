using UnityEngine;

/// <summary>
/// Trampolim: lanca o jogador para cima ao pisar. O colisor pode ser solido ou
/// trigger; nos dois casos o impulso so vale quando o jogador vem de cima.
/// </summary>
public class Trampoline : MonoBehaviour
{
    [SerializeField] private float forca = 28f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        Lancar(colisao.collider);
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        Lancar(outro);
    }

    private void Lancar(Collider2D outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        PlayerController2D controlador = outro.GetComponent<PlayerController2D>();
        if (controlador == null)
            return;

        // So quando esta caindo sobre o trampolim, nao ao encostar de lado.
        Rigidbody2D rb = outro.attachedRigidbody;
        if (rb != null && rb.linearVelocity.y > 0.5f)
            return;

        controlador.Quicar(forca);
        AudioManager.Sfx(RetroSfx.Trampolim);

        if (animator != null)
            animator.SetTrigger("Ativar");
    }
}
