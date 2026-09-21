using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Guia do tutorial: mostra um balao acima do jogador com a instrucao da vez.
///
/// Passos de ACAO (andar, pular, atacar...) so terminam quando a acao e feita, e
/// enquanto isso uma barreira translucida fecha o caminho a frente. Dicas de
/// POSICAO (buraco, espinhos...) aparecem ao passar por um ponto e somem
/// adiante, sem travar nada.
/// Os passos estao amarrados ao desenho da cena Tutorial em LevelDesigns.
/// </summary>
public class TutorialGuide : MonoBehaviour
{
    public enum Conclusao { Andar, Coletar, Pular, PuloDuplo, Atacar, MatarInimigo, PassarX }

    [System.Serializable]
    public class Passo
    {
        public string texto;
        public float xInicio;       // o balao aparece quando o jogador passa daqui
        public Conclusao conclusao; // o que encerra o passo
        public float xLimite;       // acao: onde fica a barreira; dica: onde ela some

        public Passo(string texto, float xInicio, Conclusao conclusao, float xLimite)
        {
            this.texto = texto;
            this.xInicio = xInicio;
            this.conclusao = conclusao;
            this.xLimite = xLimite;
        }

        public bool EhAcao => conclusao != Conclusao.PassarX;
    }

    [Header("Balao")]
    [SerializeField] private RectTransform balao;
    [SerializeField] private TMP_Text texto;
    [SerializeField] private float alturaAcimaDoJogador = 2.4f;

    [Header("Barreira")]
    [SerializeField] private int camadaDaBarreira;
    [SerializeField] private Color corDaBarreira = new Color(0.55f, 0.9f, 1f, 0.45f);

    private readonly List<Passo> passos = PassosPadrao();
    private int indice;
    private bool passoAtivo;

    private Transform jogador;
    private PlayerController2D controlador;
    private RectTransform canvasRect;

    private GameObject barreira;
    private SpriteRenderer brilhoDaBarreira;

    private float tempoAndando;
    private int moedasNoInicioDoPasso;
    private bool inimigoMorreu;

    private static List<Passo> PassosPadrao()
    {
        return new List<Passo>
        {
            // Acoes: a barreira fica em xLimite ate a acao ser feita.
            new Passo("ANDAR:  SETAS  ou  A / D",                    0f,   Conclusao.Andar,        6f),
            new Passo("PEGUE OS DIAMANTES",                           0f,   Conclusao.Coletar,     10f),
            new Passo("PULAR:  ESPACO",                               0f,   Conclusao.Pular,       13f),
            new Passo("PULO DUPLO:  ESPACO DE NOVO NO AR",            0f,   Conclusao.PuloDuplo,   17f),
            new Passo("ATACAR:  L",                                   0f,   Conclusao.Atacar,      19.5f),
            new Passo("GAMBA A FRENTE!  PISE NELE OU CORTE COM L",   17f,  Conclusao.MatarInimigo, 28f),

            // Dicas: aparecem em xInicio e somem em xLimite.
            new Passo("BURACO!  USE O PULO DUPLO",                    25f,  Conclusao.PassarX,     32f),
            new Passo("ESPINHOS TIRAM VIDA.  PULE!",                  31.5f, Conclusao.PassarX,    36.5f),
            new Passo("FRUTA CURA 1 CORACAO  (SO SE ESTIVER FERIDO)", 36f,  Conclusao.PassarX,     41f),
            new Passo("CHECKPOINT:  SALVA SEU PROGRESSO",             45f,  Conclusao.PassarX,     50f),
            new Passo("BURACO GRANDE:  PULO DUPLO!",                  50.5f, Conclusao.PassarX,    57f),
            new Passo("TRAMPOLIM:  PULE EM CIMA",                     57f,  Conclusao.PassarX,     63f),
            new Passo("AGUIA!  PISE NELA OU CORTE",                   68f,  Conclusao.PassarX,     76f),
            new Passo("LOJA DO RAPOSO:  CHEGUE PERTO E APERTE  E",    86f,  Conclusao.PassarX,     93.5f),
            new Passo("DOIS GAMBAS:  HORA DA ESPADA  (L)",            93.5f, Conclusao.PassarX,    103f),
            new Passo("CHEGUE AO TROFEU!",                            103f, Conclusao.PassarX,     999f),
        };
    }

    private void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            jogador = go.transform;
            controlador = go.GetComponent<PlayerController2D>();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        EnemyDamage.OnInimigoMorto += AoMorrerInimigo;

        CriarBarreira();

        if (balao != null)
            balao.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        EnemyDamage.OnInimigoMorto -= AoMorrerInimigo;
    }

    private void AoMorrerInimigo()
    {
        inimigoMorreu = true;
    }

    // ----------------- Passos -----------------

    private void Update()
    {
        if (jogador == null || indice >= passos.Count)
            return;

        Passo passo = passos[indice];
        float x = jogador.position.x;

        if (!passoAtivo)
        {
            if (x < passo.xInicio)
                return;

            ComecarPasso(passo);
        }

        bool concluido = passo.EhAcao
            ? Verificar(passo.conclusao)
            : x >= passo.xLimite;

        if (concluido)
            ConcluirPasso(passo);
    }

    private void ComecarPasso(Passo passo)
    {
        passoAtivo = true;
        tempoAndando = 0f;
        inimigoMorreu = false;
        moedasNoInicioDoPasso = GameManager.Instance != null ? GameManager.Instance.Moedas : 0;

        if (texto != null)
            texto.text = passo.texto;

        if (balao != null)
            balao.gameObject.SetActive(true);

        if (passo.EhAcao)
            MostrarBarreira(passo.xLimite);
    }

    private void ConcluirPasso(Passo passo)
    {
        passoAtivo = false;
        indice++;

        if (balao != null)
            balao.gameObject.SetActive(false);

        if (passo.EhAcao)
        {
            EsconderBarreira();
            AudioManager.Sfx(RetroSfx.Checkpoint, 0.6f);
        }
    }

    private bool Verificar(Conclusao conclusao)
    {
        switch (conclusao)
        {
            case Conclusao.Andar:
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f)
                    tempoAndando += Time.deltaTime;
                return tempoAndando >= 0.6f;

            case Conclusao.Coletar:
                return GameManager.Instance != null && GameManager.Instance.Moedas > moedasNoInicioDoPasso;

            case Conclusao.Pular:
                return Input.GetButtonDown("Jump") && controlador != null && controlador.NoChao;

            case Conclusao.PuloDuplo:
                return Input.GetButtonDown("Jump") && controlador != null && !controlador.NoChao;

            case Conclusao.Atacar:
                return PlayerAttack.ApertouAtaque();

            case Conclusao.MatarInimigo:
                return inimigoMorreu;

            default:
                return false;
        }
    }

    // ----------------- Barreira -----------------

    /// <summary>Coluna translucida com colisor, criada em runtime (nao existe na cena).</summary>
    private void CriarBarreira()
    {
        barreira = new GameObject("BarreiraDoTutorial");
        barreira.layer = camadaDaBarreira;

        BoxCollider2D col = barreira.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 40f);

        brilhoDaBarreira = barreira.AddComponent<SpriteRenderer>();
        brilhoDaBarreira.sprite = SpriteDeBrilho();
        brilhoDaBarreira.drawMode = SpriteDrawMode.Tiled;
        brilhoDaBarreira.size = new Vector2(0.6f, 40f);
        brilhoDaBarreira.sortingLayerName = "Foreground";
        brilhoDaBarreira.color = corDaBarreira;

        barreira.SetActive(false);
    }

    private void MostrarBarreira(float x)
    {
        if (barreira == null) return;
        barreira.transform.position = new Vector3(x + 0.5f, 10f, 0f);
        barreira.SetActive(true);
    }

    private void EsconderBarreira()
    {
        if (barreira != null)
            barreira.SetActive(false);
    }

    /// <summary>Textura 8x16: faixa clara com bordas suaves, para o "campo de forca".</summary>
    private static Sprite SpriteDeBrilho()
    {
        const int w = 8, h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float borda = Mathf.Min(x, w - 1 - x) / (w / 2f);     // 0 na borda, 1 no meio
                float listra = (y % 8) < 4 ? 1f : 0.7f;               // listras horizontais sutis
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(borda) * listra));
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
    }

    // ----------------- Visual -----------------

    private void LateUpdate()
    {
        // Barreira pulsando.
        if (barreira != null && barreira.activeSelf && brilhoDaBarreira != null)
        {
            Color c = corDaBarreira;
            c.a = corDaBarreira.a * (0.7f + 0.3f * Mathf.Sin(Time.time * 4f));
            brilhoDaBarreira.color = c;
        }

        // Balao seguindo a cabeca do jogador, com um leve balanco.
        if (jogador == null || balao == null || !balao.gameObject.activeSelf || canvasRect == null || Camera.main == null)
            return;

        Vector3 mundo = jogador.position + Vector3.up * alturaAcimaDoJogador;
        Vector2 tela = Camera.main.WorldToScreenPoint(mundo);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, tela, null, out Vector2 local))
            balao.anchoredPosition = local + Vector2.up * Mathf.Sin(Time.time * 3f) * 5f;
    }
}
