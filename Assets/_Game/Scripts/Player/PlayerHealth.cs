using System.Collections;
using UnityEngine;

/// <summary>
/// Vida do jogador dentro da fase (os coracoes da HUD).
/// Quando chega a zero, avisa o GameManager, que desconta uma tentativa
/// e reinicia a fase a partir do ultimo checkpoint.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 3;

    [Header("Ao tomar dano")]
    [Tooltip("Tempo invencivel depois de levar um golpe.")]
    [SerializeField] private float tempoInvencivel = 1f;
    [SerializeField] private Vector2 forcaDoEmpurrao = new Vector2(6f, 8f);

    [Header("Audio (opcional)")]
    [SerializeField] private AudioClip somDano;
    [SerializeField] private AudioClip somMorte;

    private PlayerController2D controlador;
    private Animator animator;
    private SpriteRenderer sprite;
    private AudioSource audioSource;

    private bool invencivel;
    private bool morto;

    public int VidaAtual { get; private set; }
    public int VidaMaxima => vidaMaxima;

    /// <summary>Disparado quando a vida muda, para a HUD redesenhar os coracoes.</summary>
    public event System.Action OnVidaMudou;

    private void Awake()
    {
        controlador = GetComponent<PlayerController2D>();
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        VidaAtual = vidaMaxima;
    }

    private void Start()
    {
        // Coracoes comprados na loja valem para o resto da partida.
        int extras = GameManager.Garantir().CoracoesExtras;
        if (extras > 0)
        {
            vidaMaxima += extras;
            VidaAtual = vidaMaxima;
        }

        OnVidaMudou?.Invoke();
    }

    /// <summary>Aumenta a vida maxima e ja preenche o coracao novo (compra na loja).</summary>
    public void AumentarVidaMaxima(int quantidade)
    {
        vidaMaxima += quantidade;
        VidaAtual = Mathf.Min(vidaMaxima, VidaAtual + quantidade);
        OnVidaMudou?.Invoke();
    }

    /// <summary>
    /// Tira vida do jogador. <paramref name="origemDoDano"/> serve apenas para
    /// decidir o lado do empurrao; passe a posicao do inimigo ou do espinho.
    /// </summary>
    public void TomarDano(int quantidade, Vector3 origemDoDano)
    {
        if (invencivel || morto)
            return;

        VidaAtual = Mathf.Max(0, VidaAtual - quantidade);
        OnVidaMudou?.Invoke();

        if (VidaAtual <= 0)
        {
            Morrer();
            return;
        }

        AudioManager.Sfx(somDano != null ? somDano : RetroSfx.Dano);

        if (animator != null)
            animator.SetTrigger("Dano");

        // Empurra para o lado oposto de quem causou o dano.
        if (controlador != null)
        {
            float direcao = transform.position.x < origemDoDano.x ? -1f : 1f;
            controlador.Empurrar(new Vector2(forcaDoEmpurrao.x * direcao, forcaDoEmpurrao.y));
        }

        StartCoroutine(FicarInvencivel());
    }

    /// <summary>Mata o jogador na hora, ignorando a vida. Usado por espinhos e pelo abismo.</summary>
    public void MatarInstantaneamente()
    {
        if (morto)
            return;

        VidaAtual = 0;
        OnVidaMudou?.Invoke();
        Morrer();
    }

    public void Curar(int quantidade)
    {
        if (morto)
            return;

        VidaAtual = Mathf.Min(vidaMaxima, VidaAtual + quantidade);
        OnVidaMudou?.Invoke();
    }

    private void Morrer()
    {
        morto = true;
        AudioManager.Sfx(somMorte != null ? somMorte : RetroSfx.Morte);

        if (animator != null)
            animator.SetTrigger("Morrer");

        if (controlador != null)
            controlador.DefinirControle(false);

        // Congela o corpo onde esta: sem isso ele continuava caindo com a
        // gravidade enquanto a animacao de morte tocava, atravessando o chao.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Desliga as colisoes para o corpo nao ficar preso ou tomando dano.
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // Deixa a animacao de morte tocar e depois escurece a tela; o GameManager
        // reinicia a fase em 1,4 s, quando a cortina ja fechou.
        StartCoroutine(EscurecerDepoisDaAnimacao());

        if (GameManager.Instance != null)
            GameManager.Instance.PerderVida();
    }

    private IEnumerator EscurecerDepoisDaAnimacao()
    {
        yield return new WaitForSeconds(0.6f);
        ScreenFader.Escurecer(0.6f);
    }

    private IEnumerator FicarInvencivel()
    {
        invencivel = true;

        // Pisca o sprite enquanto dura a invencibilidade.
        float tempoRestante = tempoInvencivel;
        const float intervaloPiscada = 0.1f;

        while (tempoRestante > 0f)
        {
            if (sprite != null)
                sprite.enabled = !sprite.enabled;

            yield return new WaitForSeconds(intervaloPiscada);
            tempoRestante -= intervaloPiscada;
        }

        if (sprite != null)
            sprite.enabled = true;

        invencivel = false;
    }

    private void Tocar(AudioClip clipe)
    {
        if (audioSource != null && clipe != null)
            audioSource.PlayOneShot(clipe);
    }
}
