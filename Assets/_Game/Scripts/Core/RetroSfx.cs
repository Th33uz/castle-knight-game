using UnityEngine;

/// <summary>
/// Efeitos sonoros estilo 8-bit gerados por codigo, sem arquivos de audio.
///
/// Cada efeito e uma onda simples (quadrada, triangular ou ruido) com variacao
/// de frequencia e volume ao longo do tempo. Sao criados uma vez, no primeiro
/// acesso, e reutilizados.
/// </summary>
public static class RetroSfx
{
    private const int TaxaDeAmostragem = 22050;

    private static AudioClip pulo, puloDuplo, moeda, dano, pisao, morte, checkpoint, vitoria, trampolim, cura, vidaExtra;
    private static AudioClip espada, golpe, chefeDano, chefeMorte, compra, recusado;

    // Caixa registradora: duas notas rapidas e altas.
    public static AudioClip Compra => compra ??= Gerar("sfx_compra", 0.3f, t =>
        Quadrada(t, t < 0.1f ? 1318f : 1760f) * Decair(t, 0.3f) * 0.6f);

    // "Nao pode": nota grave curta.
    public static AudioClip Recusado => recusado ??= Gerar("sfx_recusado", 0.25f, t =>
        Quadrada(t, 180f) * Decair(t, 0.25f) * 0.5f);

    // Espada cortando o ar: ruido curto que afina.
    public static AudioClip Espada => espada ??= Gerar("sfx_espada", 0.16f, t =>
        Ruido(t) * Decair(t, 0.16f) * Mathf.Lerp(0.4f, 1f, t / 0.16f) * 0.7f);

    // Espada acertando: baque grave.
    public static AudioClip Golpe => golpe ??= Gerar("sfx_golpe", 0.18f, t =>
        (Triangular(t, Mathf.Lerp(220f, 80f, t / 0.18f)) + Ruido(t) * 0.4f) * Decair(t, 0.18f));

    public static AudioClip ChefeDano => chefeDano ??= Gerar("sfx_chefe_dano", 0.35f, t =>
        (Quadrada(t, Mathf.Lerp(160f, 70f, t / 0.35f)) + Ruido(t) * 0.5f) * Decair(t, 0.35f));

    public static AudioClip ChefeMorte => chefeMorte ??= Gerar("sfx_chefe_morte", 1.3f, t =>
        (Ruido(t) * 0.8f + Quadrada(t, Mathf.Lerp(200f, 40f, t / 1.3f)) * 0.5f) * Decair(t, 1.3f));

    // Fruta: tres notas subindo, mais suaves que a moeda.
    public static AudioClip Cura => cura ??= Gerar("sfx_cura", 0.5f, t =>
        Triangular(t, NotaDoArpejo(t, 0.16f, 659f, 784f, 1046f)) * Decair(t, 0.5f) * 0.9f);

    // Vida extra: fanfarra curta.
    public static AudioClip VidaExtra => vidaExtra ??= Gerar("sfx_vida_extra", 0.8f, t =>
        Quadrada(t, NotaDoArpejo(t, 0.13f, 784f, 988f, 1175f, 1568f, 1175f, 1568f)) * Decair(t, 0.8f) * 0.7f);

    public static AudioClip Pulo => pulo ??= Gerar("sfx_pulo", 0.18f, t =>
        Quadrada(t, Mathf.Lerp(300f, 700f, t / 0.18f)) * Decair(t, 0.18f));

    public static AudioClip PuloDuplo => puloDuplo ??= Gerar("sfx_pulo_duplo", 0.22f, t =>
        Quadrada(t, Mathf.Lerp(500f, 1100f, t / 0.22f)) * Decair(t, 0.22f));

    // Duas notas rapidas subindo: o classico "blim" de moeda.
    public static AudioClip Moeda => moeda ??= Gerar("sfx_moeda", 0.24f, t =>
        Quadrada(t, t < 0.08f ? 987f : 1318f) * Decair(t, 0.24f) * 0.7f);

    public static AudioClip Dano => dano ??= Gerar("sfx_dano", 0.3f, t =>
        Triangular(t, Mathf.Lerp(400f, 120f, t / 0.3f)) * Decair(t, 0.3f));

    public static AudioClip Pisao => pisao ??= Gerar("sfx_pisao", 0.16f, t =>
        Ruido(t) * Decair(t, 0.16f) * 0.8f);

    public static AudioClip Morte => morte ??= Gerar("sfx_morte", 0.9f, t =>
        Quadrada(t, Mathf.Lerp(600f, 60f, t / 0.9f)) * Decair(t, 0.9f) * 0.8f);

    // Arpejo curto para cima.
    public static AudioClip Checkpoint => checkpoint ??= Gerar("sfx_checkpoint", 0.45f, t =>
        Quadrada(t, NotaDoArpejo(t, 0.15f, 523f, 659f, 784f)) * Decair(t, 0.45f) * 0.7f);

    public static AudioClip Vitoria => vitoria ??= Gerar("sfx_vitoria", 1.1f, t =>
        Quadrada(t, NotaDoArpejo(t, 0.18f, 523f, 659f, 784f, 1046f, 784f, 1046f)) * 0.6f);

    public static AudioClip Trampolim => trampolim ??= Gerar("sfx_trampolim", 0.25f, t =>
        Triangular(t, Mathf.Lerp(200f, 900f, t / 0.25f)) * Decair(t, 0.25f));

    // ----------------- Formas de onda -----------------

    private static float Quadrada(float t, float frequencia)
    {
        return Mathf.Sin(2f * Mathf.PI * frequencia * t) >= 0f ? 0.5f : -0.5f;
    }

    private static float Triangular(float t, float frequencia)
    {
        float fase = (t * frequencia) % 1f;
        return (Mathf.Abs(fase * 4f - 2f) - 1f) * 0.7f;
    }

    private static float Ruido(float t)
    {
        // Ruido deterministico a partir do tempo, para o clipe ser sempre igual.
        float x = Mathf.Sin(t * 12345.678f) * 43758.5453f;
        return ((x - Mathf.Floor(x)) * 2f - 1f) * 0.6f;
    }

    private static float Decair(float t, float duracao)
    {
        return Mathf.Clamp01(1f - t / duracao);
    }

    /// <summary>Devolve a nota tocada no instante t, trocando a cada <paramref name="duracaoDaNota"/>.</summary>
    private static float NotaDoArpejo(float t, float duracaoDaNota, params float[] notas)
    {
        int indice = Mathf.Min(notas.Length - 1, (int)(t / duracaoDaNota));
        return notas[indice];
    }

    // ----------------- Montagem do clipe -----------------

    private static AudioClip Gerar(string nome, float duracao, System.Func<float, float> amostra)
    {
        int quantidade = Mathf.CeilToInt(duracao * TaxaDeAmostragem);
        float[] dados = new float[quantidade];

        for (int i = 0; i < quantidade; i++)
        {
            float t = (float)i / TaxaDeAmostragem;
            dados[i] = Mathf.Clamp(amostra(t), -1f, 1f) * 0.6f;
        }

        AudioClip clipe = AudioClip.Create(nome, quantidade, 1, TaxaDeAmostragem, false);
        clipe.SetData(dados, 0);
        return clipe;
    }
}
