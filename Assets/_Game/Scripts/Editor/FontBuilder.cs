using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Cria o TMP_FontAsset da fonte pixel (Press Start 2P) a partir do .ttf, para a
/// interface nao usar a fonte padrao lisa do TextMesh Pro.
/// </summary>
public static class FontBuilder
{
    private const string CaminhoTtf = "Assets/_Game/Fonts/PressStart2P-Regular.ttf";
    private const string CaminhoAsset = "Assets/_Game/Fonts/PressStart2P SDF.asset";

    /// <summary>Devolve a fonte pixel, criando o asset na primeira vez. Null se o .ttf nao existir.</summary>
    public static TMP_FontAsset Garantir()
    {
        TMP_FontAsset existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoAsset);
        if (existente != null)
            return existente;

        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(CaminhoTtf);
        if (ttf == null)
        {
            Debug.LogWarning("[Setup] Fonte " + CaminhoTtf + " nao encontrada; a UI usa a fonte padrao.");
            return null;
        }

        // Atlas dinamico: os glifos sao gerados conforme aparecem no texto, entao
        // nao precisa listar os caracteres de antemao.
        TMP_FontAsset fonte = TMP_FontAsset.CreateFontAsset(
            ttf, 32, 4, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);

        if (fonte == null)
        {
            Debug.LogWarning("[Setup] Nao consegui gerar o TMP_FontAsset da fonte pixel.");
            return null;
        }

        fonte.name = "PressStart2P SDF";
        AssetDatabase.CreateAsset(fonte, CaminhoAsset);

        // Material e atlas precisam virar sub-assets, senao somem ao recarregar.
        fonte.material.name = fonte.name + " Material";
        AssetDatabase.AddObjectToAsset(fonte.material, fonte);

        if (fonte.atlasTextures != null && fonte.atlasTextures.Length > 0 && fonte.atlasTextures[0] != null)
        {
            fonte.atlasTextures[0].name = fonte.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fonte.atlasTextures[0], fonte);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Fonte pixel criada em " + CaminhoAsset);
        return fonte;
    }
}
