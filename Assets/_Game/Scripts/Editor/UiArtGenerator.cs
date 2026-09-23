using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera os sprites de interface que os packs nao trazem, em pixel art e no
/// mesmo espirito da arte do jogo: balao de fala, painel, botao e teclas.
/// Todos em 9-slice (menos as setas), para esticar sem deformar os cantos.
/// </summary>
public static class UiArtGenerator
{
    private const string Pasta = "Assets/_Game/Art/UI/Gerados";
    public const string CaminhoBalao = Pasta + "/balao.png";
    public const string CaminhoSeta = Pasta + "/balao_seta.png";
    public const string CaminhoPainel = Pasta + "/painel.png";
    public const string CaminhoBotao = Pasta + "/botao.png";
    public const string CaminhoTecla = Pasta + "/tecla.png";
    public const string CaminhoTeclaEsq = Pasta + "/tecla_esq.png";
    public const string CaminhoTeclaDir = Pasta + "/tecla_dir.png";
    public const string CaminhoTeclaE = Pasta + "/tecla_e.png";
    public const string CaminhoTeclaY = Pasta + "/tecla_y.png";

    private static readonly Color32 Nada = new Color32(0, 0, 0, 0);

    // Balao
    private static readonly Color32 Contorno = new Color32(59, 42, 26, 255);
    private static readonly Color32 Creme = new Color32(255, 246, 216, 255);
    private static readonly Color32 CremeSombra = new Color32(232, 216, 176, 255);
    private static readonly Color32 CremeBrilho = new Color32(255, 255, 240, 255);

    // Painel
    private static readonly Color32 Navy = new Color32(22, 26, 46, 255);
    private static readonly Color32 NavyClaro = new Color32(40, 46, 78, 255);

    // Botao
    private static readonly Color32 Verde = new Color32(62, 142, 78, 255);
    private static readonly Color32 VerdeClaro = new Color32(111, 207, 122, 255);
    private static readonly Color32 VerdeEscuro = new Color32(42, 97, 54, 255);

    // Tecla
    private static readonly Color32 TeclaTopo = new Color32(236, 236, 244, 255);
    private static readonly Color32 TeclaBrilho = new Color32(255, 255, 255, 255);
    private static readonly Color32 TeclaLado = new Color32(154, 154, 176, 255);
    private static readonly Color32 TeclaContorno = new Color32(44, 40, 60, 255);
    private static readonly Color32 TeclaTexto = new Color32(44, 40, 60, 255);

    public static void Garantir()
    {
        if (!AssetDatabase.IsValidFolder(Pasta))
            AssetDatabase.CreateFolder("Assets/_Game/Art/UI", "Gerados");

        if (!Existe(CaminhoBalao)) GerarBalao();
        if (!Existe(CaminhoSeta)) GerarSeta();
        if (!Existe(CaminhoPainel)) GerarPainel();
        if (!Existe(CaminhoBotao)) GerarBotao();
        if (!Existe(CaminhoTecla)) GerarTecla(CaminhoTecla, null);
        if (!Existe(CaminhoTeclaEsq)) GerarTecla(CaminhoTeclaEsq, "esq");
        if (!Existe(CaminhoTeclaDir)) GerarTecla(CaminhoTeclaDir, "dir");
        if (!Existe(CaminhoTeclaE)) GerarTecla(CaminhoTeclaE, "E");
        if (!Existe(CaminhoTeclaY)) GerarTecla(CaminhoTeclaY, "Y");

        AssetDatabase.Refresh();
    }

    private static bool Existe(string caminho) => AssetDatabase.LoadAssetAtPath<Sprite>(caminho) != null;

    // ----------------- Balao de fala -----------------

    private static void GerarBalao()
    {
        const int w = 48, h = 40, chanfro = 4;
        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);
                bool foraDoCanto = dx + dy < chanfro;
                bool bordaDoCanto = dx + dy < chanfro + 2;
                bool borda = dx < 2 || dy < 2;

                Color32 cor;
                if (foraDoCanto) cor = Nada;
                else if (borda || bordaDoCanto) cor = Contorno;
                else if (y == 2 || y == 3) cor = CremeSombra;
                else if (y == h - 3) cor = CremeBrilho;
                else cor = Creme;

                px[y * w + x] = cor;
            }

        Salvar(CaminhoBalao, w, h, px, new Vector4(12, 12, 12, 12));
    }

    private static void GerarSeta()
    {
        const int w = 16, h = 12;
        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            int meiaLargura = Mathf.RoundToInt((y + 1) * (w / 2f) / h);
            int centro = w / 2;

            for (int x = 0; x < w; x++)
            {
                int dist = Mathf.Abs(x - centro + (x >= centro ? 0 : 1)) + 1;
                bool dentro = dist <= meiaLargura;
                bool borda = dentro && (dist >= meiaLargura - 1 || y <= 1);
                px[y * w + x] = !dentro ? Nada : borda ? Contorno : Creme;
            }
        }

        Salvar(CaminhoSeta, w, h, px, Vector4.zero);
    }

    // ----------------- Painel escuro com borda creme -----------------

    private static void GerarPainel()
    {
        const int w = 48, h = 48, chanfro = 3;
        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);
                int d = Mathf.Min(dx, dy);

                Color32 cor;
                if (dx + dy < chanfro) cor = Nada;
                else if (d < 2 || dx + dy < chanfro + 2) cor = Creme;        // borda clara
                else if (d < 3) cor = Contorno;                                // linha escura interna
                else if (d < 5) cor = NavyClaro;                               // friso
                else cor = Navy;

                px[y * w + x] = cor;
            }

        Salvar(CaminhoPainel, w, h, px, new Vector4(12, 12, 12, 12));
    }

    // ----------------- Botao com bisel -----------------

    private static void GerarBotao()
    {
        const int w = 32, h = 20, chanfro = 2;
        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);

                Color32 cor;
                if (dx + dy < chanfro) cor = Nada;
                else if (dx < 2 || dy < 2 || dx + dy < chanfro + 2) cor = Contorno;
                else if (y >= h - 4) cor = VerdeClaro;     // brilho em cima
                else if (y < 5) cor = VerdeEscuro;         // sombra embaixo
                else cor = Verde;

                px[y * w + x] = cor;
            }

        Salvar(CaminhoBotao, w, h, px, new Vector4(6, 6, 6, 6));
    }

    // ----------------- Tecla (keycap) -----------------

    /// <param name="seta">null = tecla lisa (o rotulo e texto por cima); "esq"/"dir" = seta desenhada.</param>
    private static void GerarTecla(string caminho, string seta)
    {
        const int w = 24, h = 24, chanfro = 2;
        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);

                Color32 cor;
                if (dx + dy < chanfro) cor = Nada;
                else if (dx < 2 || dy < 2 || dx + dy < chanfro + 2) cor = TeclaContorno;
                else if (y < 6) cor = TeclaLado;            // lateral da tecla (3D)
                else if (y == 6) cor = TeclaContorno;       // separacao topo/lado
                else if (y >= h - 4) cor = TeclaBrilho;
                else cor = TeclaTopo;

                px[y * w + x] = cor;
            }

        if (seta == "E" || seta == "Y")
        {
            // Letra 5x7 desenhada no topo da tecla (aviso da loja, no mundo).
            string[] glifo = seta == "E"
                ? new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" }
                : new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" };

            for (int linha = 0; linha < glifo.Length; linha++)
                for (int col = 0; col < 5; col++)
                    if (glifo[linha][col] == '1')
                        Pintar(px, w, 10 + col, 16 - linha, TeclaTexto);
        }
        else if (seta != null)
        {
            // Seta de 9x7 no centro do topo da tecla (linhas 9..15).
            bool dir = seta == "dir";
            int cx = w / 2, cy = 13;
            for (int i = -4; i <= 4; i++)
            {
                int x = cx + (dir ? i : -i);
                Pintar(px, w, x, cy, TeclaTexto);               // haste
                if (i >= 1)
                {
                    int ponta = 4 - i;                           // triangulo
                    for (int k = -ponta; k <= ponta; k++)
                        Pintar(px, w, x, cy + k, TeclaTexto);
                }
            }
        }

        Salvar(caminho, w, h, px, seta == null ? new Vector4(8, 8, 8, 8) : Vector4.zero);
    }

    private static void Pintar(Color32[] px, int w, int x, int y, Color32 cor)
    {
        if (x >= 0 && x < w && y >= 0 && y < px.Length / w)
            px[y * w + x] = cor;
    }

    // ----------------- Salvar -----------------

    private static void Salvar(string caminho, int w, int h, Color32[] px, Vector4 borda)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(caminho, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

        TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (importador == null)
            return;

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = 16;
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;
        importador.spriteBorder = borda;   // (esq, baixo, dir, cima) do 9-slice
        importador.SaveAndReimport();
    }
}
