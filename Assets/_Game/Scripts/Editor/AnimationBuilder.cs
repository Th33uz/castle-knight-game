using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Cria AnimationClips e AnimatorControllers por codigo a partir dos quadros
/// dos packs. Fazer isso na mao para ~40 animacoes levaria horas de arrastar.
/// </summary>
public static class AnimationBuilder
{
    public const string Pasta = "Assets/_Game/Animations";

    private static void GarantirPasta()
    {
        if (!AssetDatabase.IsValidFolder(Pasta))
            AssetDatabase.CreateFolder("Assets/_Game", "Animations");
    }

    // ----------------- Carregar quadros -----------------

    /// <summary>
    /// Quadros soltos numa pasta, cujo nome e "prefixo + numero" (ex.: "opossum-1"),
    /// ordenados pelo numero. So olha os arquivos diretamente na pasta.
    /// </summary>
    public static Sprite[] QuadrosDaPasta(string pasta, string prefixo)
    {
        pasta = pasta.TrimEnd('/');
        Regex padrao = new Regex("^" + Regex.Escape(prefixo) + @"(\d+)$", RegexOptions.IgnoreCase);
        var lista = new List<(int numero, Sprite sprite)>();

        // Procura por textura (e nao por "t:Sprite"): toda imagem importada como
        // Sprite e uma Texture2D, e o LoadAssetAtPath<Sprite> pega o sprite dela.
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { pasta }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(caminho).Replace('\\', '/') != pasta)
                continue;

            Match m = padrao.Match(Path.GetFileNameWithoutExtension(caminho));
            if (!m.Success)
                continue;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (sprite != null)
                lista.Add((int.Parse(m.Groups[1].Value), sprite));
        }

        if (lista.Count == 0)
            Debug.LogWarning("[Anim] Nenhum quadro '" + prefixo + "N' em " + pasta);

        return lista.OrderBy(q => q.numero).Select(q => q.sprite).ToArray();
    }

    /// <summary>Quadros de uma folha fatiada ("nome_0", "nome_1"...), na ordem do indice.</summary>
    public static Sprite[] QuadrosFatiados(string caminhoPng, int inicio = 0, int quantidade = -1)
    {
        var lista = new List<(int indice, Sprite sprite)>();

        foreach (UnityEngine.Object ativo in AssetDatabase.LoadAllAssetsAtPath(caminhoPng))
        {
            Sprite sprite = ativo as Sprite;
            if (sprite == null)
                continue;

            int sub = sprite.name.LastIndexOf('_');
            if (sub >= 0 && int.TryParse(sprite.name.Substring(sub + 1), out int indice))
                lista.Add((indice, sprite));
        }

        IEnumerable<Sprite> ordenados = lista.OrderBy(q => q.indice).Select(q => q.sprite).Skip(inicio);
        if (quantidade > 0)
            ordenados = ordenados.Take(quantidade);

        Sprite[] resultado = ordenados.ToArray();
        if (resultado.Length == 0)
            Debug.LogWarning("[Anim] Folha sem quadros fatiados: " + caminhoPng);

        return resultado;
    }

    /// <summary>Um sprite especifico de uma folha fatiada, pelo indice.</summary>
    public static Sprite SpriteFatiado(string caminhoPng, int indice)
    {
        foreach (UnityEngine.Object ativo in AssetDatabase.LoadAllAssetsAtPath(caminhoPng))
        {
            if (ativo is Sprite sprite && sprite.name.EndsWith("_" + indice))
                return sprite;
        }

        return null;
    }

    // ----------------- Clipes -----------------

    public static AnimationClip CriarClip(string nome, Sprite[] quadros, float fps, bool loop)
    {
        GarantirPasta();

        if (quadros == null || quadros.Length == 0)
        {
            Debug.LogWarning("[Anim] Clip '" + nome + "' sem quadros; ignorado.");
            return null;
        }

        AnimationClip clip = new AnimationClip { frameRate = fps };

        EditorCurveBinding ligacao = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] chaves = new ObjectReferenceKeyframe[quadros.Length];
        for (int i = 0; i < quadros.Length; i++)
            chaves[i] = new ObjectReferenceKeyframe { time = i / fps, value = quadros[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, ligacao, chaves);

        // stopTime = n/fps faz o ultimo quadro durar o mesmo que os outros; sem
        // isso ele seria trocado instantaneamente ao reiniciar o loop.
        AnimationClipSettings cfg = AnimationUtility.GetAnimationClipSettings(clip);
        cfg.loopTime = loop;
        cfg.stopTime = quadros.Length / fps;
        AnimationUtility.SetAnimationClipSettings(clip, cfg);

        string caminho = Pasta + "/" + nome + ".anim";
        AssetDatabase.DeleteAsset(caminho);
        AssetDatabase.CreateAsset(clip, caminho);
        return clip;
    }

    // ----------------- Controllers -----------------

    private static AnimatorController NovoController(string nome)
    {
        GarantirPasta();
        string caminho = Pasta + "/" + nome + ".controller";
        AssetDatabase.DeleteAsset(caminho);
        return AnimatorController.CreateAnimatorControllerAtPath(caminho);
    }

    private static AnimatorStateTransition Transicao(AnimatorState de, AnimatorState para, bool comExitTime = false)
    {
        AnimatorStateTransition t = de.AddTransition(para);
        t.hasExitTime = comExitTime;
        t.exitTime = 1f;
        t.hasFixedDuration = true;
        t.duration = 0f;
        return t;
    }

    /// <summary>
    /// Um estado em loop (serra, fruta, inimigo andando, fogo). Com
    /// <paramref name="ataque"/>, ganha um estado extra disparado pelo trigger
    /// "Atacar" que volta ao loop quando termina (chefe atirando).
    /// </summary>
    public static AnimatorController CriarControllerLoop(string nome, AnimationClip clip, AnimationClip ataque = null)
    {
        AnimatorController c = NovoController(nome);
        AnimatorStateMachine sm = c.layers[0].stateMachine;

        AnimatorState estado = sm.AddState("Loop");
        estado.motion = clip;
        sm.defaultState = estado;

        // Parametros que EnemyPatrol/Boss escrevem; existirem evita avisos no console.
        c.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
        c.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);

        if (ataque != null)
        {
            AnimatorState sAtaque = sm.AddState("Ataque");
            sAtaque.motion = ataque;

            AnimatorStateTransition t = sm.AddAnyStateTransition(sAtaque);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Atacar");
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = false;
            Transicao(sAtaque, estado, comExitTime: true);
        }

        return c;
    }

    /// <summary>
    /// Parado / correndo, decidido pelo float "Velocidade" (gato companheiro).
    /// </summary>
    public static AnimatorController CriarControllerIdleCorrida(string nome, AnimationClip idle, AnimationClip corrida, float limiar = 0.5f)
    {
        AnimatorController c = NovoController(nome);
        c.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
        AnimatorStateMachine sm = c.layers[0].stateMachine;

        AnimatorState sIdle = sm.AddState("Idle");
        sIdle.motion = idle;
        sm.defaultState = sIdle;

        AnimatorState sRun = sm.AddState("Corrida");
        sRun.motion = corrida;

        Transicao(sIdle, sRun).AddCondition(AnimatorConditionMode.Greater, limiar, "Velocidade");
        Transicao(sRun, sIdle).AddCondition(AnimatorConditionMode.Less, limiar, "Velocidade");
        return c;
    }

    /// <summary>
    /// Idle ate receber o trigger "Ativar"; toca <paramref name="ativo"/> uma vez e
    /// depois fica em <paramref name="depois"/> (loop) ou volta ao idle.
    /// Checkpoint: bandeira guardada -> hasteando -> tremulando.
    /// Trampolim: parado -> pulo -> parado.
    /// </summary>
    public static AnimatorController CriarControllerAtivavel(string nome, AnimationClip idle, AnimationClip ativo, AnimationClip depois)
    {
        AnimatorController c = NovoController(nome);
        c.AddParameter("Ativar", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = c.layers[0].stateMachine;

        AnimatorState sIdle = sm.AddState("Idle");
        sIdle.motion = idle;
        sm.defaultState = sIdle;

        AnimatorState sAtivo = sm.AddState("Ativando");
        sAtivo.motion = ativo;

        Transicao(sIdle, sAtivo).AddCondition(AnimatorConditionMode.If, 0f, "Ativar");

        if (depois != null)
        {
            AnimatorState sDepois = sm.AddState("Ativado");
            sDepois.motion = depois;
            Transicao(sAtivo, sDepois, comExitTime: true);
        }
        else
        {
            Transicao(sAtivo, sIdle, comExitTime: true);
        }

        return c;
    }

    /// <summary>
    /// Maquina de estados da Bruxa: parada / andando pelo float "Velocidade",
    /// mais os triggers "Atacar" (carregar + lancar), "Dano" e "Morrer".
    /// </summary>
    public static AnimatorController CriarControllerBruxa(string nome,
        AnimationClip idle, AnimationClip correr, AnimationClip carregar,
        AnimationClip atacar, AnimationClip dano, AnimationClip morte)
    {
        AnimatorController c = NovoController(nome);
        c.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
        c.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Dano", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Morrer", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = c.layers[0].stateMachine;

        AnimatorState sIdle = sm.AddState("Idle");        sIdle.motion = idle;
        AnimatorState sCorrer = sm.AddState("Correr");    sCorrer.motion = correr;
        AnimatorState sCarregar = sm.AddState("Carregar"); sCarregar.motion = carregar;
        AnimatorState sAtacar = sm.AddState("Atacar");    sAtacar.motion = atacar;
        AnimatorState sDano = sm.AddState("Dano");        sDano.motion = dano;
        AnimatorState sMorte = sm.AddState("Morte");      sMorte.motion = morte;
        sm.defaultState = sIdle;

        Transicao(sIdle, sCorrer).AddCondition(AnimatorConditionMode.Greater, 0.3f, "Velocidade");
        Transicao(sCorrer, sIdle).AddCondition(AnimatorConditionMode.Less, 0.3f, "Velocidade");

        // Atacar sempre passa por "carregar" antes: da o aviso visual de que a
        // magia vem, para a luta ser justa.
        AnimatorStateTransition paraCarregar = sm.AddAnyStateTransition(sCarregar);
        paraCarregar.AddCondition(AnimatorConditionMode.If, 0f, "Atacar");
        paraCarregar.hasExitTime = false;
        paraCarregar.duration = 0f;
        paraCarregar.canTransitionToSelf = false;
        Transicao(sCarregar, sAtacar, comExitTime: true);
        Transicao(sAtacar, sIdle, comExitTime: true);

        AnimatorStateTransition paraDano = sm.AddAnyStateTransition(sDano);
        paraDano.AddCondition(AnimatorConditionMode.If, 0f, "Dano");
        paraDano.hasExitTime = false;
        paraDano.duration = 0f;
        paraDano.canTransitionToSelf = false;
        Transicao(sDano, sIdle, comExitTime: true);

        AnimatorStateTransition paraMorte = sm.AddAnyStateTransition(sMorte);
        paraMorte.AddCondition(AnimatorConditionMode.If, 0f, "Morrer");
        paraMorte.hasExitTime = false;
        paraMorte.duration = 0f;
        paraMorte.canTransitionToSelf = false;

        return c;
    }

    /// <summary>
    /// Maquina de estados do jogador. Le os parametros que o PlayerController2D
    /// e o PlayerHealth ja escrevem.
    /// </summary>
    public static AnimatorController CriarControllerJogador(string nome,
        AnimationClip idle, AnimationClip corrida, AnimationClip pulo, AnimationClip puloDuplo,
        AnimationClip queda, AnimationClip dano, AnimationClip morte,
        AnimationClip ataque, AnimationClip ataqueAereo)
    {
        AnimatorController c = NovoController(nome);
        c.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
        c.AddParameter("VelocidadeY", AnimatorControllerParameterType.Float);
        c.AddParameter("NoChao", AnimatorControllerParameterType.Bool);
        c.AddParameter("Pular", AnimatorControllerParameterType.Trigger);
        c.AddParameter("PuloDuplo", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Dano", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Morrer", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        c.AddParameter("AtaqueAereo", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = c.layers[0].stateMachine;

        AnimatorState sIdle = sm.AddState("Idle");   sIdle.motion = idle;
        AnimatorState sRun = sm.AddState("Corrida"); sRun.motion = corrida;
        AnimatorState sJump = sm.AddState("Pulo");   sJump.motion = pulo;
        AnimatorState sDbl = sm.AddState("PuloDuplo"); sDbl.motion = puloDuplo;
        AnimatorState sFall = sm.AddState("Queda");  sFall.motion = queda;
        AnimatorState sHurt = sm.AddState("Dano");   sHurt.motion = dano;
        AnimatorState sDie = sm.AddState("Morte");   sDie.motion = morte;
        AnimatorState sAtk = sm.AddState("Ataque");  sAtk.motion = ataque;
        AnimatorState sAtkAr = sm.AddState("AtaqueAereo"); sAtkAr.motion = ataqueAereo;
        sm.defaultState = sIdle;

        // Golpes: entram de qualquer estado e voltam quando o clipe acaba.
        AnimatorStateTransition qualquerParaAtaque = sm.AddAnyStateTransition(sAtk);
        qualquerParaAtaque.AddCondition(AnimatorConditionMode.If, 0f, "Atacar");
        qualquerParaAtaque.hasExitTime = false;
        qualquerParaAtaque.duration = 0f;
        qualquerParaAtaque.canTransitionToSelf = false;
        Transicao(sAtk, sIdle, comExitTime: true);

        AnimatorStateTransition qualquerParaAtaqueAereo = sm.AddAnyStateTransition(sAtkAr);
        qualquerParaAtaqueAereo.AddCondition(AnimatorConditionMode.If, 0f, "AtaqueAereo");
        qualquerParaAtaqueAereo.hasExitTime = false;
        qualquerParaAtaqueAereo.duration = 0f;
        qualquerParaAtaqueAereo.canTransitionToSelf = false;
        Transicao(sAtkAr, sFall, comExitTime: true);
        Transicao(sAtkAr, sIdle).AddCondition(AnimatorConditionMode.If, 0f, "NoChao");

        // Chao: parado <-> correndo
        Transicao(sIdle, sRun).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidade");
        Transicao(sRun, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidade");

        // Sair do chao: subindo -> pulo; descendo -> queda
        foreach (AnimatorState noChao in new[] { sIdle, sRun })
        {
            AnimatorStateTransition paraPulo = Transicao(noChao, sJump);
            paraPulo.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
            paraPulo.AddCondition(AnimatorConditionMode.Greater, 0.5f, "VelocidadeY");

            AnimatorStateTransition paraQueda = Transicao(noChao, sFall);
            paraQueda.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
            paraQueda.AddCondition(AnimatorConditionMode.Less, -0.5f, "VelocidadeY");
        }

        // No ar
        Transicao(sJump, sFall).AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocidadeY");
        Transicao(sJump, sIdle).AddCondition(AnimatorConditionMode.If, 0f, "NoChao");
        Transicao(sFall, sIdle).AddCondition(AnimatorConditionMode.If, 0f, "NoChao");

        // Pulo duplo entra de qualquer estado e cai na queda quando termina.
        AnimatorStateTransition qualquerParaDuplo = sm.AddAnyStateTransition(sDbl);
        qualquerParaDuplo.AddCondition(AnimatorConditionMode.If, 0f, "PuloDuplo");
        qualquerParaDuplo.hasExitTime = false;
        qualquerParaDuplo.duration = 0f;
        qualquerParaDuplo.canTransitionToSelf = false;
        Transicao(sDbl, sFall, comExitTime: true);
        Transicao(sDbl, sIdle).AddCondition(AnimatorConditionMode.If, 0f, "NoChao");

        // Dano e morte tambem entram de qualquer estado.
        AnimatorStateTransition qualquerParaDano = sm.AddAnyStateTransition(sHurt);
        qualquerParaDano.AddCondition(AnimatorConditionMode.If, 0f, "Dano");
        qualquerParaDano.hasExitTime = false;
        qualquerParaDano.duration = 0f;
        qualquerParaDano.canTransitionToSelf = false;
        Transicao(sHurt, sIdle, comExitTime: true);

        AnimatorStateTransition qualquerParaMorte = sm.AddAnyStateTransition(sDie);
        qualquerParaMorte.AddCondition(AnimatorConditionMode.If, 0f, "Morrer");
        qualquerParaMorte.hasExitTime = false;
        qualquerParaMorte.duration = 0f;
        qualquerParaMorte.canTransitionToSelf = false;

        return c;
    }
}
