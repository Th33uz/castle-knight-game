using UnityEngine;

/// <summary>
/// Toca a musica de fundo e os efeitos sonoros. Vive junto do GameManager,
/// sobrevivendo a troca de cena, para a musica nao recomecar quando a fase
/// reinicia depois de uma morte.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Range(0f, 1f)] [SerializeField] private float volumeMusica = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float volumeEfeitos = 0.8f;

    private AudioSource fonteMusica;
    private AudioSource fonteEfeitos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        fonteMusica = gameObject.AddComponent<AudioSource>();
        fonteMusica.loop = true;
        fonteMusica.playOnAwake = false;
        fonteMusica.volume = volumeMusica;

        fonteEfeitos = gameObject.AddComponent<AudioSource>();
        fonteEfeitos.playOnAwake = false;
        fonteEfeitos.volume = volumeEfeitos;
    }

    /// <summary>Garante que existe um AudioManager, criando-o no objeto do GameManager.</summary>
    public static AudioManager Garantir()
    {
        if (Instance != null)
            return Instance;

        GameObject dono = GameManager.Instance != null
            ? GameManager.Instance.gameObject
            : new GameObject("AudioManager");

        if (GameManager.Instance == null)
            DontDestroyOnLoad(dono);

        AudioManager existente = dono.GetComponent<AudioManager>();
        return existente != null ? existente : dono.AddComponent<AudioManager>();
    }

    /// <summary>Troca a musica so se for diferente da atual, para nao cortar ao reiniciar a fase.</summary>
    public void TocarMusica(AudioClip clipe)
    {
        if (clipe == null)
            return;

        if (fonteMusica.clip == clipe && fonteMusica.isPlaying)
            return;

        fonteMusica.clip = clipe;
        fonteMusica.Play();
    }

    public void PararMusica()
    {
        fonteMusica.Stop();
    }

    public void TocarEfeito(AudioClip clipe, float volume = 1f)
    {
        if (clipe != null)
            fonteEfeitos.PlayOneShot(clipe, volume);
    }

    // Atalhos estaticos: qualquer script chama AudioManager.Sfx(RetroSfx.Pulo) sem
    // se preocupar se o gerenciador ja existe.
    public static void Sfx(AudioClip clipe, float volume = 1f)
    {
        Garantir().TocarEfeito(clipe, volume);
    }
}
