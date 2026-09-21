using UnityEngine;

/// <summary>
/// Ponto de salvamento. Ao encostar, o jogador passa a reaparecer aqui
/// quando morrer. Precisa de um colisor marcado como "Is Trigger".
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Visual (opcional)")]
    [Tooltip("Sprite mostrado depois de ativado, por exemplo a bandeira levantada.")]
    [SerializeField] private Sprite spriteAtivado;
    [SerializeField] private AudioClip somAtivacao;

    [Header("Posicao de nascimento")]
    [Tooltip("Altura somada a posicao do checkpoint, para o jogador nao nascer dentro do chao.")]
    [SerializeField] private float deslocamentoVertical = 0.5f;

    private SpriteRenderer sprite;
    private Animator animator;
    private bool ativado;

    private void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (ativado || !outro.CompareTag("Player"))
            return;

        ativado = true;

        Vector3 posicaoDeNascimento = transform.position + Vector3.up * deslocamentoVertical;

        if (GameManager.Instance != null)
            GameManager.Instance.SalvarCheckpoint(posicaoDeNascimento);

        if (animator != null)
            animator.SetTrigger("Ativar");
        else if (sprite != null && spriteAtivado != null)
            sprite.sprite = spriteAtivado;

        AudioManager.Sfx(somAtivacao != null ? somAtivacao : RetroSfx.Checkpoint);
    }
}
