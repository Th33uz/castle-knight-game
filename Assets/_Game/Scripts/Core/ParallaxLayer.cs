using UnityEngine;

/// <summary>
/// Camada de fundo que se move mais devagar que a camera, criando profundidade.
///
/// Fator 0 = a camada fica presa na camera (ceu distante). Fator 1 = anda junto
/// com o mundo (primeiro plano). O sprite deve estar em Draw Mode "Tiled" com
/// largura suficiente para cobrir a fase inteira; assim nao e preciso reciclar
/// pedacos enquanto a camera anda.
/// </summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("Quanto da distancia percorrida pela camera esta camada acompanha.")]
    [SerializeField] private float fatorHorizontal = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float fatorVertical = 0.1f;

    private Transform cameraAlvo;
    private Vector3 posicaoInicialDaCamera;
    private Vector3 posicaoInicialDaCamada;

    private void Start()
    {
        if (Camera.main != null)
            cameraAlvo = Camera.main.transform;

        if (cameraAlvo == null)
            return;

        posicaoInicialDaCamera = cameraAlvo.position;
        posicaoInicialDaCamada = transform.position;
    }

    // Roda depois do CameraFollow (execution order maior), senao a camada fica
    // um quadro atrasada em relacao a camera e treme.
    private void LateUpdate()
    {
        if (cameraAlvo == null)
            return;

        Vector3 deslocamento = cameraAlvo.position - posicaoInicialDaCamera;

        // A camada se move NA MESMA direcao da camera, mas menos: o que sobra e o
        // deslocamento relativo que o olho percebe como distancia.
        transform.position = new Vector3(
            posicaoInicialDaCamada.x + deslocamento.x * (1f - fatorHorizontal),
            posicaoInicialDaCamada.y + deslocamento.y * (1f - fatorVertical),
            posicaoInicialDaCamada.z);
    }

    public void Configurar(float horizontal, float vertical)
    {
        fatorHorizontal = horizontal;
        fatorVertical = vertical;
    }
}
