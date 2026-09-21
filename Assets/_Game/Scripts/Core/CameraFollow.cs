using UnityEngine;

/// <summary>
/// Faz a camera seguir o jogador com suavizacao, opcionalmente presa aos
/// limites da fase para nao mostrar o vazio fora do cenario.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Quem a camera segue. Se ficar vazio, procura pela tag Player.")]
    [SerializeField] private Transform alvo;
    [SerializeField] private Vector2 deslocamento = new Vector2(0f, 1f);

    [Header("Suavizacao")]
    [Tooltip("Quanto maior, mais devagar a camera alcanca o jogador.")]
    [SerializeField] private float tempoDeSuavizacao = 0.15f;

    [Header("Limites da fase")]
    [SerializeField] private bool usarLimites = false;
    [SerializeField] private Vector2 limiteMinimo = new Vector2(-10f, -5f);
    [SerializeField] private Vector2 limiteMaximo = new Vector2(10f, 5f);

    private Vector3 velocidadeAtual;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (alvo == null)
        {
            GameObject jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador != null)
                alvo = jogador.transform;
        }

        // Comeca ja enquadrada, sem aquele deslize inicial.
        if (alvo != null)
            transform.position = PosicaoDesejada();
    }

    // LateUpdate roda depois de todo movimento do frame, evitando tremedeira.
    private void LateUpdate()
    {
        if (alvo == null)
            return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            PosicaoDesejada(),
            ref velocidadeAtual,
            tempoDeSuavizacao);
    }

    private Vector3 PosicaoDesejada()
    {
        float x = alvo.position.x + deslocamento.x;
        float y = alvo.position.y + deslocamento.y;

        if (usarLimites)
        {
            // Desconta meia tela para que a borda da camera pare no limite,
            // e nao o centro dela.
            float meiaAltura = cam != null ? cam.orthographicSize : 0f;
            float meiaLargura = meiaAltura * (cam != null ? cam.aspect : 1f);

            x = Mathf.Clamp(x, limiteMinimo.x + meiaLargura, limiteMaximo.x - meiaLargura);
            y = Mathf.Clamp(y, limiteMinimo.y + meiaAltura, limiteMaximo.y - meiaAltura);
        }

        // Mantem o Z da camera (normalmente -10), senao ela para de enxergar a cena.
        return new Vector3(x, y, transform.position.z);
    }

    // Desenha a caixa dos limites no Editor para facilitar o ajuste.
    private void OnDrawGizmosSelected()
    {
        if (!usarLimites)
            return;

        Gizmos.color = Color.yellow;
        Vector3 centro = (limiteMinimo + limiteMaximo) / 2f;
        Vector3 tamanho = limiteMaximo - limiteMinimo;
        Gizmos.DrawWireCube(centro, tamanho);
    }
}
