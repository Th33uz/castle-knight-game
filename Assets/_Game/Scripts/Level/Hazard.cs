using UnityEngine;

/// <summary>
/// Obstaculo que machuca por contato: espinhos, serra, lava, agua.
/// Funciona tanto com colisor solido quanto com "Is Trigger".
/// </summary>
public class Hazard : MonoBehaviour
{
    [Tooltip("Marcado: mata na hora. Desmarcado: tira a quantidade de dano abaixo.")]
    [SerializeField] private bool mataInstantaneamente = false;
    [SerializeField] private int dano = 1;

    [Tooltip("Ao montar a fase, assenta no chao pela base do DESENHO e nao do colisor. " +
             "O fogo so machuca na chama, mas o queimador precisa ficar em cima do chao, nao dentro dele.")]
    [SerializeField] private bool assentaPeloDesenho = false;

    public bool AssentaPeloDesenho => assentaPeloDesenho;

    private void OnTriggerEnter2D(Collider2D outro)
    {
        Machucar(outro);
    }

    private void OnTriggerStay2D(Collider2D outro)
    {
        Machucar(outro);
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        Machucar(colisao.collider);
    }

    private void OnCollisionStay2D(Collision2D colisao)
    {
        Machucar(colisao.collider);
    }

    private void Machucar(Collider2D outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        PlayerHealth vida = outro.GetComponent<PlayerHealth>();
        if (vida == null)
            return;

        // A invencibilidade temporaria dentro do PlayerHealth ja impede que os
        // eventos Stay tirem vida todo frame.
        if (mataInstantaneamente)
            vida.MatarInstantaneamente();
        else
            vida.TomarDano(dano, transform.position);
    }
}
