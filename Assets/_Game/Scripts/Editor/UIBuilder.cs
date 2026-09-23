using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Monta as telas do jogo (HUD com coracoes, menu principal e pausa) com a
/// fonte pixel e os botoes ja ligados aos metodos.
/// </summary>
public static class UIBuilder
{
    private const string Art = "Assets/_Game/Art";
    private static readonly Vector2 ResolucaoDeReferencia = new Vector2(1920f, 1080f);

    private static readonly Color CorPainel = new Color(0.07f, 0.08f, 0.14f, 0.82f);
    private static readonly Color CorBotao = new Color(0.16f, 0.36f, 0.24f, 1f);
    private static readonly Color CorBotaoRealce = new Color(0.24f, 0.52f, 0.34f, 1f);
    private static readonly Color CorTexto = new Color(0.98f, 0.96f, 0.86f, 1f);
    private static readonly Color CorTitulo = new Color(1f, 0.85f, 0.3f, 1f);

    private static TMP_FontAsset fonte;

    // ----------------- Estruturas basicas -----------------

    /// <param name="primeiroBotao">
    /// Botao que ja nasce selecionado. Sem isso o controle nao tem por onde
    /// comecar a navegar o menu (o mouse funciona, o gamepad nao).
    /// </param>
    public static void CriarEventSystem(GameObject primeiroBotao = null)
    {
        EventSystem existente = Object.FindFirstObjectByType<EventSystem>();

        if (existente == null)
        {
            GameObject go = new GameObject("EventSystem");
            existente = go.AddComponent<EventSystem>();
            // Modulo antigo: funciona com o projeto em "Both" sem precisar de InputActions.
            StandaloneInputModule modulo = go.AddComponent<StandaloneInputModule>();
            modulo.horizontalAxis = "Horizontal";
            modulo.verticalAxis = "Vertical";
            modulo.submitButton = "Submit";
            modulo.cancelButton = "Cancel";
        }

        if (primeiroBotao != null)
        {
            SerializedObject so = new SerializedObject(existente);
            so.FindProperty("m_FirstSelected").objectReferenceValue = primeiroBotao;
            so.ApplyModifiedProperties();
        }
    }

    private static Canvas CriarCanvas(string nome)
    {
        fonte = FontBuilder.Garantir();
        UiArtGenerator.Garantir();

        GameObject go = new GameObject(nome);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escala = go.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = ResolucaoDeReferencia;
        escala.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // =====================================================================
    // HUD
    // =====================================================================

    public static void CriarHUD(NivelDef nivel)
    {
        Canvas canvas = CriarCanvas("Canvas HUD");

        // Coracoes
        // Cheio = coracao vermelho; vazio = so o contorno branco, que le melhor
        // sobre qualquer fundo do que o coracao preto do pack.
        Sprite cheio = Carregar(Art + "/UI/Hearts/heart.png");
        Sprite vazio = Carregar(Art + "/UI/Hearts/border.png");

        // 5 coracoes: 3 de base + ate 2 comprados na loja (a HUD esconde os que passam do maximo).
        Image[] coracoes = new Image[3 + GameManager.MaximoDeCoracoesExtras];
        for (int i = 0; i < coracoes.Length; i++)
            coracoes[i] = CriarImagem(canvas.transform, "Coracao" + i, cheio, new Vector2(0f, 1f), new Vector2(40f + i * 78f, -40f), new Vector2(68f, 68f));

        // Diamantes (moeda)
        Sprite gema = Carregar(Art + "/SunnyLand/Items/Gem/gem-1.png");
        CriarImagem(canvas.transform, "IconeMoeda", gema, new Vector2(0f, 1f), new Vector2(40f, -124f), new Vector2(60f, 60f));
        TMP_Text textoMoedas = CriarTexto(canvas.transform, "TextoMoedas", "0", new Vector2(0f, 1f), new Vector2(112f, -128f), TextAlignmentOptions.Left, 40f);

        // Tentativas
        Sprite heroi = Carregar(Art + "/Adventurer/adventurer-idle-00.png");
        CriarImagem(canvas.transform, "IconeVidas", heroi, new Vector2(0f, 1f), new Vector2(30f, -200f), new Vector2(100f, 74f));
        TMP_Text textoTentativas = CriarTexto(canvas.transform, "TextoTentativas", "x3", new Vector2(0f, 1f), new Vector2(112f, -212f), TextAlignmentOptions.Left, 40f);

        HUDController hud = canvas.gameObject.AddComponent<HUDController>();
        SerializedObject so = new SerializedObject(hud);
        SerializedProperty lista = so.FindProperty("imagensCoracoes");
        lista.arraySize = coracoes.Length;
        for (int i = 0; i < coracoes.Length; i++)
            lista.GetArrayElementAtIndex(i).objectReferenceValue = coracoes[i];
        so.FindProperty("coracaoCheio").objectReferenceValue = cheio;
        so.FindProperty("coracaoVazio").objectReferenceValue = vazio;
        so.FindProperty("textoMoedas").objectReferenceValue = textoMoedas;
        so.FindProperty("textoTentativas").objectReferenceValue = textoTentativas;
        so.ApplyModifiedProperties();

        CriarBarraDoChefe(canvas);
        CriarPainelDePausa(canvas);
        CriarLoja(canvas);

        if (nivel != null)
        {
            CriarCartaoDeFase(canvas, nivel);

            if (nivel.cena == "Tutorial")
                CriarGuiaDoTutorial(canvas);
        }

        CriarEventSystem();
    }

    /// <summary>Canto inferior direito da cutscene: keycap ESPACO + "PULAR".</summary>
    public static Canvas CriarAvisoDePular()
    {
        Canvas canvas = CriarCanvas("Canvas Cutscene");

        GameObject grupo = new GameObject("AvisoPular", typeof(RectTransform));
        grupo.transform.SetParent(canvas.transform, false);
        RectTransform rect = grupo.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-50f, 50f);
        rect.sizeDelta = new Vector2(420f, 90f);

        // Keycap que troca sozinho entre ESPACO (teclado) e A (controle).
        GameObject tecla = CriarTeclaAjustavel(grupo.transform, new Vector2(-130f, 0f));

        TMP_Text rotulo = CriarTexto(grupo.transform, "Rotulo", "PULAR",
            new Vector2(0.5f, 0.5f), new Vector2(60f, 0f), TextAlignmentOptions.Left, 26f, new Color(1f, 1f, 1f, 0.85f));
        rotulo.rectTransform.pivot = new Vector2(0f, 0.5f);
        rotulo.rectTransform.sizeDelta = new Vector2(200f, 60f);

        CriarEventSystem();
        return canvas;
    }

    /// <summary>Tela da loja: saldo, tres itens com icone/descricao/preco e botao de compra.</summary>
    private static void CriarLoja(Canvas canvas)
    {
        GameObject painel = CriarPainelEscuro(canvas.transform, "PainelLoja");
        GameObject caixa = CriarCaixa(painel.transform, "Caixa", new Vector2(1000f, 720f));

        CriarTexto(caixa.transform, "Titulo", "LOJA DO RAPOSO", new Vector2(0.5f, 1f), new Vector2(0f, -50f), TextAlignmentOptions.Center, 44f, CorTitulo)
            .rectTransform.sizeDelta = new Vector2(900f, 70f);

        // Saldo: diamante + numero, no canto superior direito da caixa.
        CriarImagem(caixa.transform, "IconeSaldo", Carregar(Art + "/SunnyLand/Items/Gem/gem-1.png"), new Vector2(1f, 1f), new Vector2(-150f, -110f), new Vector2(52f, 52f));
        TMP_Text saldo = CriarTexto(caixa.transform, "Saldo", "0", new Vector2(1f, 1f), new Vector2(-40f, -110f), TextAlignmentOptions.Right, 34f);
        saldo.rectTransform.sizeDelta = new Vector2(100f, 60f);

        string[] icones =
        {
            Art + "/SunnyLand/Items/Cherry/cherry-1.png",
            Art + "/Adventurer/adventurer-idle-00.png",
            Art + "/UI/Hearts/heart.png",
        };

        Button[] botoes = new Button[ShopUI.Itens.Length];
        TMP_Text[] status = new TMP_Text[ShopUI.Itens.Length];

        for (int i = 0; i < ShopUI.Itens.Length; i++)
        {
            ShopUI.ItemDaLoja item = ShopUI.Itens[i];
            float y = 80f - i * 130f;

            // Faixa de fundo da linha, para separar os itens.
            Image faixa = CriarImagem(caixa.transform, "Faixa" + i, null, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(900f, 110f));
            faixa.color = new Color(1f, 1f, 1f, 0.05f);

            CriarImagem(caixa.transform, "Icone" + i, Carregar(icones[i]), new Vector2(0.5f, 0.5f), new Vector2(-390f, y), new Vector2(84f, 84f));

            TMP_Text nome = CriarTexto(caixa.transform, "Nome" + i, item.nome, new Vector2(0.5f, 0.5f), new Vector2(-330f, y + 20f), TextAlignmentOptions.Left, 26f, CorTitulo);
            nome.rectTransform.pivot = new Vector2(0f, 0.5f);
            nome.rectTransform.sizeDelta = new Vector2(420f, 40f);

            TMP_Text descricao = CriarTexto(caixa.transform, "Descricao" + i, item.descricao, new Vector2(0.5f, 0.5f), new Vector2(-330f, y - 18f), TextAlignmentOptions.Left, 18f, new Color(0.8f, 0.8f, 0.85f));
            descricao.rectTransform.pivot = new Vector2(0f, 0.5f);
            descricao.rectTransform.sizeDelta = new Vector2(420f, 40f);

            CriarImagem(caixa.transform, "IconePreco" + i, Carregar(Art + "/SunnyLand/Items/Gem/gem-1.png"), new Vector2(0.5f, 0.5f), new Vector2(120f, y), new Vector2(40f, 40f));
            TMP_Text preco = CriarTexto(caixa.transform, "Preco" + i, item.preco.ToString(), new Vector2(0.5f, 0.5f), new Vector2(150f, y), TextAlignmentOptions.Left, 30f);
            preco.rectTransform.pivot = new Vector2(0f, 0.5f);
            preco.rectTransform.sizeDelta = new Vector2(100f, 50f);

            Button botao = CriarBotao(caixa.transform, "BotaoComprar" + i, "COMPRAR  [" + (i + 1) + "]", new Vector2(330f, y));
            botao.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 72f);
            Transform textoDoBotao = botao.transform.Find("Texto");
            foreach (TMP_Text t in botao.GetComponentsInChildren<TMP_Text>())
            {
                t.rectTransform.sizeDelta = new Vector2(260f, 72f);
                t.fontSize = 20f;
            }

            botoes[i] = botao;
            status[i] = textoDoBotao != null ? textoDoBotao.GetComponent<TMP_Text>() : null;
        }

        Button fechar = CriarBotao(caixa.transform, "BotaoFechar", "FECHAR  [E]", new Vector2(0f, -305f));

        ShopUI loja = canvas.gameObject.AddComponent<ShopUI>();
        SerializedObject so = new SerializedObject(loja);
        so.FindProperty("raiz").objectReferenceValue = painel;
        so.FindProperty("textoSaldo").objectReferenceValue = saldo;

        SerializedProperty listaBotoes = so.FindProperty("botoesComprar");
        SerializedProperty listaStatus = so.FindProperty("textosStatus");
        listaBotoes.arraySize = botoes.Length;
        listaStatus.arraySize = status.Length;
        for (int i = 0; i < botoes.Length; i++)
        {
            listaBotoes.GetArrayElementAtIndex(i).objectReferenceValue = botoes[i];
            listaStatus.GetArrayElementAtIndex(i).objectReferenceValue = status[i];
            UnityEventTools.AddIntPersistentListener(botoes[i].onClick, loja.Comprar, i);
        }
        so.ApplyModifiedProperties();

        UnityEventTools.AddPersistentListener(fechar.onClick, loja.Fechar);

        // Na loja o controle anda entre os tres itens e o botao de fechar.
        Button[] navegaveis = new Button[botoes.Length + 1];
        botoes.CopyTo(navegaveis, 0);
        navegaveis[botoes.Length] = fechar;
        LigarNavegacao(navegaveis, vertical: true);

        painel.SetActive(false);
    }

    /// <summary>"FASE 1 - FLORESTA" com o sprite do bioma; congela o jogo por alguns segundos.</summary>
    private static void CriarCartaoDeFase(Canvas canvas, NivelDef nivel)
    {
        GameObject raiz = CriarPainelTransparente(canvas.transform, "CartaoDeFase");
        Image fundo = raiz.AddComponent<Image>();
        fundo.color = new Color(0.03f, 0.04f, 0.08f, 0.88f);
        CanvasGroup grupo = raiz.AddComponent<CanvasGroup>();

        Sprite icone = string.IsNullOrEmpty(nivel.icone) ? null : Carregar(nivel.icone);
        Image imagem = CriarImagem(raiz.transform, "Icone", icone, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(320f, 320f));

        TMP_Text titulo = CriarTexto(raiz.transform, "Titulo", nivel.titulo, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), TextAlignmentOptions.Center, 72f, CorTitulo);
        titulo.rectTransform.sizeDelta = new Vector2(1400f, 110f);
        TMP_Text subtitulo = CriarTexto(raiz.transform, "Subtitulo", nivel.subtitulo, new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), TextAlignmentOptions.Center, 40f);
        subtitulo.rectTransform.sizeDelta = new Vector2(1400f, 80f);
        CriarTexto(raiz.transform, "Dica", "aperte qualquer tecla", new Vector2(0.5f, 0f), new Vector2(0f, 60f), TextAlignmentOptions.Center, 22f, new Color(0.7f, 0.7f, 0.7f));

        LevelIntroCard cartao = canvas.gameObject.AddComponent<LevelIntroCard>();
        SerializedObject so = new SerializedObject(cartao);
        so.FindProperty("titulo").stringValue = nivel.titulo;
        so.FindProperty("subtitulo").stringValue = nivel.subtitulo;
        so.FindProperty("sprite").objectReferenceValue = icone;
        so.FindProperty("raiz").objectReferenceValue = raiz;
        so.FindProperty("grupo").objectReferenceValue = grupo;
        so.FindProperty("textoTitulo").objectReferenceValue = titulo;
        so.FindProperty("textoSubtitulo").objectReferenceValue = subtitulo;
        so.FindProperty("imagem").objectReferenceValue = imagem;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Balao de fala do tutorial (sprite 9-slice gerado + setinha), posicionado
    /// pelo TutorialGuide acima do jogador. O pivo fica na ponta da seta.
    /// </summary>
    private static void CriarGuiaDoTutorial(Canvas canvas)
    {
        UiArtGenerator.Garantir();
        Color corDoTexto = new Color(0.23f, 0.16f, 0.10f);

        GameObject balao = new GameObject("BalaoTutorial", typeof(RectTransform));
        balao.transform.SetParent(canvas.transform, false);
        RectTransform rect = balao.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(860f, 100f);

        // Setinha: ocupa os 22 px de baixo; a caixa fica em cima dela.
        Image seta = CriarImagem(balao.transform, "Seta", Carregar(UiArtGenerator.CaminhoSeta),
            new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(36f, 26f));
        seta.preserveAspect = false;

        GameObject caixa = new GameObject("Caixa", typeof(RectTransform));
        caixa.transform.SetParent(balao.transform, false);
        Image fundo = caixa.AddComponent<Image>();
        fundo.sprite = Carregar(UiArtGenerator.CaminhoBalao);
        fundo.type = Image.Type.Sliced;
        // Borda do 9-slice em 12 px x (100/16) daria 75 px na tela; /4 deixa ~19 px.
        fundo.pixelsPerUnitMultiplier = 4f;
        fundo.raycastTarget = false;
        RectTransform rc = caixa.GetComponent<RectTransform>();
        rc.anchorMin = new Vector2(0f, 0f);
        rc.anchorMax = new Vector2(1f, 1f);
        rc.offsetMin = new Vector2(0f, 22f);
        rc.offsetMax = new Vector2(0f, 0f);

        TMP_Text texto = CriarTexto(caixa.transform, "Texto", "", new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), TextAlignmentOptions.Center, 26f, corDoTexto);
        texto.rectTransform.sizeDelta = new Vector2(800f, 70f);

        TutorialGuide guia = canvas.gameObject.AddComponent<TutorialGuide>();
        SerializedObject so = new SerializedObject(guia);
        so.FindProperty("balao").objectReferenceValue = rect;
        so.FindProperty("texto").objectReferenceValue = texto;
        so.FindProperty("camadaDaBarreira").intValue = LayerMask.NameToLayer("Ground");
        so.ApplyModifiedProperties();

        balao.SetActive(false);
    }

    /// <summary>Barra de vida do chefe no topo da tela; comeca escondida.</summary>
    private static void CriarBarraDoChefe(Canvas canvas)
    {
        GameObject raiz = new GameObject("BarraDoChefe", typeof(RectTransform));
        raiz.transform.SetParent(canvas.transform, false);
        RectTransform rect = raiz.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -30f);
        rect.sizeDelta = new Vector2(640f, 170f);

        TMP_Text nome = CriarTexto(raiz.transform, "Nome", "CHEFE", new Vector2(0.5f, 1f), new Vector2(0f, 0f), TextAlignmentOptions.Center, 30f, CorTitulo);

        // A barra em si (preenche da esquerda) e a moldura decorada por cima.
        Image preenchimento = CriarImagem(raiz.transform, "Preenchimento",
            Carregar(Art + "/UI/Hearts/health_bar.png"), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(392f, 104f));
        preenchimento.preserveAspect = false;
        preenchimento.type = Image.Type.Filled;
        preenchimento.fillMethod = Image.FillMethod.Horizontal;
        preenchimento.fillOrigin = (int)Image.OriginHorizontal.Left;

        Image moldura = CriarImagem(raiz.transform, "Moldura",
            Carregar(Art + "/UI/Hearts/health_bar_decoration.png"), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(512f, 104f));
        moldura.preserveAspect = false;

        BossHealthBar barra = canvas.gameObject.AddComponent<BossHealthBar>();
        SerializedObject so = new SerializedObject(barra);
        so.FindProperty("raiz").objectReferenceValue = raiz;
        so.FindProperty("preenchimento").objectReferenceValue = preenchimento;
        so.FindProperty("textoNome").objectReferenceValue = nome;
        so.ApplyModifiedProperties();

        raiz.SetActive(false);
    }

    private static void CriarPainelDePausa(Canvas canvas)
    {
        PauseMenu pausa = canvas.gameObject.AddComponent<PauseMenu>();

        GameObject painel = CriarPainelEscuro(canvas.transform, "PainelPausa");
        GameObject caixa = CriarCaixa(painel.transform, "Caixa", new Vector2(700f, 560f));

        CriarTexto(caixa.transform, "Titulo", "PAUSA", new Vector2(0.5f, 1f), new Vector2(0f, -60f), TextAlignmentOptions.Center, 56f, CorTitulo);

        Button continuar = CriarBotao(caixa.transform, "BotaoContinuar", "CONTINUAR", new Vector2(0f, 40f));
        Button reiniciar = CriarBotao(caixa.transform, "BotaoReiniciar", "REINICIAR", new Vector2(0f, -60f));
        Button menu = CriarBotao(caixa.transform, "BotaoMenu", "MENU", new Vector2(0f, -160f));

        UnityEventTools.AddPersistentListener(continuar.onClick, pausa.Continuar);
        UnityEventTools.AddPersistentListener(reiniciar.onClick, pausa.ReiniciarFase);
        UnityEventTools.AddPersistentListener(menu.onClick, pausa.VoltarAoMenu);

        LigarNavegacao(new[] { continuar, reiniciar, menu }, vertical: true);

        SerializedObject so = new SerializedObject(pausa);
        so.FindProperty("painelPausa").objectReferenceValue = painel;
        so.FindProperty("primeiroBotao").objectReferenceValue = continuar.gameObject;
        so.ApplyModifiedProperties();

        painel.SetActive(false);
    }

    // =====================================================================
    // Menu principal
    // =====================================================================

    public static void CriarMenuPrincipal()
    {
        Canvas canvas = CriarCanvas("Canvas Menu");
        MainMenuController controlador = canvas.gameObject.AddComponent<MainMenuController>();

        // Painel principal: titulo em cima, botoes numa caixa no centro.
        GameObject principal = CriarPainelTransparente(canvas.transform, "PainelPrincipal");

        CriarTitulo(principal.transform, "CASTLE KNIGHT", new Vector2(0f, -110f), 104f);

        GameObject caixa = CriarCaixa(principal.transform, "CaixaBotoes", new Vector2(560f, 600f));
        caixa.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -140f);

        Button jogar = CriarBotao(caixa.transform, "BotaoJogar", "JOGAR", new Vector2(0f, 200f));
        Button fases = CriarBotao(caixa.transform, "BotaoFases", "FASES", new Vector2(0f, 100f));
        Button controles = CriarBotao(caixa.transform, "BotaoControles", "CONTROLES", new Vector2(0f, 0f));
        Button creditos = CriarBotao(caixa.transform, "BotaoCreditos", "CREDITOS", new Vector2(0f, -100f));
        Button sair = CriarBotao(caixa.transform, "BotaoSair", "SAIR", new Vector2(0f, -200f));

        // Selecao de fases: um cartao por fase, com o sprite do bioma.
        GameObject painelFases = CriarPainelEscuro(canvas.transform, "PainelFases");
        CriarTexto(painelFases.transform, "Titulo", "ESCOLHA A FASE", new Vector2(0.5f, 1f), new Vector2(0f, -90f), TextAlignmentOptions.Center, 56f, CorTitulo)
            .rectTransform.sizeDelta = new Vector2(1400f, 90f);

        List<NivelDef> niveis = LevelDesigns.Todas();
        float passo = 340f;
        float x0 = -(niveis.Count - 1) * passo / 2f;
        var botoesDeFase = new List<(Button botao, string cena)>();

        for (int i = 0; i < niveis.Count; i++)
        {
            NivelDef n = niveis[i];
            Button cartao = CriarCartaoDeSelecao(painelFases.transform, n, new Vector2(x0 + i * passo, 0f));
            botoesDeFase.Add((cartao, n.cena));
        }

        Button voltarFases = CriarBotao(painelFases.transform, "BotaoVoltar", "VOLTAR", new Vector2(0f, -400f));

        // Painel de controles: teclas desenhadas como keycaps, uma linha por acao.
        GameObject painelControles = CriarPainelEscuro(canvas.transform, "PainelControles");
        GameObject caixaControles = CriarCaixa(painelControles.transform, "Caixa", new Vector2(1000f, 760f));
        CriarTexto(caixaControles.transform, "Titulo", "CONTROLES", new Vector2(0.5f, 1f), new Vector2(0f, -50f), TextAlignmentOptions.Center, 48f, CorTitulo);

        // Tres colunas: teclado | controle | o que faz.
        const float xTeclas = -420f;
        const float xControle = -60f;
        const float xRotulo = 60f;

        CriarTexto(caixaControles.transform, "CabecalhoTeclado", "TECLADO", new Vector2(0.5f, 0.5f), new Vector2(xTeclas + 60f, 285f), TextAlignmentOptions.Center, 18f, new Color(0.7f, 0.7f, 0.75f));
        CriarTexto(caixaControles.transform, "CabecalhoControle", "CONTROLE", new Vector2(0.5f, 0.5f), new Vector2(xControle, 285f), TextAlignmentOptions.Center, 18f, new Color(0.7f, 0.7f, 0.75f));

        // Andar: setas e A / D
        float y = 210f;
        CriarTeclaSeta(caixaControles.transform, esquerda: true, new Vector2(xTeclas, y));
        CriarTeclaSeta(caixaControles.transform, esquerda: false, new Vector2(xTeclas + 80f, y));
        CriarTecla(caixaControles.transform, "A", new Vector2(xTeclas + 176f, y));
        CriarTecla(caixaControles.transform, "D", new Vector2(xTeclas + 252f, y));
        CriarBotaoDoControle(caixaControles.transform, "ANALOG", new Vector2(xControle, y), 150f);
        CriarRotulo(caixaControles.transform, "ANDAR", new Vector2(xRotulo, y));

        y = 110f;
        CriarTecla(caixaControles.transform, "ESPACO", new Vector2(xTeclas + 66f, y), 210f);
        CriarBotaoDoControle(caixaControles.transform, "A", new Vector2(xControle, y));
        CriarRotulo(caixaControles.transform, "PULAR  (de novo no ar: pulo duplo)", new Vector2(xRotulo, y), 22f);

        y = 10f;
        CriarTecla(caixaControles.transform, "L", new Vector2(xTeclas, y));
        CriarBotaoDoControle(caixaControles.transform, "B", new Vector2(xControle, y));
        CriarRotulo(caixaControles.transform, "GOLPE DE ESPADA", new Vector2(xRotulo, y));

        y = -90f;
        CriarTecla(caixaControles.transform, "E", new Vector2(xTeclas, y));
        CriarBotaoDoControle(caixaControles.transform, "Y", new Vector2(xControle, y));
        CriarRotulo(caixaControles.transform, "FALAR COM A LOJA", new Vector2(xRotulo, y));

        y = -180f;
        CriarTecla(caixaControles.transform, "ESC", new Vector2(xTeclas + 24f, y), 124f);
        CriarBotaoDoControle(caixaControles.transform, "START", new Vector2(xControle, y), 130f);
        CriarRotulo(caixaControles.transform, "PAUSAR", new Vector2(xRotulo, y));

        // Legenda dos itens, com os proprios sprites.
        y = -262f;
        CriarImagem(caixaControles.transform, "IconeGema", Carregar(Art + "/SunnyLand/Items/Gem/gem-1.png"), new Vector2(0.5f, 0.5f), new Vector2(xTeclas + 20f, y), new Vector2(56f, 56f));
        CriarRotulo(caixaControles.transform, "DIAMANTE = PONTOS  (50 = VIDA EXTRA)", new Vector2(xTeclas + 60f, y), 20f);
        y = -312f;
        CriarImagem(caixaControles.transform, "IconeCereja", Carregar(Art + "/SunnyLand/Items/Cherry/cherry-1.png"), new Vector2(0.5f, 0.5f), new Vector2(xTeclas + 20f, y), new Vector2(56f, 56f));
        CriarRotulo(caixaControles.transform, "FRUTA = CURA 1 CORACAO  (SO SE FERIDO)", new Vector2(xTeclas + 60f, y), 20f);

        Button voltar1 = CriarBotao(caixaControles.transform, "BotaoVoltar", "VOLTAR", new Vector2(0f, -335f));

        // Painel de creditos
        GameObject painelCreditos = CriarPainelEscuro(canvas.transform, "PainelCreditos");
        GameObject caixaCreditos = CriarCaixa(painelCreditos.transform, "Caixa", new Vector2(1100f, 700f));
        CriarTexto(caixaCreditos.transform, "Titulo", "CREDITOS", new Vector2(0.5f, 1f), new Vector2(0f, -60f), TextAlignmentOptions.Center, 52f, CorTitulo);
        TMP_Text textoCreditos = CriarTexto(caixaCreditos.transform, "Texto",
            "Personagem: Animated Pixel Adventurer - rvros\n\n" +
            "Cenarios e inimigos: SunnyLand, SunnyLand Winter,\nSuper Grotto Escape, GothicVania Church - ansimuz\n\n" +
            "A Bruxa: Witches Pack - 9E0\n\n" +
            "Armadilhas e itens: Pixel Adventure - Pixel Frog\n\n" +
            "Coracoes: Hearts and health bar - VampireGirl\n\n" +
            "Gato: Black Cat Sprites - carysaurus\n\n" +
            "Musica: Pascal Belisle e ansimuz\n\n" +
            "Fonte: Press Start 2P - CodeMan38",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), TextAlignmentOptions.Center, 24f);
        textoCreditos.rectTransform.sizeDelta = new Vector2(1000f, 460f);
        Button voltar2 = CriarBotao(caixaCreditos.transform, "BotaoVoltar", "VOLTAR", new Vector2(0f, -280f));

        UnityEventTools.AddPersistentListener(jogar.onClick, controlador.Jogar);
        UnityEventTools.AddPersistentListener(fases.onClick, controlador.MostrarFases);
        UnityEventTools.AddPersistentListener(controles.onClick, controlador.MostrarControles);
        UnityEventTools.AddPersistentListener(creditos.onClick, controlador.MostrarCreditos);
        UnityEventTools.AddPersistentListener(sair.onClick, controlador.Sair);
        UnityEventTools.AddPersistentListener(voltar1.onClick, controlador.MostrarPrincipal);
        UnityEventTools.AddPersistentListener(voltar2.onClick, controlador.MostrarPrincipal);
        UnityEventTools.AddPersistentListener(voltarFases.onClick, controlador.MostrarPrincipal);

        // Cada cartao chama IrParaFase("NomeDaCena").
        foreach ((Button botao, string cena) in botoesDeFase)
            UnityEventTools.AddStringPersistentListener(botao.onClick, controlador.IrParaFase, cena);

        SerializedObject so = new SerializedObject(controlador);
        so.FindProperty("painelPrincipal").objectReferenceValue = principal;
        so.FindProperty("painelControles").objectReferenceValue = painelControles;
        so.FindProperty("painelCreditos").objectReferenceValue = painelCreditos;
        so.FindProperty("painelFases").objectReferenceValue = painelFases;
        so.ApplyModifiedProperties();

        painelControles.SetActive(false);
        painelCreditos.SetActive(false);
        painelFases.SetActive(false);

        // Navegacao vertical entre os botoes do menu, em ciclo, para o controle
        // andar de JOGAR ate SAIR e voltar.
        LigarNavegacao(new[] { jogar, fases, controles, creditos, sair }, vertical: true);
        LigarNavegacao(botoesDeFase.ConvertAll(b => b.botao).ToArray(), vertical: false);

        CriarEventSystem(jogar.gameObject);
    }

    /// <summary>
    /// Liga os botoes em sequencia (Explicit) para o direcional do controle
    /// andar entre eles. O modo Automatic da Unity erra quando os botoes ficam
    /// dentro de paineis diferentes.
    /// </summary>
    private static void LigarNavegacao(Button[] botoes, bool vertical)
    {
        if (botoes == null || botoes.Length == 0)
            return;

        for (int i = 0; i < botoes.Length; i++)
        {
            if (botoes[i] == null)
                continue;

            Button anterior = botoes[(i - 1 + botoes.Length) % botoes.Length];
            Button proximo = botoes[(i + 1) % botoes.Length];

            Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };

            if (vertical)
            {
                nav.selectOnUp = anterior;
                nav.selectOnDown = proximo;
            }
            else
            {
                nav.selectOnLeft = anterior;
                nav.selectOnRight = proximo;
            }

            botoes[i].navigation = nav;
        }
    }

    /// <summary>Cartao clicavel da selecao de fases: moldura, sprite do bioma e nome.</summary>
    private static Button CriarCartaoDeSelecao(Transform pai, NivelDef nivel, Vector2 posicao)
    {
        GameObject go = new GameObject("Cartao" + nivel.cena, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        // Mesmo painel 9-slice das caixas; o hover tinge de amarelo.
        Image fundo = go.AddComponent<Image>();
        fundo.sprite = Carregar(UiArtGenerator.CaminhoPainel);
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = 2f;

        Button botao = go.AddComponent<Button>();
        botao.targetGraphic = fundo;
        ColorBlock cores = botao.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 0.92f, 0.6f);
        cores.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cores.selectedColor = new Color(1f, 0.92f, 0.6f);
        cores.fadeDuration = 0.05f;
        botao.colors = cores;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(300f, 440f);

        Sprite icone = string.IsNullOrEmpty(nivel.icone) ? null : Carregar(nivel.icone);
        CriarImagem(go.transform, "Icone", icone, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(240f, 240f));

        TMP_Text titulo = CriarTexto(go.transform, "Titulo", nivel.titulo, new Vector2(0.5f, 0f), new Vector2(0f, 90f), TextAlignmentOptions.Center, 28f, CorTitulo);
        titulo.rectTransform.sizeDelta = new Vector2(280f, 50f);
        TMP_Text subtitulo = CriarTexto(go.transform, "Subtitulo", nivel.subtitulo, new Vector2(0.5f, 0f), new Vector2(0f, 40f), TextAlignmentOptions.Center, 18f);
        subtitulo.rectTransform.sizeDelta = new Vector2(280f, 50f);

        return botao;
    }

    // =====================================================================
    // Blocos reutilizaveis
    // =====================================================================

    private static Sprite Carregar(string caminho)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        if (s == null)
            Debug.LogWarning("[UI] Sprite nao encontrado: " + caminho);
        return s;
    }

    private static GameObject CriarPainelEscuro(Transform pai, string nome)
    {
        GameObject painel = CriarPainelTransparente(pai, nome);
        Image fundo = painel.AddComponent<Image>();
        fundo.color = new Color(0f, 0f, 0f, 0.6f);
        return painel;
    }

    private static GameObject CriarPainelTransparente(Transform pai, string nome)
    {
        GameObject painel = new GameObject(nome, typeof(RectTransform));
        painel.transform.SetParent(pai, false);

        RectTransform rect = painel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return painel;
    }

    /// <summary>Caixa centralizada: painel escuro 9-slice com borda creme e cantos chanfrados.</summary>
    private static GameObject CriarCaixa(Transform pai, string nome, Vector2 tamanho)
    {
        GameObject caixa = new GameObject(nome, typeof(RectTransform));
        caixa.transform.SetParent(pai, false);

        Image fundo = caixa.AddComponent<Image>();
        fundo.sprite = Carregar(UiArtGenerator.CaminhoPainel);
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = 2f;   // 1 px do sprite ~ 3 px de tela
        fundo.color = new Color(1f, 1f, 1f, 0.96f);

        RectTransform rect = caixa.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = tamanho;

        return caixa;
    }

    /// <summary>Titulo do jogo: gradiente dourado com sombra deslocada.</summary>
    private static void CriarTitulo(Transform pai, string conteudo, Vector2 posicao, float tamanho)
    {
        TMP_Text sombra = CriarTexto(pai, "TituloSombra", conteudo, new Vector2(0.5f, 1f), posicao + new Vector2(7f, -7f), TextAlignmentOptions.Center, tamanho, new Color(0.1f, 0.06f, 0.02f, 0.85f));
        sombra.rectTransform.sizeDelta = new Vector2(1600f, 160f);

        TMP_Text titulo = CriarTexto(pai, "Titulo", conteudo, new Vector2(0.5f, 1f), posicao, TextAlignmentOptions.Center, tamanho, CorTitulo);
        titulo.rectTransform.sizeDelta = new Vector2(1600f, 160f);
        titulo.enableVertexGradient = true;
        titulo.colorGradient = new VertexGradient(
            new Color(1f, 0.95f, 0.6f),  // topo esquerdo
            new Color(1f, 0.95f, 0.6f),  // topo direito
            new Color(0.95f, 0.55f, 0.15f), // base esquerda
            new Color(0.95f, 0.55f, 0.15f)); // base direita
    }

    /// <summary>Keycap com o nome da tecla escrito em cima.</summary>
    private static void CriarTecla(Transform pai, string rotulo, Vector2 posicao, float largura = 76f)
    {
        GameObject go = new GameObject("Tecla" + rotulo, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        Image imagem = go.AddComponent<Image>();
        imagem.sprite = Carregar(UiArtGenerator.CaminhoTecla);
        imagem.type = Image.Type.Sliced;
        imagem.pixelsPerUnitMultiplier = 2f;
        imagem.raycastTarget = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(largura, 76f);

        // O topo da tecla ocupa os 2/3 de cima; o texto fica centrado nele.
        float tamanhoFonte = rotulo.Length > 1 ? 20f : 28f;
        TMP_Text texto = CriarTexto(go.transform, "Rotulo", rotulo, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), TextAlignmentOptions.Center, tamanhoFonte, new Color(0.17f, 0.16f, 0.24f));
        texto.rectTransform.sizeDelta = new Vector2(largura, 50f);
    }

    /// <summary>Keycap de seta (esquerda ou direita), com a seta ja desenhada no sprite.</summary>
    private static void CriarTeclaSeta(Transform pai, bool esquerda, Vector2 posicao)
    {
        Image imagem = CriarImagem(pai, esquerda ? "TeclaEsq" : "TeclaDir",
            Carregar(esquerda ? UiArtGenerator.CaminhoTeclaEsq : UiArtGenerator.CaminhoTeclaDir),
            new Vector2(0.5f, 0.5f), posicao, new Vector2(76f, 76f));
        imagem.preserveAspect = true;
    }

    /// <summary>
    /// Keycap cujo rotulo e largura acompanham o dispositivo em uso
    /// (ESPACO no teclado, A no controle).
    /// </summary>
    private static GameObject CriarTeclaAjustavel(Transform pai, Vector2 posicao)
    {
        GameObject go = new GameObject("TeclaPular", typeof(RectTransform));
        go.transform.SetParent(pai, false);

        Image imagem = go.AddComponent<Image>();
        imagem.sprite = Carregar(UiArtGenerator.CaminhoTecla);
        imagem.type = Image.Type.Sliced;
        imagem.pixelsPerUnitMultiplier = 2f;
        imagem.raycastTarget = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(200f, 76f);

        TMP_Text texto = CriarTexto(go.transform, "Rotulo", "ESPACO", new Vector2(0.5f, 0.5f), new Vector2(0f, 8f),
            TextAlignmentOptions.Center, 20f, new Color(0.17f, 0.16f, 0.24f));
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.offsetMin = Vector2.zero;
        texto.rectTransform.offsetMax = Vector2.zero;

        AvisoDeBotao aviso = go.AddComponent<AvisoDeBotao>();
        SerializedObject so = new SerializedObject(aviso);
        so.FindProperty("rotulo").objectReferenceValue = texto;
        so.FindProperty("fundo").objectReferenceValue = rect;
        so.ApplyModifiedProperties();

        return go;
    }

    /// <summary>Botao do controle: pastilha redonda escura com a letra (A, B, Y, START).</summary>
    private static void CriarBotaoDoControle(Transform pai, string rotulo, Vector2 posicao, float largura = 76f)
    {
        GameObject go = new GameObject("Botao" + rotulo, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        Image fundo = go.AddComponent<Image>();
        fundo.sprite = Carregar(UiArtGenerator.CaminhoBotao);
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = 2f;
        fundo.color = new Color(0.30f, 0.34f, 0.46f);  // cinza-azulado, diferente dos botoes verdes
        fundo.raycastTarget = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(largura, 64f);

        TMP_Text texto = CriarTexto(go.transform, "Rotulo", rotulo, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f),
            TextAlignmentOptions.Center, rotulo.Length > 1 ? 18f : 26f);
        texto.rectTransform.sizeDelta = rect.sizeDelta;
    }

    private static void CriarRotulo(Transform pai, string conteudo, Vector2 posicao, float tamanho = 26f)
    {
        TMP_Text texto = CriarTexto(pai, "Rotulo", conteudo, new Vector2(0.5f, 0.5f), posicao, TextAlignmentOptions.Left, tamanho);
        texto.rectTransform.pivot = new Vector2(0f, 0.5f);
        texto.rectTransform.sizeDelta = new Vector2(760f, 60f);
    }

    private static Image CriarImagem(Transform pai, string nome, Sprite sprite, Vector2 ancora, Vector2 posicao, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        Image imagem = go.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = ancora;
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        return imagem;
    }

    private static TMP_Text CriarTexto(Transform pai, string nome, string conteudo, Vector2 ancora, Vector2 posicao,
        TextAlignmentOptions alinhamento, float tamanhoFonte, Color? cor = null)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanhoFonte;
        texto.alignment = alinhamento;
        texto.color = cor ?? CorTexto;
        texto.raycastTarget = false;
        if (fonte != null)
            texto.font = fonte;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = ancora;
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(700f, 90f);
        return texto;
    }

    private static Button CriarBotao(Transform pai, string nome, string rotulo, Vector2 posicao)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        // Sprite de botao com bisel; as cores do ColorBlock so tingem (branco = cor original).
        Image fundo = go.AddComponent<Image>();
        fundo.sprite = Carregar(UiArtGenerator.CaminhoBotao);
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = 2f;

        Button botao = go.AddComponent<Button>();
        botao.targetGraphic = fundo;
        ColorBlock cores = botao.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 0.95f, 0.7f);
        cores.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        cores.selectedColor = new Color(1f, 0.95f, 0.7f);
        cores.fadeDuration = 0.05f;
        botao.colors = cores;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(420f, 80f);

        // Texto com sombra, deslocado 3 px para cima para ficar no "topo" do bisel.
        TMP_Text sombra = CriarTexto(go.transform, "Sombra", rotulo, new Vector2(0.5f, 0.5f), new Vector2(2f, 1f), TextAlignmentOptions.Center, 30f, new Color(0f, 0f, 0f, 0.5f));
        sombra.rectTransform.sizeDelta = rect.sizeDelta;
        TMP_Text texto = CriarTexto(go.transform, "Texto", rotulo, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), TextAlignmentOptions.Center, 30f);
        texto.rectTransform.sizeDelta = rect.sizeDelta;

        return botao;
    }
}
