using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Gera as cenas do jogo a partir das definicoes em LevelDesigns: fundo em
/// parallax, terreno pintado com o tileset do bioma, inimigos, itens,
/// armadilhas, HUD, musica, camera com limites.
///
/// As cenas sao copiadas da SampleScene do template porque ela ja traz a
/// Global Light 2D; sem essa luz, todo sprite fica preto no URP 2D.
/// </summary>
public static class SceneBuilder
{
    private const string CenaBase = "Assets/Scenes/SampleScene.unity";
    private const string PastaCenas = "Assets/_Game/Scenes";
    private const string PastaMusica = "Assets/_Game/Audio/Music";

    private const float TamanhoDaCamera = 6f;   // metade da altura visivel, em tiles

    // O chao desce ate aqui, bem abaixo do que a camera mostra (limite -3, ou
    // seja, ela enxerga ate y = -3): os buracos viram abismos e nao "ilhas
    // flutuando no ceu".
    private const int FundoDoMundo = -12;

    // A zona de morte fica logo abaixo da borda visivel: o jogador morre ainda
    // na tela, a animacao de morte aparece, e o fade cobre o reinicio.
    private const float AlturaDoAbismo = -3f;

    public static void GerarTodas(bool sobrescrever)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CenaBase) == null)
        {
            Debug.LogError("[Setup] Nao encontrei " + CenaBase + ", que serve de base para as cenas.");
            return;
        }

        GarantirPasta();

        foreach (string nome in LevelDesigns.OrdemDasCenas)
        {
            bool existe = AssetDatabase.LoadAssetAtPath<SceneAsset>(CaminhoDa(nome)) != null;

            if (existe && !sobrescrever)
            {
                Debug.Log("[Setup] Cena " + nome + " ja existe, mantida como esta.");
                continue;
            }

            if (existe)
                AssetDatabase.DeleteAsset(CaminhoDa(nome));

            if (nome == "MainMenu")
                MontarMenu(nome);
            else
                MontarFase(LevelDesigns.PorNome(nome));
        }

        AtualizarBuildSettings();
        AssetDatabase.SaveAssets();
    }

    private static void GarantirPasta()
    {
        if (!AssetDatabase.IsValidFolder(PastaCenas))
            AssetDatabase.CreateFolder("Assets/_Game", "Scenes");
    }

    private static string CaminhoDa(string nome) => PastaCenas + "/" + nome + ".unity";

    private static Scene AbrirCopiaDaBase(string nome)
    {
        AssetDatabase.CopyAsset(CenaBase, CaminhoDa(nome));
        AssetDatabase.Refresh();
        return EditorSceneManager.OpenScene(CaminhoDa(nome), OpenSceneMode.Single);
    }

    // =====================================================================
    // Menu
    // =====================================================================

    private static void MontarMenu(string nome)
    {
        Scene cena = AbrirCopiaDaBase(nome);
        ConfigurarLuzGlobal();

        // Um pedacinho de floresta ao fundo, com o heroi parado e um inimigo
        // passeando, para o menu ja ter a cara do jogo.
        var vitrine = new NivelDef { cena = "MainMenu", bioma = Bioma.Floresta, largura = 60, corDoCeu = new Color(0.36f, 0.78f, 0.94f) };
        vitrine.Chao(0, 60).Plataforma(14, 7, 4).Plataforma(22, 10, 3);
        vitrine.Fila("Gema", 22.5f, 11.5f, 3).Fila("Gema", 4.5f, 5.5f, 3);
        // Em cima da plataforma: patrulha so ali e nunca alcanca o heroi parado.
        vitrine.NoChao("Gamba", 16f, 8f);
        vitrine.Enfeite("Assets/_Game/Art/SunnyLand/Props/tree.png", 4f, 4f, 3)
               .Enfeite("Assets/_Game/Art/SunnyLand/Props/bush.png", 26f, 4f, 6)
               .Enfeite("Assets/_Game/Art/SunnyLand/Props/tree.png", 30f, 4f, 3);

        CriarFundo(vitrine);
        PintarTerreno(vitrine);
        ColocarProps(vitrine);
        ColocarObjetos(vitrine);

        GameObject heroi = Instanciar("Jogador", new Vector3(9f, 4f, 0f), noChao: true);
        if (heroi != null)
        {
            // Sem controle nem vida: fica parado respirando, o Animator segue no Idle,
            // e nada que aconteca na vitrine pode disparar morte/reinicio de cena.
            PlayerController2D controlador = heroi.GetComponent<PlayerController2D>();
            if (controlador != null) controlador.enabled = false;
            PlayerHealth vida = heroi.GetComponent<PlayerHealth>();
            if (vida != null) vida.enabled = false;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographicSize = TamanhoDaCamera;
            cam.backgroundColor = vitrine.corDoCeu;
            cam.transform.position = new Vector3(12f, 7.5f, -10f);
        }

        CriarMusica("menu");
        UIBuilder.CriarMenuPrincipal();

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("[Setup] Cena " + nome + " criada.");
    }

    // =====================================================================
    // Fases
    // =====================================================================

    private static void MontarFase(NivelDef nivel)
    {
        Scene cena = AbrirCopiaDaBase(nivel.cena);
        ConfigurarLuzGlobal();

        CriarFundo(nivel);
        PintarTerreno(nivel);
        ColocarProps(nivel);
        ColocarObjetos(nivel);
        ColocarChefe(nivel);

        GameObject ponto = new GameObject("PontoDeNascimento");
        GameObject jogador = Instanciar("Jogador", nivel.nascimento, noChao: true);
        ponto.transform.position = jogador != null ? jogador.transform.position : (Vector3)nivel.nascimento;

        CriarLimiteDeQueda(nivel.largura);
        CriarParedes(nivel.largura);
        ConfigurarCamera(nivel, jogador);
        CriarLevelManager(ponto.transform, jogador);
        CriarMusica(nivel.musica);
        UIBuilder.CriarHUD(nivel);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("[Setup] Cena " + nivel.cena + " criada (" + nivel.largura + " tiles).");
    }

    // ----------------- Fundo -----------------

    private static void CriarFundo(NivelDef nivel)
    {
        GameObject pai = new GameObject("Fundo");
        float larguraTotal = nivel.largura + 80f;

        foreach (CamadaFundo camada in Cenarios.Fundo(nivel.bioma))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(camada.sprite);
            if (sprite == null)
            {
                Debug.LogWarning("[Setup] Fundo nao encontrado: " + camada.sprite);
                continue;
            }

            GameObject go = new GameObject("Fundo " + Path.GetFileNameWithoutExtension(camada.sprite));
            go.transform.SetParent(pai.transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            // Tiled repete a imagem na horizontal ao longo de toda a fase.
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            float altura = sprite.bounds.size.y;
            sr.size = new Vector2(larguraTotal, altura);
            sr.sortingLayerName = "Background";
            sr.sortingOrder = camada.ordem;

            go.transform.position = new Vector3(nivel.largura / 2f, camada.baseY + altura / 2f, 0f);

            go.AddComponent<ParallaxLayer>().Configurar(camada.fatorX, camada.fatorY);
        }
    }

    // ----------------- Terreno -----------------

    private static void PintarTerreno(NivelDef nivel)
    {
        TileKit kit = TileKits.Para(nivel.bioma);
        TerrainPainter pintor = new TerrainPainter(kit);

        GameObject grid = new GameObject("Grid");
        grid.AddComponent<Grid>().cellSize = Vector3.one;

        Tilemap chao = NovoTilemap(grid, "Chao", "Midground", 0, LayerMask.NameToLayer("Ground"), colisor: true, trigger: false);
        Tilemap detalhes = NovoTilemap(grid, "Detalhes", "Midground", 1, 0, colisor: false, trigger: false);
        Tilemap fundo = NovoTilemap(grid, "DecoracaoFundo", "Background", 4, 0, colisor: false, trigger: false);

        // A neve tem uns 6 px de "ar" no topo do tile; baixa a colisao para os
        // pes ficarem na neve desenhada, e nao flutuando sobre ela.
        float afundamento = AfundamentoDaSuperficie(nivel.bioma);
        TilemapCollider2D colisorDoChao = chao.GetComponent<TilemapCollider2D>();
        if (colisorDoChao != null)
            colisorDoChao.offset = new Vector2(0f, -afundamento);

        foreach (RectInt r in nivel.chao)
        {
            pintor.PintarChao(chao, AteOFundoDoMundo(r));
            pintor.DecorarSuperficie(detalhes, r);
        }

        foreach (RectInt p in nivel.plataformas)
            pintor.PintarPlataforma(chao, p.x, p.y, p.width);

        foreach (Carimbo c in nivel.carimbos)
            pintor.Carimbar(fundo, c.indice, c.x, c.y);

        if (kit.temPerigo && nivel.perigos.Count > 0)
        {
            Tilemap lava = NovoTilemap(grid, "Lava", "Midground", 0, LayerMask.NameToLayer("Hazard"), colisor: true, trigger: true);
            lava.gameObject.tag = "Hazard";

            Hazard perigo = lava.gameObject.AddComponent<Hazard>();
            SerializedObject so = new SerializedObject(perigo);
            so.FindProperty("mataInstantaneamente").boolValue = true;
            so.ApplyModifiedProperties();

            foreach (RectInt r in nivel.perigos)
                pintor.PintarPerigo(lava, AteOFundoDoMundo(r));
        }

        GerarColisores(grid);
    }

    /// <summary>
    /// Blocos que nascem no chao ou apoiados nele (y ate 4) descem ate o fundo
    /// do mundo: viram terreno continuo, sem borda de baixo visivel. Pilares
    /// e degraus passam a ser parte do relevo em vez de blocos soltos.
    /// </summary>
    private static RectInt AteOFundoDoMundo(RectInt r)
    {
        if (r.y > 4)
            return r;

        return new RectInt(r.x, FundoDoMundo, r.width, r.yMax - FundoDoMundo);
    }

    public static float AfundamentoDaSuperficie(Bioma bioma)
    {
        return bioma == Bioma.Inverno ? 0.25f : 0f;
    }

    /// <summary>Paredes invisiveis nas duas pontas, para o jogador nao sair da fase.</summary>
    private static void CriarParedes(int largura)
    {
        GameObject pai = new GameObject("Paredes");

        foreach (float x in new[] { -0.5f, largura + 0.5f })
        {
            GameObject parede = new GameObject(x < 0 ? "ParedeEsquerda" : "ParedeDireita");
            parede.transform.SetParent(pai.transform);
            parede.transform.position = new Vector3(x, 10f, 0f);
            parede.layer = LayerMask.NameToLayer("Ground");

            BoxCollider2D col = parede.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 60f);
        }
    }

    /// <summary>
    /// Forca a geracao da geometria dos colisores antes de salvar a cena.
    ///
    /// Em modo batch a fisica nao roda, entao o TilemapCollider2D nao processa os
    /// tiles pintados e o CompositeCollider2D seria salvo vazio: a fase aparece
    /// certa, mas o jogador atravessa o chao. (O PhysicsSetup refaz isso em jogo
    /// por seguranca, mas a cena salva tambem deve estar correta.)
    /// </summary>
    private static void GerarColisores(GameObject grid)
    {
        Physics2D.SyncTransforms();

        foreach (TilemapCollider2D colisor in grid.GetComponentsInChildren<TilemapCollider2D>())
            colisor.ProcessTilemapChanges();

        foreach (CompositeCollider2D composto in grid.GetComponentsInChildren<CompositeCollider2D>())
        {
            composto.GenerateGeometry();
            Debug.Log("[Setup] Colisor do chao: " + composto.pathCount + " contornos, " + composto.pointCount + " pontos.");
        }
    }

    private static Tilemap NovoTilemap(GameObject grid, string nome, string sortingLayer, int ordem, int camada, bool colisor, bool trigger)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(grid.transform);
        go.layer = camada;

        Tilemap mapa = go.AddComponent<Tilemap>();
        TilemapRenderer renderizador = go.AddComponent<TilemapRenderer>();
        renderizador.sortingLayerName = sortingLayer;
        renderizador.sortingOrder = ordem;

        if (!colisor)
            return mapa;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        TilemapCollider2D colisorDoMapa = go.AddComponent<TilemapCollider2D>();

        if (trigger)
        {
            colisorDoMapa.isTrigger = true;
            return mapa;
        }

        // O Composite funde os colisores de todos os tiles num contorno unico;
        // sem ele o jogador engancha nas juncoes invisiveis entre dois tiles.
        CompositeCollider2D composto = go.AddComponent<CompositeCollider2D>();
        composto.geometryType = CompositeCollider2D.GeometryType.Polygons;

#if UNITY_2023_1_OR_NEWER
        colisorDoMapa.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
        colisorDoMapa.usedByComposite = true;
#endif
        return mapa;
    }

    // ----------------- Props e objetos -----------------

    private static void ColocarProps(NivelDef nivel)
    {
        if (nivel.props.Count == 0)
            return;

        GameObject pai = new GameObject("Cenario");

        foreach (Prop prop in nivel.props)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(prop.sprite);
            if (sprite == null)
            {
                Debug.LogWarning("[Setup] Prop nao encontrado: " + prop.sprite);
                continue;
            }

            GameObject go = new GameObject(Path.GetFileNameWithoutExtension(prop.sprite));
            go.transform.SetParent(pai.transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 5 + prop.ordem;

            // "pe" e a base do desenho; o pivo do sprite e o centro.
            go.transform.position = new Vector3(prop.pe.x, prop.pe.y + sprite.bounds.extents.y - sprite.bounds.center.y, 0f);
        }
    }

    private static void ColocarObjetos(NivelDef nivel)
    {
        GameObject pai = new GameObject("Objetos");
        float afundamento = AfundamentoDaSuperficie(nivel.bioma);

        foreach (Objeto o in nivel.objetos)
        {
            // Objetos sem fisica (espinhos, bandeira, itens) nao "assentam" sozinhos
            // na colisao rebaixada, entao descem o mesmo tanto na mao.
            Vector2 posicao = o.posicao;
            if (o.noChao)
                posicao.y -= afundamento;

            GameObject instancia = Instanciar(o.prefab, posicao, o.noChao);
            if (instancia == null)
                continue;

            instancia.transform.SetParent(pai.transform);

            EnemyPatrol patrulha = instancia.GetComponent<EnemyPatrol>();
            if (patrulha != null)
            {
                SerializedObject so = new SerializedObject(patrulha);
                so.FindProperty("comecarParaDireita").boolValue = !o.paraEsquerda;
                so.ApplyModifiedProperties();
            }

            MovingPlatform movel = instancia.GetComponent<MovingPlatform>();
            if (movel != null && o.extra != Vector2.zero)
            {
                SerializedObject so = new SerializedObject(movel);
                so.FindProperty("deslocamento").vector2Value = o.extra;
                so.ApplyModifiedProperties();
            }
        }
    }

    /// <summary>
    /// Chefe no fim da fase: dorme ate o jogador cruzar a entrada da arena; a
    /// parede fecha atras, e o trofeu so aparece quando ele morre.
    /// </summary>
    private static void ColocarChefe(NivelDef nivel)
    {
        if (!nivel.chefe.HasValue)
            return;

        ChefeDef def = nivel.chefe.Value;
        GameObject pai = new GameObject("Chefe");

        GameObject chefe = Instanciar(def.prefab, new Vector3(def.x, def.yChao, 0f), noChao: true);
        if (chefe == null)
            return;
        chefe.transform.SetParent(pai.transform);

        // Parede que fecha a arena (comeca desligada).
        GameObject parede = new GameObject("ParedeDaArena");
        parede.transform.SetParent(pai.transform);
        parede.transform.position = new Vector3(def.inicioArena - 2f, 12f, 0f);
        parede.layer = LayerMask.NameToLayer("Ground");
        BoxCollider2D colParede = parede.AddComponent<BoxCollider2D>();
        colParede.size = new Vector2(1f, 40f);
        parede.SetActive(false);

        // Gatilho na entrada.
        GameObject entrada = new GameObject("EntradaDaArena");
        entrada.transform.SetParent(pai.transform);
        entrada.transform.position = new Vector3(def.inicioArena, 10f, 0f);
        BoxCollider2D colEntrada = entrada.AddComponent<BoxCollider2D>();
        colEntrada.size = new Vector2(1f, 30f);
        colEntrada.isTrigger = true;

        BossArena arena = entrada.AddComponent<BossArena>();
        SerializedObject soArena = new SerializedObject(arena);
        soArena.FindProperty("chefe").objectReferenceValue = chefe.GetComponent<Boss>();
        soArena.FindProperty("parede").objectReferenceValue = parede;
        soArena.ApplyModifiedProperties();

        // O trofeu fica escondido ate o chefe cair.
        LevelEnd fim = Object.FindFirstObjectByType<LevelEnd>();
        if (fim != null)
            fim.gameObject.SetActive(false);

        Boss boss = chefe.GetComponent<Boss>();
        SerializedObject soChefe = new SerializedObject(boss);
        soChefe.FindProperty("liberarAoMorrer").objectReferenceValue = fim != null ? fim.gameObject : null;
        soChefe.FindProperty("desativarAoMorrer").objectReferenceValue = parede;
        soChefe.ApplyModifiedProperties();
    }

    /// <summary>
    /// Instancia um prefab. Com <paramref name="noChao"/>, a posicao dada e onde
    /// os pes encostam; o objeto sobe o suficiente para o colisor apoiar ali.
    /// </summary>
    private static GameObject Instanciar(string nome, Vector3 posicao, bool noChao)
    {
        GameObject prefab = PrefabBuilder.Carregar(nome);
        if (prefab == null)
        {
            Debug.LogWarning("[Setup] Prefab nao encontrado: " + nome);
            return null;
        }

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        if (noChao)
            posicao.y -= BaseDoColisor(instancia);

        instancia.transform.position = posicao;
        return instancia;
    }

    /// <summary>Y local da borda de baixo do colisor (negativo = abaixo do pivo).</summary>
    private static float BaseDoColisor(GameObject go)
    {
        Collider2D col = go.GetComponent<Collider2D>();

        switch (col)
        {
            case BoxCollider2D caixa: return caixa.offset.y - caixa.size.y / 2f;
            case CapsuleCollider2D capsula: return capsula.offset.y - capsula.size.y / 2f;
            case CircleCollider2D circulo: return circulo.offset.y - circulo.radius;
        }

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null ? -sr.sprite.bounds.extents.y : 0f;
    }

    // ----------------- Camera, limites, gerenciadores -----------------

    private static void ConfigurarCamera(NivelDef nivel, GameObject jogador)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographicSize = TamanhoDaCamera;
        cam.backgroundColor = nivel.corDoCeu;

        CameraFollow seguidor = cam.gameObject.AddComponent<CameraFollow>();
        SerializedObject so = new SerializedObject(seguidor);
        so.FindProperty("alvo").objectReferenceValue = jogador != null ? jogador.transform : null;
        so.FindProperty("deslocamento").vector2Value = new Vector2(0f, 1.5f);
        so.FindProperty("usarLimites").boolValue = true;
        so.FindProperty("limiteMinimo").vector2Value = new Vector2(0f, -3f);
        so.FindProperty("limiteMaximo").vector2Value = new Vector2(nivel.largura, 22f);
        so.ApplyModifiedProperties();

        if (jogador != null)
            cam.transform.position = new Vector3(jogador.transform.position.x, jogador.transform.position.y + 1.5f, -10f);
    }

    private static void CriarLimiteDeQueda(int largura)
    {
        GameObject limite = new GameObject("LimiteDeQueda");
        limite.transform.position = new Vector3(largura / 2f, AlturaDoAbismo, 0f);

        BoxCollider2D col = limite.AddComponent<BoxCollider2D>();
        col.size = new Vector2(largura + 100f, 3f);
        col.isTrigger = true;

        limite.AddComponent<KillZone>();
    }

    private static void CriarLevelManager(Transform pontoDeNascimento, GameObject jogador)
    {
        GameObject go = new GameObject("LevelManager");
        LevelManager gerente = go.AddComponent<LevelManager>();

        SerializedObject so = new SerializedObject(gerente);
        so.FindProperty("pontoDeNascimento").objectReferenceValue = pontoDeNascimento;
        so.FindProperty("jogador").objectReferenceValue = jogador != null ? jogador.transform : null;
        so.ApplyModifiedProperties();
    }

    private static void CriarMusica(string nome)
    {
        AudioClip clipe = AssetDatabase.LoadAssetAtPath<AudioClip>(PastaMusica + "/" + nome + ".ogg");
        if (clipe == null)
        {
            Debug.LogWarning("[Setup] Musica nao encontrada: " + nome);
            return;
        }

        GameObject go = new GameObject("Musica");
        MusicaDaFase musica = go.AddComponent<MusicaDaFase>();
        SerializedObject so = new SerializedObject(musica);
        so.FindProperty("musica").objectReferenceValue = clipe;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Faz a luz global iluminar todas as Sorting Layers. A cena base foi salva
    /// quando so existia a camada "Default"; no URP 2D, sprite fora da lista da
    /// luz e desenhado preto.
    /// </summary>
    private static void ConfigurarLuzGlobal()
    {
        foreach (Light2D luz in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            SerializedObject so = new SerializedObject(luz);
            SerializedProperty camadas = so.FindProperty("m_ApplyToSortingLayers");
            if (camadas == null)
                continue;

            camadas.arraySize = SortingLayer.layers.Length;
            for (int i = 0; i < SortingLayer.layers.Length; i++)
                camadas.GetArrayElementAtIndex(i).intValue = SortingLayer.layers[i].id;

            so.ApplyModifiedProperties();
        }
    }

    // ----------------- Build Settings -----------------

    public static void AtualizarBuildSettings()
    {
        var cenas = new List<EditorBuildSettingsScene>();

        foreach (string nome in LevelDesigns.OrdemDasCenas)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CaminhoDa(nome)) != null)
                cenas.Add(new EditorBuildSettingsScene(CaminhoDa(nome), true));
        }

        EditorBuildSettings.scenes = cenas.ToArray();
        Debug.Log("[Setup] Build Settings atualizado com " + cenas.Count + " cenas.");
    }
}
