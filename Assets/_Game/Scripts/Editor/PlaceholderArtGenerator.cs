using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera sprites provisorios (retangulos coloridos) para o jogo ja rodar antes
/// de baixar o pack de arte definitivo.
///
/// Depois, para trocar a arte, basta apontar o campo Sprite de cada prefab
/// para o sprite novo: nenhum script precisa ser alterado.
/// </summary>
public static class PlaceholderArtGenerator
{
    // Precisa bater com o valor usado em SpriteImportSettings, senao os sprites
    // provisorios saem com escala diferente da arte definitiva.
    public const int PixelsPorUnidade = 16;
    private const string PastaDestino = "Assets/_Game/Art/Placeholder";

    /// <summary>Cria todos os sprites provisorios e devolve o caminho da pasta.</summary>
    public static void GerarTodos()
    {
        GarantirPasta();

        // Tamanhos em pixels. Com 16 pixels por unidade, o tile de 16x16 ocupa
        // exatamente uma unidade e o jogador, de 24x32, fica com dois tiles de
        // altura, na mesma proporcao da arte definitiva.
        Gerar("player", 24, 32, new Color32(90, 175, 255, 255), new Color32(30, 90, 160, 255));
        Gerar("inimigo", 30, 24, new Color32(220, 80, 80, 255), new Color32(130, 30, 30, 255));
        Gerar("tile", 16, 16, new Color32(110, 90, 70, 255), new Color32(70, 55, 40, 255));
        Gerar("plataforma", 48, 16, new Color32(150, 110, 70, 255), new Color32(90, 65, 40, 255));
        Gerar("espinho", 16, 16, new Color32(230, 140, 60, 255), new Color32(140, 70, 20, 255));
        Gerar("checkpoint", 16, 32, new Color32(90, 220, 120, 255), new Color32(40, 130, 70, 255));
        Gerar("bandeira", 32, 32, new Color32(240, 220, 90, 255), new Color32(160, 130, 30, 255));
        GerarCirculo("moeda", 16, new Color32(255, 210, 60, 255), new Color32(180, 130, 20, 255));

        AssetDatabase.Refresh();
        Debug.Log("[Setup] Sprites provisorios criados em " + PastaDestino);
    }

    public static Sprite Carregar(string nome)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PastaDestino + "/" + nome + ".png");
    }

    private static void GarantirPasta()
    {
        if (!AssetDatabase.IsValidFolder(PastaDestino))
            AssetDatabase.CreateFolder("Assets/_Game/Art", "Placeholder");
    }

    /// <summary>Retangulo preenchido com borda de 2px, para dar contorno visivel.</summary>
    private static void Gerar(string nome, int largura, int altura, Color preenchimento, Color borda)
    {
        Color[] pixels = new Color[largura * altura];

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                bool naBorda = x < 2 || y < 2 || x >= largura - 2 || y >= altura - 2;
                pixels[y * largura + x] = naBorda ? borda : preenchimento;
            }
        }

        Salvar(nome, largura, altura, pixels);
    }

    private static void GerarCirculo(string nome, int tamanho, Color preenchimento, Color borda)
    {
        Color[] pixels = new Color[tamanho * tamanho];
        float centro = (tamanho - 1) / 2f;
        float raio = tamanho / 2f;

        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro));

                Color cor;
                if (distancia > raio)
                    cor = Color.clear;            // fora do circulo: transparente
                else if (distancia > raio - 2f)
                    cor = borda;
                else
                    cor = preenchimento;

                pixels[y * tamanho + x] = cor;
            }
        }

        Salvar(nome, tamanho, tamanho, pixels);
    }

    private static void Salvar(string nome, int largura, int altura, Color[] pixels)
    {
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        textura.SetPixels(pixels);
        textura.Apply();

        string caminho = PastaDestino + "/" + nome + ".png";
        File.WriteAllBytes(caminho, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);

        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
        ConfigurarImportacao(caminho);
    }

    /// <summary>
    /// Ajusta a importacao para pixel art: sem filtro, sem compressao e com
    /// o mesmo Pixels Per Unit usado pelo resto do jogo.
    /// </summary>
    private static void ConfigurarImportacao(string caminho)
    {
        TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (importador == null)
            return;

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = PixelsPorUnidade;
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;

        // Full Rect e obrigatorio para o modo Tiled do SpriteRenderer, usado
        // pelas plataformas que repetem a textura em vez de estica-la.
        TextureImporterSettings configuracoes = new TextureImporterSettings();
        importador.ReadTextureSettings(configuracoes);
        configuracoes.spriteMeshType = SpriteMeshType.FullRect;
        configuracoes.spriteExtrude = 0;
        importador.SetTextureSettings(configuracoes);

        importador.SaveAndReimport();
    }
}
