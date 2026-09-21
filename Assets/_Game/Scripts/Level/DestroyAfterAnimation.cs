using UnityEngine;

/// <summary>
/// Destroi o objeto quando a animacao do Animator termina. Usado nos efeitos
/// visuais de um tiro so: explosao da morte do inimigo, brilho ao pegar item.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DestroyAfterAnimation : MonoBehaviour
{
    [Tooltip("Tempo extra alem da duracao do clipe, por seguranca.")]
    [SerializeField] private float margem = 0.05f;

    private void Start()
    {
        Animator animator = GetComponent<Animator>();
        float duracao = 0.5f;

        // Pega a duracao real do clipe em vez de chutar um numero fixo.
        AnimatorClipInfo[] clipes = animator.GetCurrentAnimatorClipInfo(0);
        if (clipes.Length > 0 && clipes[0].clip != null)
            duracao = clipes[0].clip.length;

        Destroy(gameObject, duracao + margem);
    }
}
