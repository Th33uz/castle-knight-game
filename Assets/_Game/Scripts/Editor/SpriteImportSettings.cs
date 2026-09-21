using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Configura automaticamente toda textura colocada em Assets/_Game/Art.
///
/// Sem isso seria preciso ajustar na mao, arquivo por arquivo: Pixels Per Unit,
/// Filter Mode e o fatiamento das folhas de sprite. O pack de arte tem 173 PNGs.
///
/// Arquivos cujo nome termina em "(LxA)", como "Run (32x32).png", sao fatiados
/// em grade automaticamente, que e o padrao de nome usado pelo Pixel Adventure.
/// </summary>
public class SpriteImportSettings : AssetPostprocessor
{
    private const string PastaDeArte = "Assets/_Game/Art";

    // 16 porque o tileset do pack usa tiles de 16x16: assim um tile equivale a
    // exatamente uma unidade do mundo e uma celula do Grid, o que torna a
    // montagem das fases previsivel. Os personagens, de 32x32, ficam com dois
    // tiles de altura.
    private const int PixelsPorUnidade = 16;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PastaDeArte))
            return;

        TextureImporter importador = (TextureImporter)assetImporter;

        // So mexe na primeira importacao. Depois disso o arquivo .meta existe e
        // os ajustes manuais do usuario devem ser respeitados.
        if (!importador.importSettingsMissing)
            return;

        importador.textureType = TextureImporterType.Sprite;
        importador.spritePixelsPerUnit = PixelsPorUnidade;
        importador.filterMode = FilterMode.Point;          // pixel art nitida
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;

        // Full Rect e obrigatorio para o Draw Mode "Tiled" do SpriteRenderer,
        // usado nos fundos de parallax que se repetem ao longo da fase.
        TextureImporterSettings configuracoes = new TextureImporterSettings();
        importador.ReadTextureSettings(configuracoes);
        configuracoes.spriteMeshType = SpriteMeshType.FullRect;
        configuracoes.spriteExtrude = 0;
        importador.SetTextureSettings(configuracoes);

        AplicarFatiamento(importador);
    }

    /// <summary>
    /// Se o nome do arquivo trouxer o tamanho do quadro, fatia a folha em grade.
    /// Caso contrario, forca UM sprite so: o template 2D do Unity fatia
    /// automaticamente qualquer imagem com partes separadas por transparencia
    /// (os dois triangulos do espinho viravam dois sprites de 8x8).
    /// </summary>
    private void AplicarFatiamento(TextureImporter importador)
    {
        string nomeBase = Path.GetFileNameWithoutExtension(assetPath);

        if (!TentarLerTamanhoDoQuadro(nomeBase, out int larguraDoQuadro, out int alturaDoQuadro))
        {
            importador.spriteImportMode = SpriteImportMode.Single;
            return;
        }

        // As dimensoes vem do cabecalho do PNG: dentro do OnPreprocessTexture a
        // textura ainda nao foi gerada, entao nao da para consultar Texture2D.
        if (!TentarLerTamanhoDoPng(assetPath, out int largura, out int altura))
            return;

        int colunas = largura / larguraDoQuadro;
        int linhas = altura / alturaDoQuadro;

        // Uma imagem de um quadro so nao e folha de animacao: continua Single.
        if (colunas <= 0 || linhas <= 0 || (colunas == 1 && linhas == 1))
            return;

        importador.spriteImportMode = SpriteImportMode.Multiple;

        // O fatiamento passa pelo ISpriteEditorDataProvider. A antiga
        // TextureImporter.spritesheet foi removida no Unity 6: ainda compila,
        // mas nao tem mais efeito nenhum.
        SpriteDataProviderFactories fabrica = new SpriteDataProviderFactories();
        fabrica.Init();

        ISpriteEditorDataProvider provedor = fabrica.GetSpriteEditorDataProviderFromObject(importador);
        if (provedor == null)
            return;

        provedor.InitSpriteEditorDataProvider();

        List<SpriteRect> quadros = new List<SpriteRect>();

        // A origem do Unity fica embaixo a esquerda, mas as folhas sao lidas de
        // cima para baixo: por isso a linha e invertida no calculo do Y.
        for (int linha = 0; linha < linhas; linha++)
        {
            for (int coluna = 0; coluna < colunas; coluna++)
            {
                quadros.Add(new SpriteRect
                {
                    name = nomeBase + "_" + (linha * colunas + coluna),
                    spriteID = GUID.Generate(),
                    rect = new Rect(
                        coluna * larguraDoQuadro,
                        altura - (linha + 1) * alturaDoQuadro,
                        larguraDoQuadro,
                        alturaDoQuadro),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }
        }

        provedor.SetSpriteRects(quadros.ToArray());

        // Registrar nome e GUID mantem as referencias dos prefabs funcionando
        // quando a textura for reimportada.
        ISpriteNameFileIdDataProvider provedorDeNomes =
            provedor.GetDataProvider<ISpriteNameFileIdDataProvider>();

        if (provedorDeNomes != null)
        {
            List<SpriteNameFileIdPair> pares = new List<SpriteNameFileIdPair>();

            foreach (SpriteRect quadro in quadros)
                pares.Add(new SpriteNameFileIdPair(quadro.name, quadro.spriteID));

            provedorDeNomes.SetNameFileIdPairs(pares);
        }

        provedor.Apply();
    }

    /// <summary>
    /// Corrige imagens ja importadas que o fatiamento automatico do template
    /// dividiu em varios sprites sem o nome pedir isso: volta para sprite unico.
    /// Chamado pelo "Reconstruir tudo" e disponivel no menu.
    /// </summary>
    [MenuItem("Jogo/Corrigir/Sprites fatiados por engano")]
    public static int CorrigirFatiamentoAutomatico()
    {
        int corrigidos = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PastaDeArte }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            string nome = Path.GetFileNameWithoutExtension(caminho);

            // Folhas com "(LxA)" no nome devem mesmo ser Multiple.
            if (TentarLerTamanhoDoQuadro(nome, out _, out _))
                continue;

            TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importador == null || importador.spriteImportMode != SpriteImportMode.Multiple)
                continue;

            importador.spriteImportMode = SpriteImportMode.Single;
            importador.SaveAndReimport();
            corrigidos++;
        }

        Debug.Log("[Setup] " + corrigidos + " imagens voltaram a ser sprite unico.");
        return corrigidos;
    }

    /// <summary>Extrai 32 e 32 de um nome como "Run (32x32)".</summary>
    private static bool TentarLerTamanhoDoQuadro(string nome, out int largura, out int altura)
    {
        largura = 0;
        altura = 0;

        int abre = nome.LastIndexOf('(');
        int fecha = nome.LastIndexOf(')');

        if (abre < 0 || fecha < abre)
            return false;

        string conteudo = nome.Substring(abre + 1, fecha - abre - 1);
        string[] partes = conteudo.Split('x');

        if (partes.Length != 2)
            return false;

        return int.TryParse(partes[0].Trim(), out largura)
            && int.TryParse(partes[1].Trim(), out altura)
            && largura > 0 && altura > 0;
    }

    /// <summary>
    /// Le largura e altura direto do cabecalho IHDR do PNG, que fica logo apos
    /// os 8 bytes de assinatura, em big-endian.
    /// </summary>
    private static bool TentarLerTamanhoDoPng(string caminho, out int largura, out int altura)
    {
        largura = 0;
        altura = 0;

        try
        {
            using (FileStream arquivo = File.OpenRead(caminho))
            {
                byte[] cabecalho = new byte[24];

                if (arquivo.Read(cabecalho, 0, 24) < 24)
                    return false;

                // Confere a assinatura PNG antes de confiar nos bytes seguintes.
                if (cabecalho[0] != 0x89 || cabecalho[1] != 0x50
                    || cabecalho[2] != 0x4E || cabecalho[3] != 0x47)
                    return false;

                largura = (cabecalho[16] << 24) | (cabecalho[17] << 16)
                    | (cabecalho[18] << 8) | cabecalho[19];
                altura = (cabecalho[20] << 24) | (cabecalho[21] << 16)
                    | (cabecalho[22] << 8) | cabecalho[23];

                return largura > 0 && altura > 0;
            }
        }
        catch (IOException)
        {
            return false;
        }
    }
}
