using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renderiza cada cena em PNG, em varios pontos ao longo da fase, para conferir
/// o visual sem abrir o Editor. Usado pelo setup em modo batch.
/// </summary>
public static class ScreenshotTool
{
    private const int Largura = 960;
    private const int Altura = 540;
    private const float Passo = 20f;     // tiles entre uma captura e a proxima

    public static void CapturarTodas(string pastaSaida)
    {
        Directory.CreateDirectory(pastaSaida);
        int total = 0;

        foreach (string nome in LevelDesigns.OrdemDasCenas)
        {
            string caminho = "Assets/_Game/Scenes/" + nome + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(caminho) == null)
                continue;

            EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

            Camera cam = Camera.main;
            if (cam == null)
                continue;

            NivelDef nivel = LevelDesigns.PorNome(nome);

            if (nivel == null)
            {
                Capturar(cam, Path.Combine(pastaSaida, nome + ".png"));
                total++;
                continue;
            }

            for (float x = 10f; x < nivel.largura; x += Passo)
            {
                cam.transform.position = new Vector3(x, 7.5f, -10f);
                Capturar(cam, Path.Combine(pastaSaida, nome + "_" + ((int)x).ToString("000") + ".png"));
                total++;
            }
        }

        Debug.Log("[Setup] " + total + " capturas salvas em " + pastaSaida);
    }

    private static void Capturar(Camera cam, string arquivo)
    {
        RenderTexture rt = new RenderTexture(Largura, Altura, 24);
        RenderTexture anterior = cam.targetTexture;

        // Canvas em Overlay nao entra no Camera.Render; troca para Camera so
        // durante a captura, para a HUD aparecer na imagem. A cena nao e salva
        // depois disso, entao a troca nao vaza para o jogo.
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }

        // Em modo de edicao o layout da UI so e calculado no tick do Editor;
        // forca o calculo agora, senao o canvas sai vazio na captura.
        Canvas.ForceUpdateCanvases();

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D textura = new Texture2D(Largura, Altura, TextureFormat.RGB24, false);
        textura.ReadPixels(new Rect(0, 0, Largura, Altura), 0, 0);
        textura.Apply();

        File.WriteAllBytes(arquivo, textura.EncodeToPNG());

        cam.targetTexture = anterior;
        RenderTexture.active = null;
        Object.DestroyImmediate(textura);
        rt.Release();
    }
}
