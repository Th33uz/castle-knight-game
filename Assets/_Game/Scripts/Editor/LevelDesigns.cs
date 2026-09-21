using System.Collections.Generic;
using UnityEngine;

/// <summary>Um objeto colocado na fase: prefab, posicao e um parametro extra (ex.: deslocamento de plataforma).</summary>
public struct Objeto
{
    public string prefab;
    public Vector2 posicao;      // se "noChao" for true, e a posicao dos PES; senao, o centro
    public bool noChao;
    public Vector2 extra;
    public bool paraEsquerda;
}

public struct Carimbo
{
    public int indice, x, y;     // indice do carimbo no TileKit; (x, y) canto inferior esquerdo
}

public struct Prop
{
    public string sprite;        // caminho do PNG
    public Vector2 pe;           // posicao da base do sprite
    public int ordem;
}

/// <summary>Chefe do fim da fase e onde a arena comeca (a parede fecha 2 tiles antes).</summary>
public struct ChefeDef
{
    public string prefab;
    public float x, yChao, inicioArena;
}

/// <summary>
/// Definicao completa de uma fase. As medidas sao em tiles (1 tile = 1 unidade).
/// O chao e descrito por retangulos; o pintor escolhe os tiles certos.
/// </summary>
public class NivelDef
{
    public string cena;
    public Bioma bioma;
    public string musica;                 // nome do arquivo em Assets/_Game/Audio/Music, sem extensao
    public int largura;

    // Cartao de apresentacao e selecao de fases.
    public string titulo = "FASE";
    public string subtitulo = "";
    public string icone;                  // caminho do PNG mostrado no cartao e no menu
    public Vector2 nascimento;            // posicao dos pes do jogador
    public Color corDoCeu = Color.black;

    public List<RectInt> chao = new List<RectInt>();
    public List<RectInt> plataformas = new List<RectInt>();   // (x, y, largura, 1): y e a linha de baixo
    public List<RectInt> perigos = new List<RectInt>();       // lava (so caverna)
    public List<Objeto> objetos = new List<Objeto>();
    public List<Carimbo> carimbos = new List<Carimbo>();
    public List<Prop> props = new List<Prop>();
    public ChefeDef? chefe;

    // ----------------- Atalhos de escrita -----------------

    public NivelDef Chao(int x, int largura, int altura = 4)
    {
        chao.Add(new RectInt(x, 0, largura, altura));
        return this;
    }

    public NivelDef Bloco(int x, int y, int largura, int altura)
    {
        chao.Add(new RectInt(x, y, largura, altura));
        return this;
    }

    public NivelDef Plataforma(int x, int y, int largura)
    {
        plataformas.Add(new RectInt(x, y, largura, 1));
        return this;
    }

    public NivelDef Lava(int x, int largura, int altura = 2)
    {
        perigos.Add(new RectInt(x, 0, largura, altura));
        return this;
    }

    /// <summary>Objeto apoiado no chao: (x, yChao) e onde os pes encostam.</summary>
    public NivelDef NoChao(string prefab, float x, float yChao, bool paraEsquerda = false)
    {
        objetos.Add(new Objeto { prefab = prefab, posicao = new Vector2(x, yChao), noChao = true, paraEsquerda = paraEsquerda });
        return this;
    }

    /// <summary>Objeto no ar, pelo centro: inimigos voadores, serras, itens.</summary>
    public NivelDef NoAr(string prefab, float x, float y, Vector2 extra = default)
    {
        objetos.Add(new Objeto { prefab = prefab, posicao = new Vector2(x, y), noChao = false, extra = extra });
        return this;
    }

    /// <summary>Fila de itens iguais, espacados.</summary>
    public NivelDef Fila(string prefab, float x, float y, int quantidade, float espacamento = 1.2f)
    {
        for (int i = 0; i < quantidade; i++)
            NoAr(prefab, x + i * espacamento, y);
        return this;
    }

    public NivelDef Movel(string prefab, float x, float y, Vector2 deslocamento)
    {
        return NoAr(prefab, x, y, deslocamento);
    }

    public NivelDef Carimbar(int indice, int x, int y)
    {
        carimbos.Add(new Carimbo { indice = indice, x = x, y = y });
        return this;
    }

    public NivelDef Enfeite(string sprite, float x, float yChao, int ordem = 5)
    {
        props.Add(new Prop { sprite = sprite, pe = new Vector2(x, yChao), ordem = ordem });
        return this;
    }

    public NivelDef Chefe(string prefab, float x, float yChao, float inicioArena)
    {
        chefe = new ChefeDef { prefab = prefab, x = x, yChao = yChao, inicioArena = inicioArena };
        return this;
    }
}

/// <summary>
/// As quatro fases do jogo. Alterar uma fase e editar a lista aqui e rodar
/// "Jogo > Reconstruir tudo".
/// </summary>
public static class LevelDesigns
{
    private const string PropsFloresta = "Assets/_Game/Art/SunnyLand/Props";
    private const string PropsCaverna = "Assets/_Game/Art/Grotto/Props";

    public static readonly string[] OrdemDasCenas = { "MainMenu", "Tutorial", "Fase1", "Fase2", "Fase3" };

    public static List<NivelDef> Todas()
    {
        return new List<NivelDef> { Tutorial(), Fase1(), Fase2(), Fase3() };
    }

    public static NivelDef PorNome(string cena)
    {
        foreach (NivelDef n in Todas())
            if (n.cena == cena)
                return n;
        return null;
    }

    // =====================================================================
    // TUTORIAL - floresta: pular, pegar diamante, pisar, curar e usar a espada
    // =====================================================================
    private static NivelDef Tutorial()
    {
        var n = new NivelDef
        {
            cena = "Tutorial", bioma = Bioma.Floresta, musica = "floresta", largura = 112,
            nascimento = new Vector2(3f, 4f), corDoCeu = new Color(0.36f, 0.78f, 0.94f),
            titulo = "TUTORIAL", subtitulo = "APRENDA A JOGAR", icone = PropsFloresta + "/sign.png"
        };

        n.Chao(0, 28).Chao(31, 20).Chao(56, 28).Chao(87, 25);

        n.Plataforma(10, 7, 4).Plataforma(18, 9, 3).Plataforma(38, 7, 4).Plataforma(44, 10, 3).Plataforma(64, 8, 5)
         .Plataforma(92, 7, 3).Plataforma(98, 9, 4);

        // Diamantes sao a moeda: mostram o caminho e recompensam cada plataforma.
        n.Fila("Gema", 6f, 5.5f, 3).Fila("Gema", 10.5f, 8.5f, 4).Fila("Gema", 18.5f, 10.6f, 3)
         .Fila("Gema", 24f, 5.5f, 4).Fila("Gema", 38.5f, 8.5f, 4).Fila("Gema", 44.5f, 11.6f, 3)
         .Fila("Gema", 52f, 5.5f, 4).Fila("Gema", 64.5f, 9.5f, 5).Fila("Gema", 71f, 5.5f, 4)
         .Fila("Gema", 92.5f, 8.5f, 3).Fila("Gema", 98.5f, 10.6f, 4).Fila("Gema", 103f, 5.5f, 4);

        // Frutas curam um coracao e so podem ser pegas machucado.
        n.NoAr("Cereja", 37f, 5.6f).NoAr("Cereja", 60f, 12.6f).NoAr("Cereja", 106f, 5.6f);

        // Inimigos e armadilhas, um de cada tipo. Os dois ultimos gambas estao
        // num trecho plano e apertado: a hora de usar a espada.
        n.NoChao("Gamba", 22f, 4f).NoChao("Gamba", 42f, 4f, paraEsquerda: true)
         .NoChao("Espinhos", 34f, 4f).NoChao("Espinhos", 35f, 4f)
         .NoAr("Aguia", 72f, 9f)
         .NoChao("Trampolim", 60f, 4f)
         .NoChao("Gamba", 95f, 4f).NoChao("Gamba", 101f, 4f, paraEsquerda: true);

        n.NoChao("Checkpoint", 48f, 4f).NoChao("Checkpoint", 89f, 4f).NoChao("FimDeFase", 108f, 4f);

        // Loja do raposo logo depois do checkpoint, antes dos dois gambas.
        n.NoChao("Loja", 91.5f, 4f);

        n.Enfeite(PropsFloresta + "/sign.png", 5f, 4f, 6)
         .Enfeite(PropsFloresta + "/tree.png", 8f, 4f, 3).Enfeite(PropsFloresta + "/bush.png", 14f, 4f, 6)
         .Enfeite(PropsFloresta + "/tree.png", 36f, 4f, 3).Enfeite(PropsFloresta + "/shrooms.png", 40f, 4f, 6)
         .Enfeite(PropsFloresta + "/bush.png", 52f, 4f, 6).Enfeite(PropsFloresta + "/tree.png", 70f, 4f, 3)
         .Enfeite(PropsFloresta + "/house.png", 76f, 4f, 4)
         .Enfeite(PropsFloresta + "/tree.png", 97f, 4f, 3)
         .Enfeite(PropsFloresta + "/bush.png", 105f, 4f, 6);

        return n;
    }

    // =====================================================================
    // FASE 1 - floresta, mais buracos e plataformas; chefe: Gamba Rei
    // =====================================================================
    private static NivelDef Fase1()
    {
        var n = new NivelDef
        {
            cena = "Fase1", bioma = Bioma.Floresta, musica = "floresta", largura = 200,
            nascimento = new Vector2(3f, 4f), corDoCeu = new Color(0.36f, 0.78f, 0.94f),
            titulo = "FASE 1", subtitulo = "FLORESTA", icone = PropsFloresta + "/tree.png"
        };

        n.Chao(0, 20).Chao(23, 12).Chao(38, 16, 6).Chao(57, 10).Chao(75, 22).Chao(100, 30)
         .Chao(133, 14).Chao(150, 12, 6).Chao(165, 35);

        n.Plataforma(8, 7, 3).Plataforma(13, 9, 3).Plataforma(18, 11, 4)
         .Plataforma(26, 8, 3).Plataforma(31, 10, 4)
         .Plataforma(60, 8, 3).Plataforma(66, 10, 3).Plataforma(70, 12, 4)
         .Plataforma(80, 8, 4).Plataforma(88, 9, 4).Plataforma(104, 8, 3).Plataforma(110, 10, 3).Plataforma(116, 8, 3)
         .Plataforma(124, 8, 4).Plataforma(136, 8, 3).Plataforma(141, 10, 3).Plataforma(153, 10, 4).Plataforma(158, 12, 3);

        // Diamantes (moeda)
        n.Fila("Gema", 8.5f, 8.5f, 3).Fila("Gema", 13.5f, 10.5f, 3).Fila("Gema", 18.5f, 12.6f, 4)
         .Fila("Gema", 26.5f, 9.5f, 3).Fila("Gema", 31.5f, 11.5f, 4).Fila("Gema", 40f, 7.5f, 6).Fila("Gema", 46f, 7.5f, 5)
         .Fila("Gema", 60.5f, 9.5f, 3).Fila("Gema", 66.5f, 11.5f, 3).Fila("Gema", 70.5f, 13.6f, 4)
         .Fila("Gema", 80.5f, 9.5f, 4).Fila("Gema", 88.5f, 10.5f, 4).Fila("Gema", 94f, 5.5f, 5)
         .Fila("Gema", 104.5f, 9.5f, 3).Fila("Gema", 110.5f, 11.6f, 3).Fila("Gema", 116.5f, 9.5f, 3)
         .Fila("Gema", 124.5f, 9.5f, 4).Fila("Gema", 136.5f, 9.5f, 3).Fila("Gema", 141.5f, 11.6f, 3)
         .Fila("Gema", 153.5f, 11.6f, 4).Fila("Gema", 158.5f, 13.6f, 3).Fila("Gema", 167f, 5.5f, 5);

        // Frutas (cura)
        n.NoAr("Cereja", 55.5f, 11.6f).NoAr("Cereja", 77f, 5.5f).NoAr("Cereja", 96f, 5.5f)
         .NoAr("Cereja", 145f, 5.5f).NoAr("Cereja", 172f, 5.5f);

        // Inimigos
        n.NoChao("Gamba", 15f, 4f).NoChao("Gamba", 28f, 4f, true).NoChao("Gamba", 44f, 6f).NoChao("Gamba", 50f, 6f, true)
         .NoChao("Gamba", 82f, 4f).NoChao("Gamba", 92f, 4f, true).NoChao("Gamba", 108f, 4f).NoChao("Gamba", 120f, 4f, true)
         .NoChao("Gamba", 139f, 4f).NoChao("Gamba", 156f, 6f, true)
         .NoAr("Aguia", 35f, 12f).NoAr("Aguia", 68f, 8f).NoAr("Aguia", 96f, 10f).NoAr("Aguia", 147f, 11f).NoAr("Aguia", 168f, 10f);

        // Armadilhas
        n.NoChao("Espinhos", 24f, 4f).NoChao("Espinhos", 25f, 4f)
         .NoChao("Espinhos", 61f, 4f).NoChao("Espinhos", 62f, 4f).NoChao("Espinhos", 63f, 4f)
         .NoChao("Espinhos", 101f, 4f).NoChao("Espinhos", 102f, 4f)
         .NoChao("Espinhos", 134f, 4f).NoChao("Espinhos", 135f, 4f)
         .NoChao("Trampolim", 78f, 4f).NoChao("Trampolim", 128f, 4f);

        n.Movel("PlataformaMovel", 69f, 5f, new Vector2(5f, 0f))
         .Movel("PlataformaMovel", 54f, 6f, new Vector2(0f, 4f))
         .Movel("PlataformaMovel", 148f, 6f, new Vector2(0f, 3f));

        n.NoChao("Checkpoint", 41f, 6f).NoChao("Checkpoint", 86f, 4f).NoChao("Checkpoint", 137f, 4f).NoChao("Checkpoint", 170f, 4f)
         .NoChao("FimDeFase", 197f, 4f);

        // Loja antes da arena; a arena comeca em 176 (a parede fecha em 174).
        n.NoChao("Loja", 172.5f, 4f);
        n.Chefe("GambaRei", 190f, 4f, 176f);

        n.Enfeite(PropsFloresta + "/tree.png", 5f, 4f, 3).Enfeite(PropsFloresta + "/bush.png", 11f, 4f, 6)
         .Enfeite(PropsFloresta + "/tree.png", 30f, 4f, 3).Enfeite(PropsFloresta + "/pine.png", 46f, 6f, 3)
         .Enfeite(PropsFloresta + "/shrooms.png", 59f, 4f, 6).Enfeite(PropsFloresta + "/tree.png", 84f, 4f, 3)
         .Enfeite(PropsFloresta + "/bush.png", 94f, 4f, 6).Enfeite(PropsFloresta + "/tree.png", 112f, 4f, 3)
         .Enfeite(PropsFloresta + "/pine.png", 123f, 4f, 3).Enfeite(PropsFloresta + "/tree.png", 142f, 4f, 3)
         .Enfeite(PropsFloresta + "/bush.png", 160f, 6f, 6).Enfeite(PropsFloresta + "/tree.png", 180f, 4f, 3)
         .Enfeite(PropsFloresta + "/tree.png", 194f, 4f, 3).Enfeite(PropsFloresta + "/shrooms.png", 187f, 4f, 6);

        return n;
    }

    // =====================================================================
    // FASE 2 - caverna com lava, lajes, morcegos e serras; chefe: Lagarto de Fogo
    // =====================================================================
    private static NivelDef Fase2()
    {
        var n = new NivelDef
        {
            cena = "Fase2", bioma = Bioma.Caverna, musica = "caverna", largura = 210,
            nascimento = new Vector2(3f, 4f), corDoCeu = new Color(0.05f, 0.07f, 0.14f),
            titulo = "FASE 2", subtitulo = "CAVERNA", icone = "Assets/_Game/Art/Grotto/Enemies/Crab/crab-walk1.png"
        };

        n.Chao(0, 18).Lava(18, 5).Chao(23, 14).Lava(37, 8).Chao(45, 16).Lava(61, 6).Chao(67, 20)
         .Lava(87, 10).Chao(97, 14).Lava(111, 5).Chao(116, 24)
         .Lava(140, 6).Chao(146, 16).Lava(162, 8).Chao(170, 40);

        n.Plataforma(19, 7, 3).Plataforma(38, 7, 3).Plataforma(42, 9, 2)
         .Plataforma(50, 8, 4).Plataforma(62, 7, 4).Plataforma(72, 9, 4).Plataforma(78, 11, 3)
         .Plataforma(88, 7, 3).Plataforma(93, 9, 3).Plataforma(100, 8, 4).Plataforma(120, 8, 4).Plataforma(128, 10, 3)
         .Plataforma(141, 7, 4).Plataforma(150, 9, 3).Plataforma(163, 7, 3).Plataforma(166, 9, 3);

        // Diamantes (moeda)
        n.Fila("Gema", 6f, 5.6f, 4).Fila("Gema", 12f, 5.6f, 4).Fila("Gema", 19.5f, 9.6f, 3).Fila("Gema", 27f, 5.6f, 5)
         .Fila("Gema", 38.5f, 9.6f, 3).Fila("Gema", 50.5f, 10.6f, 4).Fila("Gema", 55f, 5.6f, 5).Fila("Gema", 62.5f, 9.6f, 4)
         .Fila("Gema", 72.5f, 11.6f, 4).Fila("Gema", 70f, 5.6f, 6).Fila("Gema", 88.5f, 9.6f, 3).Fila("Gema", 93.5f, 11.6f, 3)
         .Fila("Gema", 100.5f, 10.6f, 4).Fila("Gema", 118f, 5.6f, 5).Fila("Gema", 120.5f, 10.6f, 4).Fila("Gema", 128.5f, 12.6f, 3)
         .Fila("Gema", 141.5f, 9.6f, 4).Fila("Gema", 150.5f, 11.6f, 3).Fila("Gema", 152f, 5.6f, 5)
         .Fila("Gema", 163.5f, 9.6f, 3).Fila("Gema", 166.5f, 11.6f, 3).Fila("Gema", 173f, 5.6f, 5);

        // Frutas (cura)
        n.NoAr("Cereja", 42.7f, 11.6f).NoAr("Cereja", 79.5f, 13.6f).NoAr("Cereja", 106f, 5.6f)
         .NoAr("Cereja", 158f, 5.6f).NoAr("Cereja", 179f, 5.6f);

        // Inimigos da caverna
        n.NoChao("Caranguejo", 10f, 4f).NoChao("Gosma", 28f, 4f).NoChao("Esqueleto", 32f, 4f, true)
         .NoChao("Caranguejo", 52f, 4f).NoChao("Gosma", 58f, 4f, true)
         .NoChao("Esqueleto", 72f, 4f).NoChao("DemonioMini", 80f, 4f, true).NoChao("Caranguejo", 84f, 4f)
         .NoChao("Gosma", 102f, 4f).NoChao("Esqueleto", 108f, 4f, true)
         .NoChao("DemonioMini", 122f, 4f).NoChao("Caranguejo", 130f, 4f, true)
         .NoChao("Esqueleto", 150f, 4f).NoChao("Gosma", 156f, 4f, true).NoChao("Caranguejo", 175f, 4f)
         .NoAr("Morcego", 20f, 11f).NoAr("Morcego", 66f, 10f).NoAr("OlhoVoador", 92f, 12f)
         .NoAr("Fantasma", 40f, 12f).NoAr("Morcego", 118f, 11f).NoAr("Morcego", 165f, 11f).NoAr("Fantasma", 148f, 12f);

        // Armadilhas
        n.NoChao("Fogo", 14f, 4f).NoChao("Fogo", 48f, 4f).NoChao("Fogo", 76f, 4f).NoChao("Fogo", 100f, 4f).NoChao("Fogo", 158f, 4f)
         .NoAr("Serra", 64f, 4.5f).NoAr("Serra", 91f, 5f).NoAr("Serra", 113.5f, 5f).NoAr("Serra", 143f, 5f);

        n.Movel("PlataformaMovelCaverna", 39f, 5f, new Vector2(5f, 0f))
         .Movel("PlataformaMovelCaverna", 89f, 5.5f, new Vector2(7f, 0f))
         .Movel("PlataformaMovelCaverna", 163f, 5.5f, new Vector2(5f, 0f));

        n.NoChao("Checkpoint", 47f, 4f).NoChao("Checkpoint", 99f, 4f).NoChao("Checkpoint", 148f, 4f).NoChao("Checkpoint", 176f, 4f)
         .NoChao("FimDeFase", 206f, 4f);

        n.NoChao("Loja", 178.5f, 4f);
        n.Chefe("LagartoDeFogo", 198f, 4f, 182f);

        n.Carimbar(0, 4, 4).Carimbar(1, 12, 6).Carimbar(3, 8, 4).Carimbar(2, 27, 4).Carimbar(0, 33, 4)
         .Carimbar(3, 55, 4).Carimbar(1, 70, 6).Carimbar(2, 82, 4).Carimbar(0, 104, 4).Carimbar(3, 124, 4).Carimbar(1, 132, 6)
         .Carimbar(0, 153, 4).Carimbar(3, 186, 4).Carimbar(1, 195, 6).Carimbar(3, 203, 4);

        n.Enfeite(PropsCaverna + "/plant-big.png", 6f, 4f, 6).Enfeite(PropsCaverna + "/palm.png", 30f, 4f, 6)
         .Enfeite(PropsCaverna + "/plant.png", 60f, 4f, 6).Enfeite(PropsCaverna + "/palm.png", 74f, 4f, 6)
         .Enfeite(PropsCaverna + "/plant-big.png", 106f, 4f, 6).Enfeite(PropsCaverna + "/palm.png", 126f, 4f, 6)
         .Enfeite(PropsCaverna + "/crate.png", 17f, 4f, 6).Enfeite(PropsCaverna + "/big-crate.png", 44f, 4f, 6)
         .Enfeite(PropsCaverna + "/palm.png", 155f, 4f, 6).Enfeite(PropsCaverna + "/plant-big.png", 178f, 4f, 6)
         .Enfeite(PropsCaverna + "/plant.png", 192f, 4f, 6).Enfeite(PropsCaverna + "/palm.png", 204f, 4f, 6);

        return n;
    }

    // =====================================================================
    // FASE 3 - inverno, vertical e rapido; chefe: Yeti Gigante
    // =====================================================================
    private static NivelDef Fase3()
    {
        var n = new NivelDef
        {
            cena = "Fase3", bioma = Bioma.Inverno, musica = "inverno", largura = 230,
            nascimento = new Vector2(3f, 4f), corDoCeu = new Color(0.72f, 0.80f, 0.92f),
            titulo = "FASE 3", subtitulo = "INVERNO", icone = "Assets/_Game/Art/Winter/Enemies/Yeti/yeti-1.png"
        };

        n.Chao(0, 22).Chao(26, 14).Chao(44, 10, 6).Chao(58, 18).Chao(80, 12).Chao(97, 20).Chao(121, 39)
         .Chao(163, 14).Chao(180, 10, 6).Chao(193, 37);

        n.Bloco(30, 4, 3, 3).Bloco(64, 4, 4, 3).Bloco(70, 4, 3, 5).Bloco(104, 4, 3, 4).Bloco(130, 4, 4, 3).Bloco(140, 4, 3, 5)
         .Bloco(168, 4, 3, 3).Bloco(200, 4, 3, 3);

        n.Plataforma(8, 7, 4).Plataforma(14, 10, 3).Plataforma(19, 8, 3)
         .Plataforma(36, 8, 4).Plataforma(48, 10, 4).Plataforma(54, 8, 3)
         .Plataforma(74, 10, 4).Plataforma(84, 8, 4).Plataforma(90, 10, 4)
         .Plataforma(108, 9, 4).Plataforma(114, 11, 3).Plataforma(124, 8, 4).Plataforma(146, 9, 4).Plataforma(152, 11, 3)
         .Plataforma(172, 9, 3).Plataforma(184, 10, 4).Plataforma(189, 12, 3);

        // Diamantes (moeda)
        n.Fila("Gema", 8.5f, 9.5f, 4).Fila("Gema", 19.5f, 10.5f, 3).Fila("Gema", 5f, 5.5f, 4)
         .Fila("Gema", 36.5f, 10.5f, 4).Fila("Gema", 45f, 7.5f, 6).Fila("Gema", 48.5f, 12.6f, 4).Fila("Gema", 54.5f, 10.5f, 3)
         .Fila("Gema", 66f, 8.5f, 3).Fila("Gema", 71.5f, 10.6f, 2).Fila("Gema", 84.5f, 10.5f, 4).Fila("Gema", 90.5f, 12.6f, 4)
         .Fila("Gema", 100f, 5.5f, 5).Fila("Gema", 108.5f, 11.5f, 4).Fila("Gema", 124.5f, 10.5f, 4).Fila("Gema", 135f, 5.5f, 5)
         .Fila("Gema", 141.5f, 10.6f, 2).Fila("Gema", 146.5f, 11.5f, 4).Fila("Gema", 152.5f, 13.6f, 3)
         .Fila("Gema", 165f, 5.5f, 4).Fila("Gema", 172.5f, 11.5f, 3).Fila("Gema", 181f, 7.5f, 6).Fila("Gema", 189.5f, 14.6f, 3)
         .Fila("Gema", 195f, 5.5f, 5);

        // Frutas (cura)
        n.NoAr("Cereja", 15.5f, 12.6f).NoAr("Cereja", 76f, 12.6f).NoAr("Cereja", 115.5f, 13.6f).NoAr("Cereja", 132f, 7.6f)
         .NoAr("Cereja", 186f, 12.6f).NoAr("Cereja", 201.5f, 7.6f);

        n.NoChao("Yeti", 12f, 4f).NoChao("Raposa", 18f, 4f, true).NoChao("Yeti", 34f, 4f)
         .NoChao("Raposa", 50f, 6f).NoChao("Yeti", 60f, 4f, true).NoChao("Raposa", 76f, 4f)
         .NoChao("Yeti", 86f, 4f).NoChao("Raposa", 100f, 4f, true).NoChao("Yeti", 112f, 4f)
         .NoChao("Raposa", 126f, 4f).NoChao("Yeti", 136f, 4f, true).NoChao("Raposa", 148f, 4f)
         .NoChao("Raposa", 166f, 4f, true).NoChao("Yeti", 185f, 6f).NoChao("Raposa", 188f, 6f)
         .NoAr("Coruja", 24f, 10f).NoAr("Coruja", 56f, 12f).NoAr("Coruja", 94f, 11f).NoAr("Coruja", 118f, 12f).NoAr("Coruja", 144f, 12f)
         .NoAr("Coruja", 178f, 11f).NoAr("Coruja", 197f, 12f);

        n.NoChao("Espinhos", 27f, 4f).NoChao("Espinhos", 28f, 4f)
         .NoChao("Espinhos", 82f, 4f).NoChao("Espinhos", 83f, 4f).NoChao("Espinhos", 84f, 4f)
         .NoChao("Espinhos", 122f, 4f).NoChao("Espinhos", 123f, 4f)
         .NoChao("Espinhos", 194f, 4f).NoChao("Espinhos", 195f, 4f)
         .NoAr("Serra", 41f, 5f).NoAr("Serra", 92f, 5.5f).NoAr("Serra", 119f, 8f).NoAr("Serra", 178.5f, 5f)
         .NoChao("Trampolim", 62f, 4f).NoChao("Trampolim", 128f, 4f).NoChao("Trampolim", 175f, 4f);

        n.Movel("PlataformaMovel", 41f, 6f, new Vector2(0f, 5f))
         .Movel("PlataformaMovel", 93f, 6f, new Vector2(3f, 0f))
         .Movel("PlataformaMovel", 116f, 6f, new Vector2(4f, 0f))
         .Movel("PlataformaMovel", 178f, 6f, new Vector2(0f, 4f));

        n.NoChao("Checkpoint", 46f, 6f).NoChao("Checkpoint", 100f, 4f).NoChao("Checkpoint", 164f, 4f).NoChao("Checkpoint", 196.5f, 4f)
         .NoChao("FimDeFase", 226f, 4f);

        n.NoChao("Loja", 198.8f, 4f);
        n.Chefe("YetiGigante", 218f, 4f, 203f);

        n.Carimbar(0, 4, 4).Carimbar(1, 10, 4).Carimbar(0, 37, 4).Carimbar(1, 61, 4).Carimbar(0, 76, 4)
         .Carimbar(0, 101, 4).Carimbar(1, 110, 4).Carimbar(0, 133, 4).Carimbar(0, 150, 4).Carimbar(1, 154, 4)
         .Carimbar(0, 170, 4).Carimbar(1, 195, 4).Carimbar(0, 206, 4).Carimbar(0, 224, 4);

        return n;
    }
}
