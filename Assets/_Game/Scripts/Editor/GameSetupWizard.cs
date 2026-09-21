using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu "Jogo" na barra superior do Unity. Reconstroi o jogo inteiro a partir
/// dos assets e das definicoes de fase: animacoes, prefabs, tiles e cenas.
/// </summary>
public static class GameSetupWizard
{
    // Camadas personalizadas comecam no indice 8; de 0 a 7 sao reservadas pela Unity.
    private static readonly string[] CamadasPersonalizadas = { "Ground", "Player", "Enemy", "Hazard", "OneWayPlatform" };
    private static readonly string[] TagsNecessarias = { "Collectible", "Checkpoint", "LevelEnd", "Hazard", "MovingPlatform" };

    private static readonly string[] PastasGeradas =
    {
        "Assets/_Game/Scenes",
        "Assets/_Game/Prefabs",
        "Assets/_Game/Animations",
        "Assets/_Game/Tiles",
    };

    [MenuItem("Jogo/Reconstruir tudo (cenas, prefabs, animacoes)", priority = 0)]
    public static void ReconstruirTudo()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Pare o Play", "Saia do modo Play antes de reconstruir o projeto.", "Ok");
            return;
        }

        if (!EditorUtility.DisplayDialog("Reconstruir tudo",
            "Isto apaga e recria as cenas, os prefabs, as animacoes e os tiles gerados.\n\n"
            + "Alteracoes feitas na mao nessas pastas serao perdidas. Continuar?", "Reconstruir", "Cancelar"))
            return;

        // A cena aberta pode ser uma das que vao ser apagadas: salva o que o
        // usuario quiser e sai dela antes, abrindo a cena base do template.
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        Reconstruir(capturarTelas: false);

        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");

        EditorUtility.DisplayDialog("Pronto",
            "Jogo reconstruido. Abra Assets/_Game/Scenes/MainMenu e aperte Play.", "Beleza");
    }

    /// <summary>Chamado pela linha de comando: Unity -batchmode -executeMethod GameSetupWizard.ReconstruirBatch</summary>
    public static void ReconstruirBatch()
    {
        Reconstruir(capturarTelas: true);
    }

    private static void Reconstruir(bool capturarTelas)
    {
        Debug.Log("[Setup] ===== Reconstruindo o jogo =====");

        GarantirTagsECamadas();
        SpriteImportSettings.CorrigirFatiamentoAutomatico();
        ApagarGerados();

        FontBuilder.Garantir();
        PrefabBuilder.GerarTodos();
        SceneBuilder.GerarTodas(sobrescrever: true);
        SceneBuilder.AtualizarBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (capturarTelas)
        {
            string pasta = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
            ScreenshotTool.CapturarTodas(pasta);
        }

        Debug.Log("[Setup] ===== Concluido =====");
    }

    private static void ApagarGerados()
    {
        foreach (string pasta in PastasGeradas)
        {
            if (AssetDatabase.IsValidFolder(pasta))
                AssetDatabase.DeleteAsset(pasta);

            AssetDatabase.CreateFolder("Assets/_Game", Path.GetFileName(pasta));
        }

        AssetDatabase.Refresh();
    }

    // ----------------- Etapas separadas -----------------

    [MenuItem("Jogo/Etapas/1 - Tags e camadas")]
    public static void GarantirTagsECamadas()
    {
        Object[] ativos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (ativos == null || ativos.Length == 0)
        {
            Debug.LogError("[Setup] Nao consegui abrir o TagManager.");
            return;
        }

        SerializedObject tagManager = new SerializedObject(ativos[0]);
        AdicionarTags(tagManager);
        DefinirCamadas(tagManager);
        tagManager.ApplyModifiedProperties();
    }

    [MenuItem("Jogo/Etapas/2 - Fonte pixel")]
    public static void GerarFonte() => FontBuilder.Garantir();

    [MenuItem("Jogo/Etapas/3 - Prefabs e animacoes")]
    public static void GerarPrefabs() => PrefabBuilder.GerarTodos();

    [MenuItem("Jogo/Etapas/4 - Cenas (so as que faltam)")]
    public static void GerarCenas() => SceneBuilder.GerarTodas(sobrescrever: false);

    [MenuItem("Jogo/Etapas/5 - Cenas (recriar todas)")]
    public static void RecriarCenas() => SceneBuilder.GerarTodas(sobrescrever: true);

    [MenuItem("Jogo/Etapas/6 - Capturar telas das fases")]
    public static void CapturarTelas()
    {
        string pasta = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
        ScreenshotTool.CapturarTodas(pasta);
        EditorUtility.RevealInFinder(pasta);
    }

    // ----------------- Implementacao -----------------

    private static void AdicionarTags(SerializedObject tagManager)
    {
        SerializedProperty tags = tagManager.FindProperty("tags");

        foreach (string tag in TagsNecessarias)
        {
            bool existe = false;
            for (int i = 0; i < tags.arraySize && !existe; i++)
                existe = tags.GetArrayElementAtIndex(i).stringValue == tag;

            if (existe)
                continue;

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        }
    }

    private static void DefinirCamadas(SerializedObject tagManager)
    {
        SerializedProperty camadas = tagManager.FindProperty("layers");

        for (int i = 0; i < CamadasPersonalizadas.Length; i++)
        {
            int indice = 8 + i;
            if (indice >= camadas.arraySize)
                break;

            SerializedProperty camada = camadas.GetArrayElementAtIndex(indice);
            if (string.IsNullOrEmpty(camada.stringValue))
                camada.stringValue = CamadasPersonalizadas[i];
        }
    }
}
