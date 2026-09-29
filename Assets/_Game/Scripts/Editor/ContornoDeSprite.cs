using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera copias dos sprites com um contorno de 1 px em volta.
///
/// O heroi (pack rvros) vem sem contorno, enquanto o gato e os bichos dos packs
/// do ansimuz vem com. Lado a lado a diferenca salta: o gato tem silhueta
/// fechada e o heroi se mistura com o fundo. Em vez de shader, o contorno e
/// desenhado no PNG - assim vale para qualquer pipeline e nao custa nada em
/// tempo de jogo.
///
/// O tamanho da imagem nao muda: o contorno ocupa os pixels transparentes que
/// encostam no desenho. Se a figura chegasse na borda do quadro o contorno seria
/// cortado ali, mas os quadros do heroi tem folga de sobra.
/// </summary>
public static class ContornoDeSprite
{
    /// <summary>Escuro e levemente arroxeado, no tom dos contornos dos outros packs.</summary>
    private static readonly Color32 CorDoContorno = new Color32(24, 18, 32, 255);

    /// <summary>
    /// Copia cada PNG de <paramref name="origem"/> para <paramref name="destino"/>
    /// com contorno. Pula os que ja existem, para nao reimportar tudo a cada
    /// reconstrucao; apague a pasta de destino para refazer.
    /// </summary>
    public static void Garantir(string origem, string destino)
    {
        if (!AssetDatabase.IsValidFolder(origem))
        {
            Debug.LogWarning("[Contorno] Pasta nao encontrada: " + origem);
            return;
        }

        if (!AssetDatabase.IsValidFolder(destino))
            AssetDatabase.CreateFolder(origem, Path.GetFileName(destino));

        int criados = 0;

        foreach (string arquivo in Directory.GetFiles(origem, "*.png"))
        {
            string saida = destino + "/" + Path.GetFileName(arquivo);
            if (File.Exists(saida))
                continue;

            if (Contornar(arquivo, saida))
                criados++;
        }

        if (criados > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log("[Contorno] " + criados + " sprites com contorno em " + destino);
        }
    }

    private static bool Contornar(string entrada, string saida)
    {
        // Carrega pelos bytes, e nao pelo AssetDatabase: assim nao depende da
        // textura estar marcada como legivel no importador.
        Texture2D textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!ImageConversion.LoadImage(textura, File.ReadAllBytes(entrada)))
        {
            Object.DestroyImmediate(textura);
            return false;
        }

        int largura = textura.width;
        int altura = textura.height;
        Color32[] px = textura.GetPixels32();
        Color32[] saidaPx = (Color32[])px.Clone();

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                int i = y * largura + x;

                // O contorno so ocupa o vazio ao redor do desenho.
                if (px[i].a > 8)
                    continue;

                if (TemVizinhoOpaco(px, largura, altura, x, y))
                    saidaPx[i] = CorDoContorno;
            }
        }

        textura.SetPixels32(saidaPx);
        textura.Apply();
        File.WriteAllBytes(saida, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        return true;
    }

    /// <summary>Olha as 8 direcoes: sem as diagonais o contorno abre nos cantos.</summary>
    private static bool TemVizinhoOpaco(Color32[] px, int largura, int altura, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || ny < 0 || nx >= largura || ny >= altura)
                    continue;

                if (px[ny * largura + nx].a > 8)
                    return true;
            }
        }

        return false;
    }
}
