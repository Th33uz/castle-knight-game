using UnityEngine;

/// <summary>
/// Faz o inimigo andar de um lado para o outro.
///
/// Inimigos de chao viram ao chegar na beirada da plataforma ou bater numa
/// parede (detectados por raycast). Inimigos voadores ignoram o chao e apenas
/// vao e voltam ate a distancia configurada, com um leve balanco vertical.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float velocidade = 2f;
    [Tooltip("Comeca andando para a direita?")]
    [SerializeField] private bool comecarParaDireita = true;
    [Tooltip("Marque se o sprite original ja olha para a ESQUERDA (a maioria dos packs olha para a esquerda).")]
    [SerializeField] private bool spriteOlhaParaEsquerda = true;

    [Header("Voador")]
    [SerializeField] private bool voador = false;
    [Tooltip("Amplitude do sobe-e-desce, em unidades. 0 desliga.")]
    [SerializeField] private float balancoVertical = 0.4f;
    [SerializeField] private float velocidadeDoBalanco = 2f;

    [Header("Detector de borda e parede (inimigos de chao)")]
    [Tooltip("Objeto vazio na frente dos pes. Sem ele, o inimigo usa o modo por distancia.")]
    [SerializeField] private Transform detectorBorda;
    [SerializeField] private float alcanceDetector = 0.6f;
    [SerializeField] private LayerMask camadaChao;

    [Header("Modo por distancia")]
    [Tooltip("Quantas unidades anda para cada lado a partir de onde nasceu.")]
    [SerializeField] private float distanciaPatrulha = 3f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;

    private Vector2 posicaoInicial;
    private int direcao = 1;
    private bool parado;
    private float tempo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        rb.freezeRotation = true;

        if (voador)
            rb.gravityScale = 0f;

        posicaoInicial = transform.position;
        direcao = comecarParaDireita ? 1 : -1;
        AtualizarSprite();
    }

    private void FixedUpdate()
    {
        if (parado)
        {
            rb.linearVelocity = voador ? Vector2.zero : new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (PrecisaVirar())
            Virar();

        tempo += Time.fixedDeltaTime;

        if (voador)
        {
            // Senoide no Y para o voo nao parecer um trilho reto.
            float alvoY = posicaoInicial.y + Mathf.Sin(tempo * velocidadeDoBalanco) * balancoVertical;
            float velY = (alvoY - rb.position.y) / Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(direcao * velocidade, velY);
        }
        else
        {
            rb.linearVelocity = new Vector2(direcao * velocidade, rb.linearVelocity.y);
        }

        if (animator != null)
            animator.SetFloat("Velocidade", Mathf.Abs(rb.linearVelocity.x));
    }

    private bool PrecisaVirar()
    {
        if (!voador && detectorBorda != null)
        {
            bool temChaoAdiante = Physics2D.Raycast(
                detectorBorda.position, Vector2.down, alcanceDetector, camadaChao);

            bool temParede = Physics2D.Raycast(
                detectorBorda.position + Vector3.up * 0.3f, Vector2.right * direcao, 0.25f, camadaChao);

            return !temChaoAdiante || temParede;
        }

        float deslocamento = transform.position.x - posicaoInicial.x;
        return (direcao > 0 && deslocamento >= distanciaPatrulha)
            || (direcao < 0 && deslocamento <= -distanciaPatrulha);
    }

    private void Virar()
    {
        direcao *= -1;
        AtualizarSprite();
    }

    private void AtualizarSprite()
    {
        if (sprite != null)
        {
            // Se a arte ja olha para a esquerda, andar para a direita exige espelhar.
            bool espelhar = spriteOlhaParaEsquerda ? direcao > 0 : direcao < 0;
            sprite.flipX = espelhar;
        }

        if (detectorBorda != null)
        {
            Vector3 pos = detectorBorda.localPosition;
            pos.x = Mathf.Abs(pos.x) * direcao;
            detectorBorda.localPosition = pos;
        }
    }

    /// <summary>Congela a patrulha. Chamado quando o inimigo morre.</summary>
    public void Parar()
    {
        parado = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!voador && detectorBorda != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(detectorBorda.position, detectorBorda.position + Vector3.down * alcanceDetector);
        }
        else
        {
            Vector3 centro = Application.isPlaying ? (Vector3)posicaoInicial : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(centro + Vector3.left * distanciaPatrulha, centro + Vector3.right * distanciaPatrulha);
        }
    }
}
