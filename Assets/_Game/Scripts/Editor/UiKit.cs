using UnityEditor;
using UnityEngine;

/// <summary>
/// Sprites de interface do pack Kenney (UI Pack - Pixel Adventure e Input
/// Prompts, os dois CC0).
///
/// As molduras sao 32x32 e precisam de 9-slice: sem a borda declarada, o Unity
/// estica a imagem inteira e os cantos metalicos viram um borrao. O numero e a
/// espessura da moldura em pixels, medida na arte.
/// </summary>
public static class UiKit
{
    private const string Pasta = "Assets/_Game/Art/UI/Kenney";
    private const string PastaTeclas = Pasta + "/Teclas";

    public const string Caixa = Pasta + "/caixa.png";       // madeira com cantos de metal
    public const string Botao = Pasta + "/botao.png";       // creme, contrasta com a caixa
    public const string Moldura = Pasta + "/moldura.png";   // vazada, para o botao em foco
    public const string Balao = Pasta + "/balao.png";       // caixa clara, para o balao do tutorial
    public const string BarraEsq = Pasta + "/barra_esq.png";
    public const string BarraMeio = Pasta + "/barra_meio.png";
    public const string BarraDir = Pasta + "/barra_dir.png";

    // Teclado
    public const string TeclaEspaco = PastaTeclas + "/tecla_space.png";
    public const string TeclaE = PastaTeclas + "/tecla_e.png";
    public const string TeclaL = PastaTeclas + "/tecla_l.png";
    public const string TeclaEsc = PastaTeclas + "/tecla_escape.png";
    public const string TeclaEnter = PastaTeclas + "/tecla_enter.png";
    public const string TeclaA = PastaTeclas + "/tecla_a.png";
    public const string TeclaD = PastaTeclas + "/tecla_d.png";
    public const string TeclaW = PastaTeclas + "/tecla_w.png";
    public const string TeclaS = PastaTeclas + "/tecla_s.png";
    public const string TeclaSetaEsq = PastaTeclas + "/tecla_seta_left.png";
    public const string TeclaSetaDir = PastaTeclas + "/tecla_seta_right.png";
    public const string TeclaSetaCima = PastaTeclas + "/tecla_seta_up.png";
    public const string TeclaSetaBaixo = PastaTeclas + "/tecla_seta_down.png";

    // Controle (Xbox)
    public const string BotaoA = PastaTeclas + "/xbox_a.png";
    public const string BotaoB = PastaTeclas + "/xbox_b.png";
    public const string BotaoY = PastaTeclas + "/xbox_y.png";
    public const string BotaoStart = PastaTeclas + "/xbox_start.png";
    public const string Direcional = PastaTeclas + "/xbox_dpad.png";
    public const string Analogico = PastaTeclas + "/xbox_analogico.png";

    /// <summary>Roda junto do UiArtGenerator, antes de montar as telas.</summary>
    public static void Garantir()
    {
        DefinirBorda(Caixa, 12f);     // os cantos de metal sao grandes
        DefinirBorda(Balao, 12f);
        DefinirBorda(Botao, 8f);
        DefinirBorda(Moldura, 12f);
        DefinirBorda(BarraMeio, 6f);
    }

    private static void DefinirBorda(string caminho, float px)
    {
        TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (importador == null)
        {
            Debug.LogWarning("[UiKit] Sprite nao encontrado: " + caminho);
            return;
        }

        Vector4 borda = new Vector4(px, px, px, px);
        if (importador.spriteBorder == borda && importador.spriteImportMode == SpriteImportMode.Single)
            return;

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spriteBorder = borda;
        importador.SaveAndReimport();
    }
}
