using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Confere, antes de montar as cenas, se o desenho de cada fase faz sentido:
/// objeto de chao com chao embaixo e fora de bloco, enfeite apoiado inteiro
/// no chao, carimbo (arvore, pilar) sem vao embaixo e sem entrar em laje ou
/// bloco, lajes com ar entre elas, item (gema, fruta) fora da pedra.
///
/// O desenho das fases e escrito a mao em LevelDesigns, com o chao em pedacos e
/// vaos entre eles. Errar um X por dois punha espinho boiando no meio do buraco,
/// arbusto no ar, raposa dentro da parede e pilar colado na laje - e isso so
/// aparecia jogando. Aqui o erro sai no console assim que a fase e gerada.
/// </summary>
public static class ValidadorDeFases
{
    private struct Superficie
    {
        public float x0, x1, y;
        public string tipo;
    }

    /// <summary>Roda em todas as fases e escreve no console o que estiver solto.</summary>
    public static void ValidarTudo()
    {
        int problemas = 0;

        foreach (NivelDef nivel in LevelDesigns.Todas())
            problemas += Validar(nivel);

        if (problemas == 0)
            Debug.Log("[Setup] Fases validadas: nada boiando, enterrado ou colado.");
    }

    public static int Validar(NivelDef nivel)
    {
        List<Superficie> superficies = Superficies(nivel);
        TileKit kit = TileKits.Para(nivel.bioma);
        int problemas = 0;

        void Aviso(string mensagem)
        {
            problemas++;
            Debug.LogWarning("[Fases] " + nivel.cena + ": " + mensagem);
        }

        // Lajes com a altura real (2 linhas na caverna, inverno e castelo).
        var lajes = new List<RectInt>();
        foreach (RectInt r in nivel.plataformas)
            lajes.Add(new RectInt(r.x, r.y, r.width, kit.alturaPlataforma));

        // ----- Objetos no chao: apoio e fora de bloco -----
        foreach (Objeto o in nivel.objetos)
        {
            if (!o.noChao)
                continue;

            // Chao embaixo, mas um bloco por cima: o objeto nasce enterrado na
            // parede (a raposa da fase do gelo ficava assim dentro de um Bloco).
            RectInt? bloco = BlocoQueCobre(nivel, o.posicao);
            if (bloco.HasValue)
            {
                Aviso("'" + o.prefab + "' em x=" + o.posicao.x + ", y=" + o.posicao.y + " esta dentro do bloco " + Descreve(bloco.Value) + ".");
                continue;
            }

            if (!TemApoio(superficies, o.posicao))
                Aviso("'" + o.prefab + "' em x=" + o.posicao.x + " pede o chao em y=" + o.posicao.y + ", mas ali " + OqueTem(superficies, o.posicao.x) + ".");
        }

        // ----- Itens no ar (gema, fruta, serra, voador): fora da pedra -----
        foreach (Objeto o in nivel.objetos)
        {
            if (o.noChao)
                continue;

            Rect item = new Rect(o.posicao.x - 0.3f, o.posicao.y - 0.3f, 0.6f, 0.6f);
            foreach (RectInt r in nivel.chao)
                if (item.Overlaps(ParaRect(r)))
                    Aviso("'" + o.prefab + "' em (" + o.posicao.x + ", " + o.posicao.y + ") esta dentro do chao/bloco " + Descreve(r) + ".");
            foreach (RectInt l in lajes)
                if (item.Overlaps(ParaRect(l)))
                    Aviso("'" + o.prefab + "' em (" + o.posicao.x + ", " + o.posicao.y + ") esta dentro da laje " + Descreve(l) + ".");
        }

        // ----- Enfeites: pe no chao, inteiro sobre o chao, fora de bloco -----
        foreach (Prop p in nivel.props)
        {
            string nome = System.IO.Path.GetFileName(p.sprite);

            RectInt? bloco = BlocoQueCobre(nivel, p.pe);
            if (bloco.HasValue)
            {
                Aviso("enfeite '" + nome + "' em x=" + p.pe.x + " esta dentro do bloco " + Descreve(bloco.Value) + ".");
                continue;
            }

            if (!TemApoio(superficies, p.pe))
            {
                Aviso("enfeite '" + nome + "' em x=" + p.pe.x + " pede o chao em y=" + p.pe.y + ", mas ali " + OqueTem(superficies, p.pe.x) + ".");
                continue;
            }

            // Um arbusto na beirada fica metade no ar. O sprite e centrado no x.
            float meiaLargura = TamanhoDoSprite(p.sprite).x / 2f;
            Vector2 esquerda = new Vector2(p.pe.x - meiaLargura + 0.1f, p.pe.y);
            Vector2 direita = new Vector2(p.pe.x + meiaLargura - 0.1f, p.pe.y);
            if (!TemApoio(superficies, esquerda) || !TemApoio(superficies, direita))
                Aviso("enfeite '" + nome + "' em x=" + p.pe.x + " (" + (meiaLargura * 2f).ToString("0.0") + " de largura) passa da beirada do chao.");
        }

        // ----- Carimbos (arvore, pilar): sem vao embaixo, sem entrar em laje/bloco -----
        foreach (Carimbo c in nivel.carimbos)
        {
            if (c.indice < 0 || c.indice >= kit.carimbos.Count)
            {
                Aviso("carimbo " + c.indice + " em x=" + c.x + " nao existe no kit do bioma.");
                continue;
            }

            TileRef[,] desenho = kit.carimbos[c.indice];
            RectInt area = new RectInt(c.x, c.y, desenho.GetLength(1), desenho.GetLength(0));

            foreach (RectInt l in lajes)
                if (area.Overlaps(l))
                    Aviso("carimbo " + c.indice + " em (" + c.x + ", " + c.y + ") entra na laje " + Descreve(l) + ".");

            // Chao por baixo da base e esperado (a arvore nasce um tile dentro da
            // neve); bloco que sobe acima da base, nao.
            foreach (RectInt r in nivel.chao)
                if (area.Overlaps(r) && r.yMax > c.y + 1)
                    Aviso("carimbo " + c.indice + " em (" + c.x + ", " + c.y + ") entra no bloco " + Descreve(r) + ".");

            // Lampioes ficam pendurados de proposito; o que nao pode e coluna sem chao nenhum.
            for (int coluna = area.xMin; coluna < area.xMax; coluna++)
            {
                if (!TemChaoNaColuna(nivel, coluna))
                {
                    Aviso("carimbo " + c.indice + " em (" + c.x + ", " + c.y + ") fica sobre um vao (coluna " + coluna + ").");
                    break;
                }
            }
        }

        // ----- Lajes: ar entre elas, e nenhuma dentro de bloco -----
        for (int i = 0; i < lajes.Count; i++)
        {
            for (int j = i + 1; j < lajes.Count; j++)
            {
                RectInt a = lajes[i], b = lajes[j];
                bool colunaComum = a.xMin < b.xMax && b.xMin < a.xMax;
                bool linhasSeTocam = a.yMin <= b.yMax && b.yMin <= a.yMax;
                bool ladoALado = a.xMax == b.xMin || b.xMax == a.xMin;

                if (colunaComum && linhasSeTocam)
                    Aviso("lajes coladas: " + Descreve(a) + " e " + Descreve(b) + ".");
                else if (ladoALado && linhasSeTocam)
                    Aviso("lajes encostadas pela quina: " + Descreve(a) + " e " + Descreve(b) + ".");
            }

            foreach (RectInt r in nivel.chao)
                if (lajes[i].Overlaps(r))
                    Aviso("laje " + Descreve(lajes[i]) + " entra no chao/bloco " + Descreve(r) + ".");
        }

        return problemas;
    }

    // ----------------- Apoios -----------------

    /// <summary>Retangulo de chao que passa por cima do ponto onde o objeto apoia os pes, se houver.</summary>
    private static RectInt? BlocoQueCobre(NivelDef nivel, Vector2 posicao)
    {
        foreach (RectInt r in nivel.chao)
        {
            if (posicao.x >= r.xMin && posicao.x < r.xMax && r.yMax > posicao.y + 0.01f)
                return r;
        }

        return null;
    }

    private static bool TemChaoNaColuna(NivelDef nivel, int coluna)
    {
        foreach (RectInt r in nivel.chao)
            if (coluna >= r.xMin && coluna < r.xMax)
                return true;

        return false;
    }

    private static List<Superficie> Superficies(NivelDef nivel)
    {
        var lista = new List<Superficie>();

        // Chao e blocos: a superficie e o topo do retangulo.
        foreach (RectInt r in nivel.chao)
            lista.Add(new Superficie { x0 = r.xMin, x1 = r.xMax, y = r.yMax, tipo = "chao" });

        // Laje: a linha de baixo e "y", e ela ocupa alturaPlataforma linhas.
        int altura = TileKits.Para(nivel.bioma).alturaPlataforma;
        foreach (RectInt r in nivel.plataformas)
            lista.Add(new Superficie { x0 = r.xMin, x1 = r.xMax, y = r.y + altura, tipo = "laje" });

        return lista;
    }

    private static bool TemApoio(List<Superficie> superficies, Vector2 posicao)
    {
        foreach (Superficie s in superficies)
        {
            if (posicao.x >= s.x0 && posicao.x < s.x1 && Mathf.Abs(s.y - posicao.y) < 0.01f)
                return true;
        }

        return false;
    }

    private static string OqueTem(List<Superficie> superficies, float x)
    {
        var alturas = new List<string>();

        foreach (Superficie s in superficies)
        {
            if (x >= s.x0 && x < s.x1)
                alturas.Add(s.tipo + " em y=" + s.y);
        }

        return alturas.Count == 0
            ? "nao ha chao nenhum (e um vao)"
            : "so ha " + string.Join(", ", alturas);
    }

    // ----------------- Utilidades -----------------

    /// <summary>Tamanho do sprite em unidades do mundo (PPU 16). Um tile se o arquivo nao existir.</summary>
    private static Vector2 TamanhoDoSprite(string caminho)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        return sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
    }

    private static Rect ParaRect(RectInt r) => new Rect(r.xMin, r.yMin, r.width, r.height);

    private static string Descreve(RectInt r) => "x=" + r.xMin + ".." + r.xMax + " y=" + r.yMin + ".." + r.yMax;
}
