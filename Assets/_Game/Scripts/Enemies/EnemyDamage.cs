using UnityEngine;

/// <summary>
/// Contato entre inimigo e jogador: machuca ao encostar de lado e morre quando
/// o jogador cai em cima (o "pisao" do Mario), soltando um efeito visual.
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Dano")]
    [SerializeField] private int dano = 1;

    [Header("Pisao na cabeca")]
    [Tooltip("Desmarque para inimigos que nao podem ser mortos pisando, como serras e espinhosos.")]
    [SerializeField] private bool podeSerPisado = true;
    [Tooltip("O quanto o jogador precisa estar acima do centro do inimigo para valer como pisao.")]
    [SerializeField] private float alturaMinimaDoPisao = 0.35f;
    [SerializeField] private float forcaDoQuique = 14f;

    [Header("Espada")]
    [Tooltip("Marque em armadilhas (serra, fogo): a espada nao as destroi.")]
    [SerializeField] private bool imuneAEspada = false;

    [Header("Morte")]
    [Tooltip("Prefab do efeito que aparece no lugar do inimigo ao morrer.")]
    [SerializeField] private GameObject efeitoMorte;

    /// <summary>Disparado quando qualquer inimigo morre (o tutorial escuta para saber que o jogador aprendeu).</summary>
    public static event System.Action OnInimigoMorto;

    private EnemyPatrol patrulha;
    private bool morto;

    private void Awake()
    {
        patrulha = GetComponent<EnemyPatrol>();
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        TratarContato(colisao.collider);
    }

    private void OnCollisionStay2D(Collision2D colisao)
    {
        TratarContato(colisao.collider);
    }

    // Inimigos voadores usam colisor trigger para nao empurrar o jogador.
    private void OnTriggerEnter2D(Collider2D outro)
    {
        TratarContato(outro);
    }

    private void OnTriggerStay2D(Collider2D outro)
    {
        TratarContato(outro);
    }

    private void TratarContato(Collider2D outro)
    {
        if (morto || !outro.CompareTag("Player"))
            return;

        PlayerHealth vida = outro.GetComponent<PlayerHealth>();
        if (vida == null)
            return;

        Rigidbody2D rbJogador = outro.attachedRigidbody;
        bool vindoDeCima = rbJogador != null && rbJogador.linearVelocity.y < 0f;
        bool acimaDoInimigo = outro.transform.position.y > transform.position.y + alturaMinimaDoPisao;

        if (podeSerPisado && vindoDeCima && acimaDoInimigo)
        {
            PlayerController2D controlador = outro.GetComponent<PlayerController2D>();
            if (controlador != null)
                controlador.Quicar(forcaDoQuique);

            Morrer();
            return;
        }

        vida.TomarDano(dano, transform.position);
    }

    /// <summary>Golpe de espada. Devolve false se este inimigo nao pode ser cortado.</summary>
    public bool LevarGolpe()
    {
        if (morto || imuneAEspada)
            return false;

        Morrer();
        return true;
    }

    private void Morrer()
    {
        morto = true;
        AudioManager.Sfx(RetroSfx.Pisao);
        OnInimigoMorto?.Invoke();

        if (patrulha != null)
            patrulha.Parar();

        if (efeitoMorte != null)
            Instantiate(efeitoMorte, transform.position, Quaternion.identity);

        // O corpo some na hora; quem fica na tela e o efeito de morte.
        Destroy(gameObject);
    }
}
