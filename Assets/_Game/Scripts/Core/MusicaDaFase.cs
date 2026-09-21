using UnityEngine;

/// <summary>
/// Diz qual musica toca nesta cena. Um por cena, configurado pelo gerador.
/// </summary>
public class MusicaDaFase : MonoBehaviour
{
    [SerializeField] private AudioClip musica;

    private void Start()
    {
        AudioManager.Garantir().TocarMusica(musica);
    }
}
