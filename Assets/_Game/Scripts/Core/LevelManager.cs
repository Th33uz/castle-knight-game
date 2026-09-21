using UnityEngine;

/// <summary>
/// Controla o que e especifico de uma fase: onde o jogador nasce e o respawn
/// no checkpoint. Coloque um destes em cada cena de gameplay.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Referencias da fase")]
    [Tooltip("Objeto vazio marcando onde o jogador comeca a fase.")]
    [SerializeField] private Transform pontoDeNascimento;
    [Tooltip("O jogador ja presente na cena. Se ficar vazio, e procurado pela tag Player.")]
    [SerializeField] private Transform jogador;

    private void Start()
    {
        GarantirGameManager();

        if (jogador == null)
        {
            GameObject encontrado = GameObject.FindGameObjectWithTag("Player");
            if (encontrado != null)
                jogador = encontrado.transform;
        }

        if (jogador == null)
        {
            Debug.LogWarning("[LevelManager] Nenhum jogador encontrado na cena.", this);
            return;
        }

        // Nasce no checkpoint salvo; se nao houver, no ponto de nascimento da fase.
        Vector3 padrao = pontoDeNascimento != null ? pontoDeNascimento.position : jogador.position;
        jogador.position = GameManager.Instance.PontoDeNascimento(padrao);
    }

    /// <summary>
    /// Cria um GameManager caso a cena tenha sido aberta direto no Editor,
    /// sem passar pelo menu. Evita NullReferenceException durante os testes.
    /// </summary>
    private static void GarantirGameManager()
    {
        if (GameManager.Instance != null)
            return;

        GameObject go = new GameObject("GameManager (criado automaticamente)");
        go.AddComponent<GameManager>();
    }
}
