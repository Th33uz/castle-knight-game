using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Monta todos os prefabs do jogo com arte, animacao, colisores e scripts ja
/// ligados. As animacoes sao geradas aqui mesmo, pelo AnimationBuilder.
/// </summary>
public static class PrefabBuilder
{
    private const string PastaPrefabs = "Assets/_Game/Prefabs";
    private const string Art = "Assets/_Game/Art";

    public static void GerarTodos()
    {
        GarantirPasta();

        GameObject fxMorte = CriarFx("FxMorte", Art + "/SunnyLand/FX/EnemyDeath", "enemy-death-", 14f);
        GameObject fxColeta = CriarFx("FxColeta", Art + "/SunnyLand/FX/ItemFeedback", "item-feedback-", 14f);

        CriarJogador();
        CriarInimigos(fxMorte);
        CriarChefes(fxMorte);
        CriarArmadilhas();
        CriarItens(fxColeta);
        CriarMarcadores();
        CriarPlataformas();
        CriarLoja();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Prefabs criados em " + PastaPrefabs);
    }

    public static GameObject Carregar(string nome)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PastaPrefabs + "/" + nome + ".prefab");
    }

    private static void GarantirPasta()
    {
        if (!AssetDatabase.IsValidFolder(PastaPrefabs))
            AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
    }

    // ----------------- Jogador -----------------

    private static void CriarJogador()
    {
        string pasta = Art + "/Adventurer";

        AnimationClip idle = AnimationBuilder.CriarClip("Jogador_Idle", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-idle-"), 8f, true);
        AnimationClip corrida = AnimationBuilder.CriarClip("Jogador_Corrida", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-run-"), 12f, true);
        AnimationClip pulo = AnimationBuilder.CriarClip("Jogador_Pulo", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-jump-"), 12f, false);
        AnimationClip puloDuplo = AnimationBuilder.CriarClip("Jogador_PuloDuplo", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-smrslt-"), 14f, false);
        AnimationClip queda = AnimationBuilder.CriarClip("Jogador_Queda", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-fall-"), 8f, true);
        AnimationClip dano = AnimationBuilder.CriarClip("Jogador_Dano", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-hurt-"), 10f, false);
        AnimationClip morte = AnimationBuilder.CriarClip("Jogador_Morte", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-die-"), 10f, false);
        AnimationClip ataque = AnimationBuilder.CriarClip("Jogador_Ataque", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-attack1-"), 14f, false);
        AnimationClip ataqueAereo = AnimationBuilder.CriarClip("Jogador_AtaqueAereo", AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-air-attack1-"), 14f, false);

        AnimatorController controller = AnimationBuilder.CriarControllerJogador(
            "Jogador", idle, corrida, pulo, puloDuplo, queda, dano, morte, ataque, ataqueAereo);

        GameObject jogador = new GameObject("Jogador");
        jogador.tag = "Player";
        jogador.layer = LayerMask.NameToLayer("Player");

        SpriteRenderer sr = jogador.AddComponent<SpriteRenderer>();
        sr.sprite = AnimationBuilder.QuadrosDaPasta(pasta, "adventurer-idle-")[0];
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 5;

        Animator animator = jogador.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D rb = jogador.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // O quadro do Adventurer tem 50x37 px, mas o corpo ocupa so ~19x29 px,
        // deslocado 2 px para a esquerda do centro: o colisor segue o corpo.
        CapsuleCollider2D col = jogador.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.9f, 1.7f);
        col.offset = new Vector2(-0.1f, -0.2f);
        col.direction = CapsuleDirection2D.Vertical;

        // Sem atrito: encostar na lateral de uma plataforma no ar nao segura o
        // corpo contra a parede (ele escorrega em vez de ficar grudado na borda).
        // O movimento no chao nao depende de atrito: o controlador escreve a
        // velocidade a cada frame.
        col.sharedMaterial = MaterialSemAtrito();

        GameObject checagemChao = new GameObject("ChecagemChao");
        checagemChao.transform.SetParent(jogador.transform);
        checagemChao.transform.localPosition = new Vector3(col.offset.x, col.offset.y - col.size.y / 2f, 0f);

        jogador.AddComponent<AudioSource>().playOnAwake = false;

        PlayerController2D controlador = jogador.AddComponent<PlayerController2D>();
        SerializedObject so = new SerializedObject(controlador);
        so.FindProperty("checagemChao").objectReferenceValue = checagemChao.transform;
        so.FindProperty("camadaChao").intValue = MascaraDe("Ground", "OneWayPlatform");
        so.FindProperty("pulosMaximos").intValue = 2; // pulo duplo com a cambalhota
        so.ApplyModifiedProperties();

        jogador.AddComponent<PlayerHealth>();

        // Espada: acerta o que estiver na camada Enemy (inimigos e chefes).
        PlayerAttack espada = jogador.AddComponent<PlayerAttack>();
        SerializedObject soEspada = new SerializedObject(espada);
        soEspada.FindProperty("camadasAtingiveis").intValue = MascaraDe("Enemy", "Hazard");
        soEspada.ApplyModifiedProperties();

        Salvar(jogador, "Jogador");
    }

    // ----------------- Chefes -----------------

    private static void CriarChefes(GameObject fxMorte)
    {
        GameObject bolaDeFogo = CriarProjetil(fxMorte);

        // Floresta: gamba gigante que investe e pula. 3 golpes.
        CriarChefe("GambaRei", "GAMBA REI",
            AnimationBuilder.QuadrosDaPasta(Art + "/SunnyLand/Enemies/Opossum", "opossum-"), 12f, null,
            escala: 2.4f, colisor: new Vector2(1.6f, 1.3f), offset: new Vector2(0f, -0.05f),
            vida: 3, velocidade: 3f, investida: 10f, pulo: 17f, intervalo: 2.2f, olhaEsquerda: true,
            projetil: null, boca: Vector2.zero, fxMorte);

        // Caverna: lagarto que cospe bolas de fogo. 4 golpes.
        CriarChefe("LagartoDeFogo", "LAGARTO DE FOGO",
            AnimationBuilder.QuadrosDaPasta(Art + "/Grotto/Enemies/Lizard", "lizard-move"), 10f,
            AnimationBuilder.QuadrosDaPasta(Art + "/Grotto/Enemies/Lizard", "lizard-shoot"),
            escala: 2.2f, colisor: new Vector2(1.3f, 1.4f), offset: new Vector2(0f, -0.3f),
            vida: 4, velocidade: 2.5f, investida: 8f, pulo: 15f, intervalo: 2.4f, olhaEsquerda: true,
            projetil: bolaDeFogo, boca: new Vector2(1.6f, 0.3f), fxMorte);

        // Inverno: yeti gigante, rapido e saltador. 4 golpes.
        CriarChefe("YetiGigante", "YETI GIGANTE",
            AnimationBuilder.QuadrosDaPasta(Art + "/Winter/Enemies/Yeti", "yeti-"), 9f, null,
            escala: 2.6f, colisor: new Vector2(1.5f, 1.6f), offset: new Vector2(0f, -0.15f),
            vida: 4, velocidade: 2.8f, investida: 9f, pulo: 18f, intervalo: 2.0f, olhaEsquerda: false,
            projetil: null, boca: Vector2.zero, fxMorte);
    }

    private static void CriarChefe(string nome, string nomeExibido, Sprite[] quadros, float fps, Sprite[] quadrosAtaque,
        float escala, Vector2 colisor, Vector2 offset, int vida, float velocidade, float investida, float pulo,
        float intervalo, bool olhaEsquerda, GameObject projetil, Vector2 boca, GameObject fxMorte)
    {
        if (quadros.Length == 0)
        {
            Debug.LogWarning("[Setup] Chefe " + nome + " sem quadros; prefab nao criado.");
            return;
        }

        AnimationClip andar = AnimationBuilder.CriarClip(nome + "_Andar", quadros, fps, true);
        AnimationClip atacar = quadrosAtaque != null && quadrosAtaque.Length > 0
            ? AnimationBuilder.CriarClip(nome + "_Atacar", quadrosAtaque, 10f, false)
            : null;
        AnimatorController controller = AnimationBuilder.CriarControllerLoop(nome, andar, atacar);

        GameObject chefe = new GameObject(nome);
        chefe.tag = "Enemy";
        chefe.layer = LayerMask.NameToLayer("Enemy");
        // Ampliado: os packs nao tem chefes, entao o inimigo comum cresce.
        chefe.transform.localScale = Vector3.one * escala;

        SpriteRenderer sr = chefe.AddComponent<SpriteRenderer>();
        sr.sprite = quadros[0];
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 2;
        chefe.AddComponent<Animator>().runtimeAnimatorController = controller;

        Rigidbody2D rb = chefe.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4f;
        rb.mass = 5f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = chefe.AddComponent<BoxCollider2D>();
        col.size = colisor;   // em unidades locais: a escala do transform amplia junto
        col.offset = offset;

        Boss boss = chefe.AddComponent<Boss>();
        SerializedObject so = new SerializedObject(boss);
        so.FindProperty("nome").stringValue = nomeExibido;
        so.FindProperty("vidaMaxima").intValue = vida;
        so.FindProperty("velocidade").floatValue = velocidade;
        so.FindProperty("velocidadeInvestida").floatValue = investida;
        so.FindProperty("forcaPulo").floatValue = pulo;
        so.FindProperty("intervaloEntreAcoes").floatValue = intervalo;
        so.FindProperty("spriteOlhaParaEsquerda").boolValue = olhaEsquerda;
        so.FindProperty("camadaChao").intValue = MascaraDe("Ground");
        so.FindProperty("projetil").objectReferenceValue = projetil;
        so.FindProperty("bocaDoTiro").vector2Value = boca;
        so.FindProperty("alturaMinimaDoPisao").floatValue = colisor.y * escala * 0.3f;
        so.FindProperty("efeitoMorte").objectReferenceValue = fxMorte;
        so.ApplyModifiedProperties();

        Salvar(chefe, nome);
    }

    private static GameObject CriarProjetil(GameObject fxImpacto)
    {
        Sprite[] quadros = AnimationBuilder.QuadrosDaPasta(Art + "/Grotto/Enemies/Lizard/Fireball", "fireball");
        AnimationClip clip = AnimationBuilder.CriarClip("BolaDeFogo_Voar", quadros, 12f, true);
        AnimatorController controller = AnimationBuilder.CriarControllerLoop("BolaDeFogo", clip);

        GameObject bola = new GameObject("BolaDeFogo");
        bola.tag = "Hazard";
        bola.layer = LayerMask.NameToLayer("Hazard");
        bola.transform.localScale = Vector3.one * 1.6f;

        SpriteRenderer sr = bola.AddComponent<SpriteRenderer>();
        sr.sprite = quadros.Length > 0 ? quadros[0] : null;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 3;
        bola.AddComponent<Animator>().runtimeAnimatorController = controller;

        Rigidbody2D rb = bola.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = bola.AddComponent<CircleCollider2D>();
        col.radius = 0.28f;
        col.isTrigger = true;

        Projetil projetil = bola.AddComponent<Projetil>();
        SerializedObject so = new SerializedObject(projetil);
        so.FindProperty("camadasQueDestroem").intValue = MascaraDe("Ground");
        so.FindProperty("efeitoImpacto").objectReferenceValue = fxImpacto;
        so.ApplyModifiedProperties();

        return Salvar(bola, "BolaDeFogo");
    }

    // ----------------- Inimigos -----------------

    private static void CriarInimigos(GameObject fxMorte)
    {
        string sl = Art + "/SunnyLand/Enemies";
        string gr = Art + "/Grotto/Enemies";
        string wi = Art + "/Winter/Enemies";

        // Colisores centrados no X: o inimigo vira com flipX, que espelha o desenho
        // mas nao o colisor, entao um deslocamento lateral ficaria errado num dos lados.
        // Floresta
        CriarInimigo("Gamba", AnimationBuilder.QuadrosDaPasta(sl + "/Opossum", "opossum-"), 10f,
            new Vector2(1.6f, 1.3f), new Vector2(0f, -0.05f), 2.5f, voador: false, pisavel: true, fxMorte);
        CriarInimigo("Aguia", AnimationBuilder.QuadrosDaPasta(sl + "/Eagle", "eagle-attack-"), 10f,
            new Vector2(1.8f, 2.0f), new Vector2(-0.1f, 0f), 3f, voador: true, pisavel: true, fxMorte);

        // Caverna
        CriarInimigo("Caranguejo", AnimationBuilder.QuadrosDaPasta(gr + "/Crab", "crab-walk"), 10f,
            new Vector2(2.2f, 1.3f), new Vector2(0f, -0.3f), 2f, voador: false, pisavel: true, fxMorte);
        CriarInimigo("Esqueleto", AnimationBuilder.QuadrosDaPasta(gr + "/Skeleton", "skeleton-walk"), 10f,
            new Vector2(0.8f, 1.6f), new Vector2(0.05f, -0.2f), 1.8f, voador: false, pisavel: true, fxMorte, olhaEsquerda: false);
        CriarInimigo("Gosma", AnimationBuilder.QuadrosDaPasta(gr + "/Slime", "slime"), 8f,
            new Vector2(1.3f, 1.2f), new Vector2(0f, -0.35f), 1.5f, voador: false, pisavel: true, fxMorte);
        CriarInimigo("Morcego", AnimationBuilder.QuadrosDaPasta(gr + "/Bat", "bat"), 12f,
            new Vector2(1.2f, 1.6f), new Vector2(-0.1f, 0f), 3f, voador: true, pisavel: true, fxMorte);
        CriarInimigo("OlhoVoador", AnimationBuilder.QuadrosDaPasta(gr + "/FlyEye", "fly-eye"), 10f,
            new Vector2(1.4f, 1.4f), Vector2.zero, 2.5f, voador: true, pisavel: true, fxMorte);
        CriarInimigo("DemonioMini", AnimationBuilder.QuadrosDaPasta(gr + "/MiniDemon", "run"), 10f,
            new Vector2(1.6f, 1.9f), new Vector2(0f, -0.15f), 3.5f, voador: false, pisavel: true, fxMorte);
        CriarInimigo("Fantasma", AnimationBuilder.QuadrosDaPasta(gr + "/Ghost", "ghost"), 8f,
            new Vector2(1.2f, 1.3f), Vector2.zero, 2f, voador: true, pisavel: false, fxMorte);

        // Inverno
        CriarInimigo("Raposa", AnimationBuilder.QuadrosDaPasta(wi + "/Fox", "fox-"), 12f,
            new Vector2(3.0f, 1.7f), new Vector2(0f, -0.1f), 4f, voador: false, pisavel: true, fxMorte, olhaEsquerda: false);
        CriarInimigo("Yeti", AnimationBuilder.QuadrosDaPasta(wi + "/Yeti", "yeti-"), 8f,
            new Vector2(1.5f, 1.6f), new Vector2(0f, -0.15f), 1.8f, voador: false, pisavel: true, fxMorte, olhaEsquerda: false);
        CriarInimigo("Coruja", AnimationBuilder.QuadrosDaPasta(wi + "/Owl", "owl-"), 10f,
            new Vector2(2.2f, 2.2f), new Vector2(0f, 0.1f), 2.5f, voador: true, pisavel: true, fxMorte);
    }

    /// <param name="olhaEsquerda">
    /// Para que lado o desenho original aponta. Conferido na prancha dos primeiros
    /// quadros: quase todos olham para a esquerda; esqueleto, raposa e yeti, para a direita.
    /// </param>
    private static void CriarInimigo(string nome, Sprite[] quadros, float fps, Vector2 tamanho, Vector2 offset,
        float velocidade, bool voador, bool pisavel, GameObject fxMorte, bool olhaEsquerda = true)
    {
        if (quadros.Length == 0)
        {
            Debug.LogWarning("[Setup] Inimigo " + nome + " sem quadros; prefab nao criado.");
            return;
        }

        AnimationClip andar = AnimationBuilder.CriarClip(nome + "_Andar", quadros, fps, true);
        AnimatorController controller = AnimationBuilder.CriarControllerLoop(nome, andar);

        GameObject inimigo = new GameObject(nome);
        inimigo.tag = "Enemy";
        inimigo.layer = LayerMask.NameToLayer("Enemy");

        SpriteRenderer sr = inimigo.AddComponent<SpriteRenderer>();
        sr.sprite = quadros[0];
        sr.sortingLayerName = "Player";

        inimigo.AddComponent<Animator>().runtimeAnimatorController = controller;

        Rigidbody2D rb = inimigo.AddComponent<Rigidbody2D>();
        rb.gravityScale = voador ? 0f : 4f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = inimigo.AddComponent<BoxCollider2D>();
        col.size = tamanho;
        col.offset = offset;
        // Voadores nao empurram o jogador: o contato e so pelo trigger.
        col.isTrigger = voador;

        EnemyPatrol patrulha = inimigo.AddComponent<EnemyPatrol>();
        SerializedObject soPatrulha = new SerializedObject(patrulha);
        soPatrulha.FindProperty("velocidade").floatValue = velocidade;
        soPatrulha.FindProperty("voador").boolValue = voador;
        soPatrulha.FindProperty("spriteOlhaParaEsquerda").boolValue = olhaEsquerda;
        soPatrulha.FindProperty("camadaChao").intValue = MascaraDe("Ground");
        soPatrulha.FindProperty("distanciaPatrulha").floatValue = 3f;

        if (!voador)
        {
            GameObject detector = new GameObject("DetectorBorda");
            detector.transform.SetParent(inimigo.transform);
            detector.transform.localPosition = new Vector3(
                offset.x + tamanho.x / 2f + 0.1f,
                offset.y - tamanho.y / 2f + 0.1f, 0f);
            soPatrulha.FindProperty("detectorBorda").objectReferenceValue = detector.transform;
        }

        soPatrulha.ApplyModifiedProperties();

        EnemyDamage dano = inimigo.AddComponent<EnemyDamage>();
        SerializedObject soDano = new SerializedObject(dano);
        soDano.FindProperty("podeSerPisado").boolValue = pisavel;
        soDano.FindProperty("alturaMinimaDoPisao").floatValue = tamanho.y * 0.3f;
        soDano.FindProperty("efeitoMorte").objectReferenceValue = fxMorte;
        soDano.ApplyModifiedProperties();

        Salvar(inimigo, nome);
    }

    // ----------------- Armadilhas -----------------

    private static void CriarArmadilhas()
    {
        string traps = Art + "/PixelAdventure/Traps";

        // Serra: gira e patrulha; nao pode ser pisada.
        {
            Sprite[] quadros = AnimationBuilder.QuadrosFatiados(traps + "/Saw/On (38x38).png");
            AnimationClip girar = AnimationBuilder.CriarClip("Serra_Girar", quadros, 18f, true);
            AnimatorController controller = AnimationBuilder.CriarControllerLoop("Serra", girar);

            GameObject serra = new GameObject("Serra");
            serra.tag = "Hazard";
            serra.layer = LayerMask.NameToLayer("Hazard");

            SpriteRenderer sr = serra.AddComponent<SpriteRenderer>();
            sr.sprite = quadros.Length > 0 ? quadros[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 1;
            serra.AddComponent<Animator>().runtimeAnimatorController = controller;

            Rigidbody2D rb = serra.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            CircleCollider2D col = serra.AddComponent<CircleCollider2D>();
            col.radius = 1f;
            col.isTrigger = true;

            EnemyPatrol patrulha = serra.AddComponent<EnemyPatrol>();
            SerializedObject soP = new SerializedObject(patrulha);
            soP.FindProperty("velocidade").floatValue = 2.5f;
            soP.FindProperty("voador").boolValue = true;
            soP.FindProperty("balancoVertical").floatValue = 0f;
            soP.FindProperty("distanciaPatrulha").floatValue = 2.5f;
            soP.ApplyModifiedProperties();

            EnemyDamage dano = serra.AddComponent<EnemyDamage>();
            SerializedObject soD = new SerializedObject(dano);
            soD.FindProperty("podeSerPisado").boolValue = false;
            soD.FindProperty("imuneAEspada").boolValue = true; // e uma serra, nao um bicho
            soD.ApplyModifiedProperties();

            Salvar(serra, "Serra");
        }

        // Espinhos: tiram 1 de vida.
        {
            GameObject espinhos = new GameObject("Espinhos");
            espinhos.tag = "Hazard";
            espinhos.layer = LayerMask.NameToLayer("Hazard");

            SpriteRenderer sr = espinhos.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(traps + "/Spikes/Idle.png");
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = 2;

            BoxCollider2D col = espinhos.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.5f);
            col.offset = new Vector2(0f, -0.25f);
            col.isTrigger = true;

            Hazard perigo = espinhos.AddComponent<Hazard>();
            SerializedObject so = new SerializedObject(perigo);
            so.FindProperty("mataInstantaneamente").boolValue = false;
            so.FindProperty("dano").intValue = 1;
            so.ApplyModifiedProperties();

            Salvar(espinhos, "Espinhos");
        }

        // Fogo: chama animada, tira 1 de vida.
        {
            Sprite[] quadros = AnimationBuilder.QuadrosFatiados(traps + "/Fire/On (16x32).png");
            AnimationClip queimar = AnimationBuilder.CriarClip("Fogo_Queimar", quadros, 10f, true);
            AnimatorController controller = AnimationBuilder.CriarControllerLoop("Fogo", queimar);

            GameObject fogo = new GameObject("Fogo");
            fogo.tag = "Hazard";
            fogo.layer = LayerMask.NameToLayer("Hazard");

            SpriteRenderer sr = fogo.AddComponent<SpriteRenderer>();
            sr.sprite = quadros.Length > 0 ? quadros[0] : null;
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = 2;
            fogo.AddComponent<Animator>().runtimeAnimatorController = controller;

            // Sprite de 16x32: a chama fica na metade de cima, a base e o queimador.
            BoxCollider2D col = fogo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.7f, 1.0f);
            col.offset = new Vector2(0f, 0.3f);
            col.isTrigger = true;

            Hazard perigo = fogo.AddComponent<Hazard>();
            SerializedObject so = new SerializedObject(perigo);
            so.FindProperty("mataInstantaneamente").boolValue = false;
            so.FindProperty("dano").intValue = 1;
            so.ApplyModifiedProperties();

            Salvar(fogo, "Fogo");
        }

        // Trampolim: solido, lanca para cima e toca a animacao de mola.
        {
            Sprite parado = AssetDatabase.LoadAssetAtPath<Sprite>(traps + "/Trampoline/Idle.png");
            Sprite[] quadrosPulo = AnimationBuilder.QuadrosFatiados(traps + "/Trampoline/Jump (28x28).png");

            AnimationClip idle = AnimationBuilder.CriarClip("Trampolim_Idle", new[] { parado }, 1f, true);
            AnimationClip pulo = AnimationBuilder.CriarClip("Trampolim_Pulo", quadrosPulo, 20f, false);
            AnimatorController controller = AnimationBuilder.CriarControllerAtivavel("Trampolim", idle, pulo, null);

            GameObject trampolim = new GameObject("Trampolim");
            // Camada propria: o jogador pisa nele (esta na mascara de chao dele),
            // mas os inimigos atravessam (PhysicsSetup ignora Enemy x OneWayPlatform).
            // Senao a raposa, com 3 tiles de largura, subia no trampolim e ficava
            // presa em cima dele virando de um lado para o outro.
            trampolim.layer = LayerMask.NameToLayer("OneWayPlatform");

            SpriteRenderer sr = trampolim.AddComponent<SpriteRenderer>();
            sr.sprite = parado;
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = 2;
            trampolim.AddComponent<Animator>().runtimeAnimatorController = controller;

            // Quadro 28x28 com a mola nos 11 px de baixo.
            BoxCollider2D col = trampolim.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 0.5f);
            col.offset = new Vector2(0f, -0.55f);

            Trampoline mola = trampolim.AddComponent<Trampoline>();
            SerializedObject so = new SerializedObject(mola);
            so.FindProperty("forca").floatValue = 30f;
            so.ApplyModifiedProperties();

            Salvar(trampolim, "Trampolim");
        }
    }

    // ----------------- Itens -----------------

    private static void CriarItens(GameObject fxColeta)
    {
        // Diamante = moeda (muitos pela fase). Fruta = cura 1 coracao (raras).
        CriarItem("Gema", AnimationBuilder.QuadrosDaPasta(Art + "/SunnyLand/Items/Gem", "gem-"), 10f, TipoDeItem.Moeda, 1, fxColeta);
        CriarItem("Cereja", AnimationBuilder.QuadrosDaPasta(Art + "/SunnyLand/Items/Cherry", "cherry-"), 10f, TipoDeItem.Cura, 1, fxColeta);
    }

    private static void CriarItem(string nome, Sprite[] quadros, float fps, TipoDeItem tipo, int valor, GameObject fx)
    {
        AnimationClip clip = AnimationBuilder.CriarClip(nome + "_Brilhar", quadros, fps, true);
        AnimatorController controller = AnimationBuilder.CriarControllerLoop(nome, clip);

        GameObject item = new GameObject(nome);
        item.tag = "Collectible";

        SpriteRenderer sr = item.AddComponent<SpriteRenderer>();
        sr.sprite = quadros.Length > 0 ? quadros[0] : null;
        sr.sortingLayerName = "Midground";
        sr.sortingOrder = 3;
        item.AddComponent<Animator>().runtimeAnimatorController = controller;

        CircleCollider2D col = item.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        Collectible coletavel = item.AddComponent<Collectible>();
        SerializedObject so = new SerializedObject(coletavel);
        so.FindProperty("tipo").enumValueIndex = (int)tipo;
        so.FindProperty("valor").intValue = valor;
        so.FindProperty("efeitoVisual").objectReferenceValue = fx;
        so.ApplyModifiedProperties();

        Salvar(item, nome);
    }

    // ----------------- Marcadores -----------------

    private static void CriarMarcadores()
    {
        string cps = Art + "/PixelAdventure/Items/Checkpoints";

        {
            Sprite semBandeira = AssetDatabase.LoadAssetAtPath<Sprite>(cps + "/Checkpoint/Checkpoint (No Flag).png");
            Sprite[] hasteando = AnimationBuilder.QuadrosFatiados(cps + "/Checkpoint/Checkpoint (Flag Out) (64x64).png");
            Sprite[] tremulando = AnimationBuilder.QuadrosFatiados(cps + "/Checkpoint/Checkpoint (Flag Idle)(64x64).png");

            AnimationClip idle = AnimationBuilder.CriarClip("Checkpoint_Idle", new[] { semBandeira }, 1f, true);
            AnimationClip ativo = AnimationBuilder.CriarClip("Checkpoint_Hastear", hasteando, 24f, false);
            AnimationClip depois = AnimationBuilder.CriarClip("Checkpoint_Tremular", tremulando, 12f, true);
            AnimatorController controller = AnimationBuilder.CriarControllerAtivavel("Checkpoint", idle, ativo, depois);

            GameObject checkpoint = new GameObject("Checkpoint");
            checkpoint.tag = "Checkpoint";

            SpriteRenderer sr = checkpoint.AddComponent<SpriteRenderer>();
            sr.sprite = semBandeira;
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = 2;
            checkpoint.AddComponent<Animator>().runtimeAnimatorController = controller;

            // Um "feixe" de 10 tiles a partir da base da haste: passar por cima
            // pulando tambem conta. (A base continua em -1,75, onde o poste toca o chao.)
            BoxCollider2D col = checkpoint.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 10f);
            col.offset = new Vector2(0f, 3.25f);
            col.isTrigger = true;

            Checkpoint script = checkpoint.AddComponent<Checkpoint>();
            SerializedObject so = new SerializedObject(script);
            so.FindProperty("deslocamentoVertical").floatValue = 0f;
            so.ApplyModifiedProperties();

            Salvar(checkpoint, "Checkpoint");
        }

        {
            Sprite parado = AssetDatabase.LoadAssetAtPath<Sprite>(cps + "/End/End (Idle).png");
            Sprite[] pressionado = AnimationBuilder.QuadrosFatiados(cps + "/End/End (Pressed) (64x64).png");

            AnimationClip idle = AnimationBuilder.CriarClip("FimDeFase_Idle", new[] { parado }, 1f, true);
            AnimationClip ativo = AnimationBuilder.CriarClip("FimDeFase_Ativar", pressionado, 14f, false);
            AnimatorController controller = AnimationBuilder.CriarControllerAtivavel("FimDeFase", idle, ativo, null);

            GameObject fim = new GameObject("FimDeFase");
            fim.tag = "LevelEnd";

            SpriteRenderer sr = fim.AddComponent<SpriteRenderer>();
            sr.sprite = parado;
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = 2;
            fim.AddComponent<Animator>().runtimeAnimatorController = controller;

            BoxCollider2D col = fim.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 2f);
            col.offset = new Vector2(0f, -0.6f);
            col.isTrigger = true;

            fim.AddComponent<LevelEnd>();

            Salvar(fim, "FimDeFase");
        }
    }

    // ----------------- Plataformas moveis -----------------

    private static void CriarPlataformas()
    {
        // Floresta e inverno: tabua de madeira do Sunny Land (32x16 px = 2x1 tiles).
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/SunnyLand/Props/platform-long.png");
            GameObject p = NovaPlataforma("PlataformaMovel", sprite, new Vector2(2f, 0.8f), new Vector2(0f, 0.1f));
            Salvar(p, "PlataformaMovel");
        }

        // Caverna: plataforma cinza animada do Pixel Adventure (32x8 px).
        {
            Sprite[] quadros = AnimationBuilder.QuadrosFatiados(Art + "/PixelAdventure/Traps/Platforms/Grey On (32x8).png");
            GameObject p = NovaPlataforma("PlataformaMovelCaverna", quadros.Length > 0 ? quadros[0] : null,
                new Vector2(2f, 0.5f), Vector2.zero);

            if (quadros.Length > 0)
            {
                AnimationClip clip = AnimationBuilder.CriarClip("PlataformaCaverna_Flutuar", quadros, 8f, true);
                p.AddComponent<Animator>().runtimeAnimatorController = AnimationBuilder.CriarControllerLoop("PlataformaCaverna", clip);
            }

            Salvar(p, "PlataformaMovelCaverna");
        }
    }

    private static GameObject NovaPlataforma(string nome, Sprite sprite, Vector2 tamanho, Vector2 offset)
    {
        GameObject plataforma = new GameObject(nome);
        plataforma.tag = "MovingPlatform";
        plataforma.layer = LayerMask.NameToLayer("Ground");

        SpriteRenderer sr = plataforma.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Midground";
        sr.sortingOrder = 2;

        BoxCollider2D col = plataforma.AddComponent<BoxCollider2D>();
        col.size = tamanho;
        col.offset = offset;

        Rigidbody2D rb = plataforma.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        plataforma.AddComponent<MovingPlatform>();
        return plataforma;
    }

    // ----------------- Loja -----------------

    /// <summary>Raposo vendedor + placa + aviso da tecla E, com a area de interacao.</summary>
    private static void CriarLoja()
    {
        UiArtGenerator.Garantir();

        Sprite[] quadros = AnimationBuilder.QuadrosDaPasta(Art + "/SunnyLand/NPC/Foxy", "f-");
        AnimationClip idle = AnimationBuilder.CriarClip("Loja_Raposo_Idle", quadros, 6f, true);
        AnimatorController controller = AnimationBuilder.CriarControllerLoop("LojaRaposo", idle);

        GameObject loja = new GameObject("Loja");

        // Area em que o aviso aparece e o E funciona. Base em -1 (pes do raposo).
        BoxCollider2D area = loja.AddComponent<BoxCollider2D>();
        area.size = new Vector2(5f, 3f);
        area.offset = new Vector2(0f, 0.5f);
        area.isTrigger = true;

        GameObject vendedor = new GameObject("Raposo");
        vendedor.transform.SetParent(loja.transform);
        vendedor.transform.localPosition = new Vector3(0.6f, 0f, 0f);
        SpriteRenderer srVendedor = vendedor.AddComponent<SpriteRenderer>();
        srVendedor.sprite = quadros.Length > 0 ? quadros[0] : null;
        srVendedor.sortingLayerName = "Player";
        srVendedor.sortingOrder = 1;
        vendedor.AddComponent<Animator>().runtimeAnimatorController = controller;

        // Placa de madeira do Sunny Land (18x20 px) ao lado, com a base no chao.
        GameObject placa = new GameObject("Placa");
        placa.transform.SetParent(loja.transform);
        placa.transform.localPosition = new Vector3(-1.1f, -0.375f, 0f);
        SpriteRenderer srPlaca = placa.AddComponent<SpriteRenderer>();
        srPlaca.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/SunnyLand/Props/sign.png");
        srPlaca.sortingLayerName = "Midground";
        srPlaca.sortingOrder = 3;

        // Keycap "E" flutuando sobre o raposo; o script liga/desliga e balanca.
        GameObject aviso = new GameObject("Aviso");
        aviso.transform.SetParent(loja.transform);
        aviso.transform.localPosition = new Vector3(0.6f, 1.7f, 0f);
        aviso.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer srAviso = aviso.AddComponent<SpriteRenderer>();
        srAviso.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiArtGenerator.CaminhoTeclaE);
        srAviso.sortingLayerName = "Foreground";

        Shop script = loja.AddComponent<Shop>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("vendedor").objectReferenceValue = srVendedor;
        so.FindProperty("aviso").objectReferenceValue = srAviso;
        so.FindProperty("vendedorOlhaParaDireita").boolValue = true;
        so.ApplyModifiedProperties();

        Salvar(loja, "Loja");
    }

    // ----------------- Efeitos -----------------

    private static GameObject CriarFx(string nome, string pasta, string prefixo, float fps)
    {
        Sprite[] quadros = AnimationBuilder.QuadrosDaPasta(pasta, prefixo);
        AnimationClip clip = AnimationBuilder.CriarClip(nome, quadros, fps, false);
        AnimatorController controller = AnimationBuilder.CriarControllerLoop(nome, clip);

        GameObject fx = new GameObject(nome);
        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = quadros.Length > 0 ? quadros[0] : null;
        sr.sortingLayerName = "Foreground";
        fx.AddComponent<Animator>().runtimeAnimatorController = controller;
        fx.AddComponent<DestroyAfterAnimation>();

        return Salvar(fx, nome);
    }

    // ----------------- Utilidades -----------------

    private static PhysicsMaterial2D MaterialSemAtrito()
    {
        const string pasta = "Assets/_Game/Physics";
        const string caminho = pasta + "/SemAtrito.physicsMaterial2D";

        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(caminho);
        if (material != null)
            return material;

        if (!AssetDatabase.IsValidFolder(pasta))
            AssetDatabase.CreateFolder("Assets/_Game", "Physics");

        material = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(material, caminho);
        return material;
    }

    private static int MascaraDe(params string[] nomesDeCamadas)
    {
        int mascara = 0;
        foreach (string nome in nomesDeCamadas)
        {
            int indice = LayerMask.NameToLayer(nome);
            if (indice >= 0)
                mascara |= 1 << indice;
        }
        return mascara;
    }

    private static GameObject Salvar(GameObject objeto, string nome)
    {
        string caminho = PastaPrefabs + "/" + nome + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(objeto, caminho);
        Object.DestroyImmediate(objeto);
        return prefab;
    }
}
