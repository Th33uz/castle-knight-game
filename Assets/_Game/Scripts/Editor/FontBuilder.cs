using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Cria os TMP_FontAssets a partir dos .ttf, para a interface nao cair na fonte
/// lisa padrao do TextMesh Pro.
///
/// Sao duas: a Press Start 2P no titulo, pelo ar de arcade, e a Kenney Pixel
/// Square em todo o resto. A troca no corpo do texto nao e so estetica - a Press
/// Start 2P nao tem o til, entao "ACAO" e "CREDITOS" viviam sem acento; a Kenney
/// desenha o portugues inteiro.
/// </summary>
public static class FontBuilder
{
    private const string Pasta = "Assets/_Game/Fonts";

    private const string TtfTitulo = Pasta + "/PressStart2P-Regular.ttf";
    private const string AssetTitulo = Pasta + "/PressStart2P SDF.asset";

    private const string TtfTexto = Pasta + "/KenneyPixelSquare.ttf";
    private const string AssetTexto = Pasta + "/KenneyPixelSquare SDF.asset";

    /// <summary>Fonte do corpo do texto: botoes, tutorial, loja, creditos.</summary>
    public static TMP_FontAsset Garantir()
    {
        return Criar(TtfTexto, AssetTexto, "KenneyPixelSquare SDF")
            ?? Criar(TtfTitulo, AssetTitulo, "PressStart2P SDF");
    }

    /// <summary>Fonte do titulo do jogo e dos cabecalhos de tela.</summary>
    public static TMP_FontAsset GarantirTitulo()
    {
        return Criar(TtfTitulo, AssetTitulo, "PressStart2P SDF") ?? Garantir();
    }

    private static TMP_FontAsset Criar(string caminhoTtf, string caminhoAsset, string nome)
    {
        TMP_FontAsset existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(caminhoAsset);
        if (existente != null)
            return existente;

        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(caminhoTtf);
        if (ttf == null)
        {
            Debug.LogWarning("[Setup] Fonte " + caminhoTtf + " nao encontrada.");
            return null;
        }

        // Atlas dinamico: os glifos sao gerados conforme aparecem no texto, entao
        // nao precisa listar os caracteres de antemao.
        TMP_FontAsset fonte = TMP_FontAsset.CreateFontAsset(
            ttf, 32, 4, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);

        if (fonte == null)
        {
            Debug.LogWarning("[Setup] Nao consegui gerar o TMP_FontAsset de " + nome + ".");
            return null;
        }

        fonte.name = nome;
        AssetDatabase.CreateAsset(fonte, caminhoAsset);

        // Material e atlas precisam virar sub-assets, senao somem ao recarregar.
        fonte.material.name = fonte.name + " Material";
        AssetDatabase.AddObjectToAsset(fonte.material, fonte);

        if (fonte.atlasTextures != null && fonte.atlasTextures.Length > 0 && fonte.atlasTextures[0] != null)
        {
            fonte.atlasTextures[0].name = fonte.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fonte.atlasTextures[0], fonte);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Fonte criada em " + caminhoAsset);
        return fonte;
    }
}
