using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Pinta o chao de uma fase num Tilemap a partir de retangulos, escolhendo o
/// tile certo para cada posicao (canto, borda, superficie, recheio) com o
/// TileKit do bioma. E o equivalente automatico de pintar com o Tile Palette.
/// </summary>
public class TerrainPainter
{
    private const string PastaTiles = "Assets/_Game/Tiles";

    private readonly TileKit kit;
    private static readonly Dictionary<string, Tile> cache = new Dictionary<string, Tile>();

    public TerrainPainter(TileKit kit)
    {
        this.kit = kit;
    }

    // ----------------- Chao e plataformas -----------------

    /// <summary>
    /// Pinta um bloco solido. (x, y) e a celula do canto inferior esquerdo; a
    /// superficie fica na linha y + altura - 1.
    /// </summary>
    public void PintarChao(Tilemap mapa, RectInt r)
    {
        for (int linha = 0; linha < r.height; linha++)
        {
            int y = r.yMax - 1 - linha;
            Faixa faixa = FaixaDoChao(linha, r.height);

            for (int coluna = 0; coluna < r.width; coluna++)
                Colocar(mapa, r.x + coluna, y, Escolher(faixa, coluna, r.width, r.x + coluna, y));
        }
    }

    /// <summary>Plataforma flutuante com os tiles proprios do bioma (bordas arredondadas).</summary>
    public void PintarPlataforma(Tilemap mapa, int x, int y, int largura)
    {
        if (kit.plataformaTopo == null)
        {
            PintarChao(mapa, new RectInt(x, y, largura, kit.alturaPlataforma));
            return;
        }

        int yTopo = y + kit.alturaPlataforma - 1;

        for (int coluna = 0; coluna < largura; coluna++)
        {
            Colocar(mapa, x + coluna, yTopo, Escolher(kit.plataformaTopo, coluna, largura, x + coluna, yTopo));

            if (kit.alturaPlataforma >= 2 && kit.plataformaBaixo != null)
                Colocar(mapa, x + coluna, y, Escolher(kit.plataformaBaixo, coluna, largura, x + coluna, y));
        }
    }

    /// <summary>Faixa de perigo (lava). Pintada num tilemap separado, com colisor trigger.</summary>
    public void PintarPerigo(Tilemap mapa, RectInt r)
    {
        if (!kit.temPerigo)
            return;

        for (int coluna = 0; coluna < r.width; coluna++)
        {
            Colocar(mapa, r.x + coluna, r.yMax - 1, kit.perigoTopo);
            for (int y = r.y; y < r.yMax - 1; y++)
                Colocar(mapa, r.x + coluna, y, kit.perigoBaixo);
        }
    }

    // ----------------- Decoracao -----------------

    /// <summary>Carimba um desenho pronto (arvore, pilar) com o canto inferior esquerdo em (x, y).</summary>
    public void Carimbar(Tilemap mapa, int indiceDoCarimbo, int x, int y)
    {
        if (indiceDoCarimbo < 0 || indiceDoCarimbo >= kit.carimbos.Count)
            return;

        TileRef[,] carimbo = kit.carimbos[indiceDoCarimbo];
        int linhas = carimbo.GetLength(0);
        int colunas = carimbo.GetLength(1);

        // A matriz esta escrita de cima para baixo, como se le; o tilemap cresce para cima.
        for (int l = 0; l < linhas; l++)
            for (int c = 0; c < colunas; c++)
            {
                TileRef t = carimbo[l, c];
                if (t.folha != null)
                    Colocar(mapa, x + c, y + (linhas - 1 - l), t);
            }
    }

    /// <summary>Espalha tufos de grama/pedrinhas em cima do chao, a cada poucos tiles.</summary>
    public void DecorarSuperficie(Tilemap mapa, RectInt chao, int espacamento = 3)
    {
        if (kit.detalhesDeChao == null || kit.detalhesDeChao.Length == 0)
            return;

        for (int x = chao.x + 1; x < chao.xMax - 1; x++)
        {
            if (Hash(x, chao.yMax) % espacamento != 0)
                continue;

            TileRef detalhe = kit.detalhesDeChao[Hash(x * 7, chao.yMax) % kit.detalhesDeChao.Length];
            Colocar(mapa, x, chao.yMax, detalhe);
        }
    }

    // ----------------- Escolha de tiles -----------------

    private Faixa FaixaDoChao(int linhaAPartirDoTopo, int altura)
    {
        if (linhaAPartirDoTopo == 0)
            return kit.topo;

        bool ultimaLinha = linhaAPartirDoTopo == altura - 1;
        if (ultimaLinha && kit.fundo != null)
            return kit.fundo;

        int indice = linhaAPartirDoTopo - 1;
        if (indice < kit.linhas.Count)
            return kit.linhas[indice];

        if (kit.recheio != null)
            return new Faixa(kit.recheio, kit.recheio, kit.recheio, kit.recheio);

        return kit.linhas[kit.linhas.Count - 1];
    }

    private TileRef Escolher(Faixa faixa, int coluna, int largura, int x, int y)
    {
        TileRef[] opcoes;

        if (largura == 1)
            opcoes = faixa.unico ?? faixa.meio;
        else if (coluna == 0)
            opcoes = faixa.esq;
        else if (coluna == largura - 1)
            opcoes = faixa.dir;
        else
            opcoes = faixa.meio;

        // Em sequencia (neve): a coluna define a variante, e a onda emenda.
        // Sorteado (grama, rocha): variacao deterministica pela posicao, para
        // o mesmo mapa sair sempre igual.
        int indice = kit.variarEmSequencia
            ? ((x % opcoes.Length) + opcoes.Length) % opcoes.Length
            : Hash(x, y) % opcoes.Length;

        return opcoes[indice];
    }

    private static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663;
            return h < 0 ? -h : h;
        }
    }

    // ----------------- Tiles como assets -----------------

    private static void Colocar(Tilemap mapa, int x, int y, TileRef t)
    {
        Tile tile = TileDe(t);
        if (tile != null)
            mapa.SetTile(new Vector3Int(x, y, 0), tile);
    }

    /// <summary>
    /// Cria (uma vez) o asset Tile para um sprite da folha. Os tiles ficam em
    /// Assets/_Game/Tiles/&lt;folha&gt;/ e podem ser usados no Tile Palette depois.
    /// </summary>
    public static Tile TileDe(TileRef t)
    {
        if (t.folha == null)
            return null;

        string chave = t.folha + "#" + t.indice;
        if (cache.TryGetValue(chave, out Tile emCache) && emCache != null)
            return emCache;

        string nomeFolha = Path.GetFileNameWithoutExtension(t.folha).Replace(" (16x16)", "");
        string pastaDaFolha = PastaTiles + "/" + Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(t.folha))) + "_" + nomeFolha;
        string caminho = pastaDaFolha + "/tile_" + t.indice + ".asset";

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(caminho);
        if (tile == null)
        {
            Sprite sprite = AnimationBuilder.SpriteFatiado(t.folha, t.indice);
            if (sprite == null)
            {
                Debug.LogWarning("[Tiles] Sprite " + t.indice + " nao encontrado em " + t.folha);
                return null;
            }

            GarantirPasta(pastaDaFolha);

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            // Colisor da celula inteira: bordas arredondadas nao deixam buracos.
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, caminho);
        }

        cache[chave] = tile;
        return tile;
    }

    private static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho))
            return;

        string pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(pai))
            GarantirPasta(pai);

        AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
    }
}
