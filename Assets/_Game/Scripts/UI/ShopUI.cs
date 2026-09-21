using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela da loja. Pausa o jogo enquanto aberta; compra por clique ou teclas 1/2/3.
/// Os itens e precos ficam aqui, num lugar so.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public enum TipoDeItem { Fruta, VidaExtra, CoracaoExtra }

    public struct ItemDaLoja
    {
        public string nome, descricao;
        public int preco;
        public TipoDeItem tipo;
    }

    public static readonly ItemDaLoja[] Itens =
    {
        new ItemDaLoja { nome = "FRUTA",          descricao = "recupera 1 coracao",            preco = 15, tipo = TipoDeItem.Fruta },
        new ItemDaLoja { nome = "VIDA EXTRA",     descricao = "+1 tentativa",                  preco = 40, tipo = TipoDeItem.VidaExtra },
        new ItemDaLoja { nome = "CORACAO EXTRA",  descricao = "+1 na vida maxima (ate 5)",     preco = 70, tipo = TipoDeItem.CoracaoExtra },
    };

    public static ShopUI Instance { get; private set; }
    public static bool Aberta => Instance != null && Instance.raiz != null && Instance.raiz.activeSelf;

    [SerializeField] private GameObject raiz;
    [SerializeField] private TMP_Text textoSaldo;
    [SerializeField] private Button[] botoesComprar;
    [SerializeField] private TMP_Text[] textosStatus;

    private PlayerHealth vidaDoJogador;
    private PlayerController2D controlador;

    private void Awake()
    {
        Instance = this;
        if (raiz != null)
            raiz.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Abrir()
    {
        if (Instance == null || Aberta)
            return;

        Instance.AbrirInterno();
    }

    private void AbrirInterno()
    {
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador != null)
        {
            vidaDoJogador = jogador.GetComponent<PlayerHealth>();
            controlador = jogador.GetComponent<PlayerController2D>();
            if (controlador != null) controlador.DefinirControle(false);
        }

        raiz.SetActive(true);
        Time.timeScale = 0f;
        AudioManager.Sfx(RetroSfx.Checkpoint, 0.5f);
        Atualizar();
    }

    public void Fechar()
    {
        if (!Aberta)
            return;

        raiz.SetActive(false);
        Time.timeScale = 1f;

        if (controlador != null)
            controlador.DefinirControle(true);
    }

    private void Update()
    {
        if (!Aberta)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Shop.ApertouInteragir())
        {
            Fechar();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) Comprar(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Comprar(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Comprar(2);
    }

    /// <summary>Ligado no OnClick de cada botao, com o indice do item.</summary>
    public void Comprar(int indice)
    {
        if (indice < 0 || indice >= Itens.Length || GameManager.Instance == null)
            return;

        ItemDaLoja item = Itens[indice];

        if (!PodeComprar(item, out _))
        {
            AudioManager.Sfx(RetroSfx.Recusado);
            return;
        }

        if (!GameManager.Instance.GastarMoedas(item.preco))
        {
            AudioManager.Sfx(RetroSfx.Recusado);
            return;
        }

        switch (item.tipo)
        {
            case TipoDeItem.Fruta:
                vidaDoJogador.Curar(1);
                AudioManager.Sfx(RetroSfx.Cura);
                break;

            case TipoDeItem.VidaExtra:
                GameManager.Instance.AdicionarVida();
                AudioManager.Sfx(RetroSfx.VidaExtra);
                break;

            case TipoDeItem.CoracaoExtra:
                GameManager.Instance.AdicionarCoracaoExtra();
                vidaDoJogador.AumentarVidaMaxima(1);
                AudioManager.Sfx(RetroSfx.VidaExtra);
                break;
        }

        AudioManager.Sfx(RetroSfx.Compra);
        Atualizar();
    }

    /// <summary>Se nao pode, <paramref name="motivo"/> diz por que (aparece no botao).</summary>
    private bool PodeComprar(ItemDaLoja item, out string motivo)
    {
        motivo = "";

        if (item.tipo == TipoDeItem.Fruta && (vidaDoJogador == null || vidaDoJogador.VidaAtual >= vidaDoJogador.VidaMaxima))
        {
            motivo = "VIDA CHEIA";
            return false;
        }

        if (item.tipo == TipoDeItem.CoracaoExtra && GameManager.Instance.CoracoesExtras >= GameManager.MaximoDeCoracoesExtras)
        {
            motivo = "MAXIMO";
            return false;
        }

        if (GameManager.Instance.Moedas < item.preco)
        {
            motivo = "FALTAM " + (item.preco - GameManager.Instance.Moedas);
            return false;
        }

        return true;
    }

    private void Atualizar()
    {
        if (GameManager.Instance == null)
            return;

        if (textoSaldo != null)
            textoSaldo.text = GameManager.Instance.Moedas.ToString();

        for (int i = 0; i < Itens.Length; i++)
        {
            bool pode = PodeComprar(Itens[i], out string motivo);

            if (botoesComprar != null && i < botoesComprar.Length && botoesComprar[i] != null)
                botoesComprar[i].interactable = pode;

            if (textosStatus != null && i < textosStatus.Length && textosStatus[i] != null)
                textosStatus[i].text = pode ? "COMPRAR  [" + (i + 1) + "]" : motivo;
        }
    }
}
