using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Confere, antes de montar as cenas, se todo objeto marcado como "no chao"
/// realmente tem chao embaixo.
///
/// O desenho das fases e escrito a mao em LevelDesigns, com o chao em pedacos e
/// vaos entre eles. Errar um X por dois punha espinho boiando no meio do buraco
/// e trampolim afundado no trecho mais alto - e isso so aparecia jogando.
/// Aqui o erro sai no console assim que a fase e gerada.
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
            Debug.Log("[Setup] Fases validadas: todo objeto de chao tem chao embaixo.");
    }

    public static int Validar(NivelDef nivel)
    {
        List<Superficie> superficies = Superficies(nivel);
        int problemas = 0;

        foreach (Objeto o in nivel.objetos)
        {
            if (!o.noChao || TemApoio(superficies, o.posicao))
                continue;

            problemas++;
            Debug.LogWarning(
                "[Fases] " + nivel.cena + ": '" + o.prefab + "' em x=" + o.posicao.x +
                " pede o chao em y=" + o.posicao.y + ", mas ali " + OqueTem(superficies, o.posicao.x) + ".");
        }

        return problemas;
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
}
