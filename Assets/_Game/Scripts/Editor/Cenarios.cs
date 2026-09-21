/// <summary>Uma camada do fundo em parallax.</summary>
public struct CamadaFundo
{
    public string sprite;      // caminho do PNG
    public float fatorX;       // 0 = colado na camera (bem longe), 1 = anda com o mundo
    public float fatorY;
    public float baseY;        // onde fica a borda de baixo da imagem, em unidades do mundo
    public int ordem;          // ordem dentro da sorting layer Background
}

/// <summary>As camadas de fundo de cada bioma, do mais distante para o mais proximo.</summary>
public static class Cenarios
{
    private const string Art = "Assets/_Game/Art";

    public static CamadaFundo[] Fundo(Bioma bioma)
    {
        switch (bioma)
        {
            case Bioma.Caverna:
                return new[]
                {
                    new CamadaFundo { sprite = Art + "/Grotto/Background/back.png",   fatorX = 0.08f, fatorY = 0.05f, baseY = -1f, ordem = 0 },
                    new CamadaFundo { sprite = Art + "/Grotto/Background/far.png",    fatorX = 0.25f, fatorY = 0.08f, baseY = -1f, ordem = 1 },
                    new CamadaFundo { sprite = Art + "/Grotto/Background/middle.png", fatorX = 0.5f,  fatorY = 0.12f, baseY = -1f, ordem = 2 },
                };

            case Bioma.Inverno:
                return new[]
                {
                    new CamadaFundo { sprite = Art + "/Winter/Background/sky.png",         fatorX = 0.03f, fatorY = 0.02f, baseY = -1f,  ordem = 0 },
                    new CamadaFundo { sprite = Art + "/Winter/Background/mountains.png",   fatorX = 0.12f, fatorY = 0.05f, baseY = 3.5f, ordem = 1 },
                    new CamadaFundo { sprite = Art + "/Winter/Background/mid-layer-a.png", fatorX = 0.3f,  fatorY = 0.08f, baseY = 2f,   ordem = 2 },
                    new CamadaFundo { sprite = Art + "/Winter/Background/mid-layer-b.png", fatorX = 0.45f, fatorY = 0.1f,  baseY = 1.5f, ordem = 3 },
                };

            default:
                return new[]
                {
                    new CamadaFundo { sprite = Art + "/SunnyLand/Background/back.png",   fatorX = 0.08f, fatorY = 0.04f, baseY = -1f,  ordem = 0 },
                    new CamadaFundo { sprite = Art + "/SunnyLand/Background/middle.png", fatorX = 0.35f, fatorY = 0.1f,  baseY = -11f, ordem = 1 },
                };
        }
    }
}
