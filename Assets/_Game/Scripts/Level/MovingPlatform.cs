using UnityEngine;

/// <summary>
/// Plataforma que vai e volta entre dois pontos, levando junto quem estiver
/// em cima. Configure o Rigidbody2D como Kinematic.
///
/// O destino e definido por um deslocamento a partir de onde a plataforma
/// nasceu, e nao por outro objeto da cena, para nao quebrar quando o prefab
/// for movido ou duplicado.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Trajeto")]
    [Tooltip("Para onde ela vai, em relacao ao ponto de partida. Ex.: (5,0) anda 5 para a direita.")]
    [SerializeField] private Vector2 deslocamento = new Vector2(4f, 0f);
    [SerializeField] private float velocidade = 2f;
    [Tooltip("Segundos parada em cada ponta.")]
    [SerializeField] private float pausaNasPontas = 0.5f;

    private Rigidbody2D rb;
    private Vector2 pontoA;
    private Vector2 pontoB;
    private Vector2 destinoAtual;
    private float tempoDePausaRestante;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        pontoA = transform.position;
        pontoB = pontoA + deslocamento;
        destinoAtual = pontoB;
    }

    private void FixedUpdate()
    {
        if (tempoDePausaRestante > 0f)
        {
            tempoDePausaRestante -= Time.fixedDeltaTime;
            return;
        }

        Vector2 proximaPosicao = Vector2.MoveTowards(
            rb.position, destinoAtual, velocidade * Time.fixedDeltaTime);

        rb.MovePosition(proximaPosicao);

        // Chegou na ponta: inverte o destino e faz a pausa.
        if (Vector2.Distance(rb.position, destinoAtual) < 0.01f)
        {
            destinoAtual = destinoAtual == pontoB ? pontoA : pontoB;
            tempoDePausaRestante = pausaNasPontas;
        }
    }

    // Tornar o jogador filho da plataforma e a forma mais simples de ele ser
    // carregado junto, sem escorregar.
    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (colisao.collider.CompareTag("Player"))
            colisao.collider.transform.SetParent(transform);
    }

    private void OnCollisionExit2D(Collision2D colisao)
    {
        if (colisao.collider.CompareTag("Player"))
            colisao.collider.transform.SetParent(null);
    }

    private void OnDrawGizmos()
    {
        // Em edicao usa a posicao atual; rodando, usa o ponto onde ela nasceu.
        Vector3 inicio = Application.isPlaying ? (Vector3)pontoA : transform.position;
        Vector3 fim = inicio + (Vector3)deslocamento;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(inicio, fim);
        Gizmos.DrawWireCube(fim, Vector3.one * 0.3f);
    }
}
