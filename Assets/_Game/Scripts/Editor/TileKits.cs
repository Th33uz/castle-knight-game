using System.Collections.Generic;
using UnityEngine;

public enum Bioma { Floresta, Caverna, Inverno, Castelo }

/// <summary>Um tile especifico: qual folha fatiada e qual indice dentro dela.</summary>
public struct TileRef
{
    public string folha;
    public int indice;

    // Por padrao o colisor e a celula inteira (bordas arredondadas nao deixam
    // buracos). Tiles em que o desenho ocupa so parte da celula (a ponta da
    // laje da caverna, que tem 9 px de ar a esquerda) usam o contorno do
    // sprite, senao o jogador fica em pe no vazio.
    public bool colisorPeloDesenho;

    public TileRef(string folha, int indice, bool colisorPeloDesenho = false)
    {
        this.folha = folha;
        this.indice = indice;
        this.colisorPeloDesenho = colisorPeloDesenho;
    }
}

/// <summary>Os tiles de uma linha do terreno: extremidade esquerda, meio (variantes) e direita.</summary>
public class Faixa
{
    public TileRef[] esq, meio, dir;
    public TileRef[] unico; // quando a faixa tem 1 tile de largura; se null, usa "meio"

    public Faixa(TileRef[] esq, TileRef[] meio, TileRef[] dir, TileRef[] unico = null)
    {
        this.esq = esq;
        this.meio = meio;
        this.dir = dir;
        this.unico = unico;
    }
}

/// <summary>
/// Como montar o chao de um bioma a partir do seu tileset. Os indices vem da
/// grade 16x16 de cada folha (indice = linha * colunas + coluna, linha 0 em cima),
/// conferidos visualmente nas imagens de grade geradas durante o setup.
/// </summary>
public class TileKit
{
    public Faixa topo;                 // superficie (grama, neve, borda iluminada)
    public List<Faixa> linhas = new List<Faixa>(); // linhas logo abaixo da superficie, em ordem
    public TileRef[] recheio;          // preenchimento profundo; null = repete a ultima linha
    public Faixa fundo;                // linha de baixo de um bloco flutuante; null = usa a ultima linha

    public int alturaPlataforma = 1;   // quantas linhas uma plataforma flutuante ocupa
    public Faixa plataformaTopo;       // se null, plataformas usam o 9-slice normal
    public Faixa plataformaBaixo;      // segunda linha da plataforma, quando alturaPlataforma == 2

    public TileRef perigoTopo, perigoBaixo; // lava / agua (so a caverna tem)
    public bool temPerigo;

    // Variantes escolhidas em sequencia pela coluna (2,3,2,3...) em vez de
    // sorteadas: tilesets em que os tiles do meio formam uma onda continua
    // (a neve) precisam da ordem certa para emendar.
    public bool variarEmSequencia;

    // Decoracao: carimbos prontos (arvores etc.) e tiles pequenos para espalhar
    public List<TileRef[,]> carimbos = new List<TileRef[,]>();
    public TileRef[] detalhesDeChao = new TileRef[0]; // tufos de grama, pedrinhas, no topo do chao
}

public static class TileKits
{
    public const string FolhaFloresta = "Assets/_Game/Art/SunnyLand/Tileset/tileset (16x16).png";
    public const string FolhaCaverna = "Assets/_Game/Art/Grotto/Tileset/tileset (16x16).png";
    public const string FolhaCaverna2 = "Assets/_Game/Art/Grotto/Tileset/tileset-2 (16x16).png";
    public const string FolhaInverno = "Assets/_Game/Art/Winter/Tileset/tileset (16x16).png";
    public const string FolhaCastelo = "Assets/_Game/Art/Castle/Tileset/tileset (16x16).png";

    public static TileKit Para(Bioma bioma)
    {
        switch (bioma)
        {
            case Bioma.Caverna: return Caverna();
            case Bioma.Inverno: return Inverno();
            case Bioma.Castelo: return Castelo();
            default: return Floresta();
        }
    }

    // Atalhos: R(folha, colunas) devolve uma funcao (col, lin) -> TileRef.
    private static System.Func<int, int, TileRef> R(string folha, int colunas)
    {
        return (col, lin) => new TileRef(folha, lin * colunas + col);
    }

    private static TileRef[] Um(TileRef t) => new[] { t };

    // ----------------- Floresta (Sunny Land, 25 colunas) -----------------

    private static TileKit Floresta()
    {
        var t = R(FolhaFloresta, 25);
        var kit = new TileKit();

        kit.topo = new Faixa(Um(t(1, 1)), Um(t(3, 1)), Um(t(5, 1)), Um(t(5, 7)));
        kit.linhas.Add(new Faixa(Um(t(1, 3)), Um(t(3, 3)), Um(t(5, 3)), Um(t(3, 3))));
        kit.fundo = new Faixa(Um(t(1, 5)), Um(t(3, 5)), Um(t(5, 5)), Um(t(3, 5)));

        // Plataformas de uma linha com a grama em cima e o fundo arredondado.
        kit.alturaPlataforma = 1;
        kit.plataformaTopo = new Faixa(Um(t(15, 14)), Um(t(17, 14)), Um(t(19, 14)), Um(t(17, 14)));

        // So tufos de grama e folhagem: o bloco de terra solto (7,7) parecia um
        // obstaculo solido e confundia, por isso ficou de fora.
        kit.detalhesDeChao = new[] { t(1, 7), t(3, 7), t(9, 7), t(11, 7) };

        return kit;
    }

    // ----------------- Caverna (Super Grotto Escape) -----------------

    private static TileKit Caverna()
    {
        var a = R(FolhaCaverna, 23);
        var b = R(FolhaCaverna2, 13);
        var kit = new TileKit();

        // Superficie: blocos de rocha com a borda de cima iluminada (linha 4).
        TileRef[] topoVariantes = { a(1, 4), a(2, 4), a(3, 4), a(4, 4) };
        TileRef[] baixoVariantes = { a(1, 5), a(2, 5), a(3, 5), a(4, 5) };
        kit.topo = new Faixa(Um(a(1, 4)), topoVariantes, Um(a(4, 4)), Um(a(2, 4)));
        kit.linhas.Add(new Faixa(Um(a(1, 5)), baixoVariantes, Um(a(4, 5)), Um(a(2, 5))));
        kit.recheio = new[] { b(1, 8), b(2, 8), b(3, 8), b(1, 9), b(2, 9), b(3, 9) };

        // Plataformas flutuantes: a laje grande, duas linhas de altura.
        //
        // Na folha a laje vai da coluna 12 a 21: a coluna 12 e a borda esquerda
        // (9 px de ar + a quina iluminada), 13-20 sao o corpo e a 21 e a borda
        // direita, com a face escura. A coluna 22 e VAZIA - antes ela era usada
        // como ponta direita, e como o colisor e por celula o jogador pisava num
        // tile invisivel no fim de toda laje; e a 21 entrava no meio, deixando
        // um risco escuro no corpo.
        kit.alturaPlataforma = 2;
        kit.plataformaTopo = new Faixa(Um(new TileRef(FolhaCaverna, 2 * 23 + 12, colisorPeloDesenho: true)),
            new[] { a(13, 2), a(14, 2), a(15, 2), a(16, 2), a(17, 2), a(18, 2), a(19, 2), a(20, 2) },
            Um(a(21, 2)));
        kit.plataformaBaixo = new Faixa(Um(new TileRef(FolhaCaverna, 3 * 23 + 12, colisorPeloDesenho: true)),
            new[] { a(13, 3), a(14, 3), a(15, 3), a(16, 3), a(17, 3), a(18, 3), a(19, 3), a(20, 3) },
            Um(a(21, 3)));

        kit.temPerigo = true;
        kit.perigoTopo = a(6, 4);
        kit.perigoBaixo = a(6, 5);

        // Carimbos decorativos (sem colisao): lampiao, musgo, pilar.
        kit.carimbos.Add(new TileRef[,] { { b(6, 2), b(7, 2) }, { b(6, 3), b(7, 3) } });   // lampiao redondo
        kit.carimbos.Add(new TileRef[,] { { b(8, 2), b(9, 2) }, { b(8, 3), b(9, 3) } });   // lampiao pendurado
        kit.carimbos.Add(new TileRef[,] { { b(10, 2), b(11, 2) }, { b(10, 3), b(11, 3) } }); // musgo
        kit.carimbos.Add(new TileRef[,]
        {
            { a(8, 4), a(9, 4), a(10, 4) },
            { a(8, 5), a(9, 5), a(10, 5) },
            { a(8, 6), a(9, 6), a(10, 6) },
            { a(8, 7), a(9, 7), a(10, 7) },
            { a(8, 8), a(9, 8), a(10, 8) },
            { a(8, 9), a(9, 9), a(10, 9) },
        }); // pilar

        return kit;
    }

    // ----------------- Castelo (GothicVania Church, 21 colunas) -----------------

    private static TileKit Castelo()
    {
        var t = R(FolhaCastelo, 21);
        var kit = new TileKit();

        // Este tileset tem DOIS conjuntos de piso. O de pedra clara (linhas 10-12)
        // esta desenhado com 8 px de deslocamento em relacao a grade de 16, entao
        // cortaria os blocos ao meio. Os blocos roxos das linhas 7-8 estao
        // alinhados certinho e sao os usados aqui: linha 7 tem a borda iluminada
        // em cima, linha 8 e o corpo.
        TileRef[] topo = { t(1, 7), t(2, 7), t(4, 7), t(5, 7) };
        TileRef[] corpo = { t(1, 8), t(2, 8), t(4, 8), t(5, 8) };

        kit.topo = new Faixa(Um(t(1, 7)), topo, Um(t(5, 7)), Um(t(2, 7)));
        kit.linhas.Add(new Faixa(Um(t(1, 8)), corpo, Um(t(5, 8)), Um(t(2, 8))));
        // Recheio profundo: pedra escura, para o fundo do mundo nao ficar listrado.
        kit.recheio = new[] { t(7, 7), t(8, 7) };

        kit.alturaPlataforma = 2;
        kit.plataformaTopo = new Faixa(Um(t(1, 7)), topo, Um(t(5, 7)), Um(t(2, 7)));
        kit.plataformaBaixo = new Faixa(Um(t(1, 8)), corpo, Um(t(5, 8)), Um(t(2, 8)));

        return kit;
    }

    // ----------------- Inverno (SunnyLand Winter Forest, 20 colunas) -----------------

    private static TileKit Inverno()
    {
        var t = R(FolhaInverno, 20);
        var kit = new TileKit();

        // O tileset e um bloco de neve 4x3: linha 1 = topo ondulado, linha 2 =
        // corpo (textura de neve), linha 3 = fundo arredondado. Para o chao
        // solido so entram topo e corpo, repetindo o corpo ate onde a camera
        // enxerga; a linha 3 fica so para o fundo das plataformas flutuantes.
        // (Antes o corpo usava a linha 3 e um azul liso: virava uma faixa de
        // bolhas no meio da terra.)
        kit.variarEmSequencia = true;
        kit.topo = new Faixa(Um(t(1, 1)), new[] { t(2, 1), t(3, 1) }, Um(t(4, 1)), Um(t(17, 1)));
        kit.linhas.Add(new Faixa(Um(t(1, 2)), new[] { t(2, 2), t(3, 2) }, Um(t(4, 2)), Um(t(17, 2))));
        kit.recheio = new[] { t(2, 2), t(3, 2) };

        kit.alturaPlataforma = 2;
        kit.plataformaTopo = new Faixa(Um(t(1, 1)), new[] { t(2, 1), t(3, 1) }, Um(t(4, 1)), Um(t(17, 1)));
        kit.plataformaBaixo = new Faixa(Um(t(1, 3)), new[] { t(2, 3), t(3, 3) }, Um(t(4, 3)), Um(t(17, 3)));

        // Arvore, 4 colunas de largura (10 a 13 na folha): a copa ocupa as
        // linhas 4-7 (a 4 so tem a ponta), o tronco fica centrado na divisa das
        // colunas 11 e 12 (linhas 9-10) e as raizes abrem nas 4 colunas
        // (linhas 12-13). A versao anterior comecava na coluna 11 e punha o
        // tronco nas colunas 12-13: cortava o galho da esquerda, mostrava so a
        // metade direita do tronco e perdia a raiz esquerda.
        TileRef vazio = new TileRef(null, -1);
        kit.carimbos.Add(new TileRef[,]
        {
            { vazio,     t(11, 4),  t(12, 4),  vazio },
            { t(10, 5),  t(11, 5),  t(12, 5),  t(13, 5) },
            { t(10, 6),  t(11, 6),  t(12, 6),  t(13, 6) },
            { t(10, 7),  t(11, 7),  t(12, 7),  t(13, 7) },
            { vazio,     t(11, 9),  t(12, 9),  vazio },
            { vazio,     t(11, 10), t(12, 10), vazio },
            { t(10, 12), t(11, 12), t(12, 12), t(13, 12) },
            { t(10, 13), t(11, 13), t(12, 13), t(13, 13) },
        });
        kit.carimbos.Add(new TileRef[,] { { t(15, 6) } }); // pedra

        return kit;
    }
}
