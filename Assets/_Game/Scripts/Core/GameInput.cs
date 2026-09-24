using UnityEngine;

/// <summary>
/// Entrada do jogo num lugar so: teclado e controle juntos.
///
/// Os botoes virtuais ("Jump", "Atacar", "Interagir", "Pausar") estao no Project
/// Settings > Input Manager, cada um com uma tecla e um botao do controle. Ler
/// por aqui evita espalhar KeyCode pelos scripts e esquecer o gamepad num deles.
///
/// Mapeamento no controle (padrao Xbox):
///   A = pular · B = atacar · Y = interagir/loja · Start = pausar
///   analogico esquerdo e direcional = andar e navegar menus
/// </summary>
public static class GameInput
{
    private static bool usandoControle;
    private static int frameVerificado = -1;

    /// <summary>
    /// True quando o ultimo comando veio do controle. Os avisos na tela usam
    /// isto para mostrar o botao certo ("A" em vez de "ESPACO").
    /// </summary>
    public static bool UsandoControle
    {
        get
        {
            DetectarDispositivo();
            return usandoControle;
        }
    }

    // Nomes de botao para a interface (tutorial, avisos, loja).
    public static string BotaoAndar => UsandoControle ? "ANALOGICO" : "SETAS  ou  A / D";
    public static string BotaoPular => UsandoControle ? "A" : "ESPACO";
    public static string BotaoAtacar => UsandoControle ? "B" : "L";
    public static string BotaoInteragir => UsandoControle ? "Y" : "E";
    public static string BotaoPausar => UsandoControle ? "START" : "ESC";

    /// <summary>
    /// Descobre se o jogador esta no teclado ou no controle, olhando de onde veio
    /// a ultima tecla. Roda uma vez por frame, por mais que varios scripts leiam.
    /// </summary>
    private static void DetectarDispositivo()
    {
        if (frameVerificado == Time.frameCount)
            return;

        frameVerificado = Time.frameCount;

        // Analogico e direcional primeiro: mexer neles NAO dispara anyKeyDown, e
        // e justamente assim que se navega um menu. Sem isto, quem pega o
        // controle e sobe/desce nos botoes continuava vendo as teclas do teclado.
        bool eixoMexido = Mathf.Abs(Eixo("Horizontal")) > 0.5f
                       || Mathf.Abs(Eixo("Vertical")) > 0.5f;

        if (eixoMexido && !TeclaDeDirecaoPressionada())
        {
            usandoControle = true;
            return;
        }

        if (!Input.anyKeyDown)
            return;

        // anyKeyDown cobre teclado, mouse e joystick; o laco separa qual foi.
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton0 + i))
            {
                usandoControle = true;
                return;
            }
        }

        usandoControle = false;
    }

    /// <summary>
    /// Os eixos "Horizontal"/"Vertical" somam teclado e controle. Para saber de
    /// onde veio o movimento, checa se alguma tecla de direcao esta pressionada.
    /// </summary>
    private static bool TeclaDeDirecaoPressionada()
    {
        return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
            || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)
            || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
    }

    /// <summary>-1 a 1. Analogico esquerdo, direcional do controle, setas ou A/D.</summary>
    public static float Horizontal
    {
        get
        {
            float valor = Eixo("Horizontal");

            // O analogico devolve valores intermediarios; o jogo e digital.
            if (Mathf.Abs(valor) < 0.35f)
                return 0f;

            return Mathf.Sign(valor);
        }
    }

    /// <summary>Cru, para quem so quer saber se o jogador mexeu para cima ou para baixo.</summary>
    public static float Vertical => Eixo("Vertical");

    public static bool PulouAgora => Apertou("Jump") || Apertou("Pular2");
    public static bool SoltouPulo => Soltou("Jump") && Soltou("Pular2");

    public static bool AtacouAgora => Apertou("Atacar") || Input.GetMouseButtonDown(0);
    // So o botao "Interagir" (E no teclado, Y no controle). NAO vale o eixo
    // "Submit": ele inclui Espaco e Enter por padrao no Unity, e ai pular na
    // frente do vendedor abria a loja. O Submit continua valendo para navegar
    // os menus, que e trabalho do EventSystem, nao deste atalho.
    /// <summary>
    /// Abre e fecha a loja. Le o botao virtual E TAMBEM as teclas direto.
    ///
    /// Depender so do eixo "Interagir" e arriscado: se o Input Manager nao
    /// estiver como o codigo espera, GetButtonDown devolve false calado (o
    /// try/catch de Apertou engole a excecao) e a loja simplesmente para de
    /// abrir. Antes isso passava despercebido porque havia um "|| Submit" de
    /// reserva - que era justamente o que fazia o Espaco abrir a loja.
    /// Espaco continua de fora: aqui so entram E e o Y do controle.
    /// </summary>
    public static bool InteragiuAgora =>
        Apertou("Interagir")
        || Input.GetKeyDown(KeyCode.E)
        || Input.GetKeyDown(KeyCode.JoystickButton3);   // Y no padrao Xbox
    public static bool PausouAgora => Apertou("Pausar");

    /// <summary>
    /// Botao de "seguir em frente": pular cutscene, fechar aviso.
    ///
    /// Antes isto terminava em Input.anyKeyDown, e ai QUALQUER tecla pulava -
    /// inclusive a que o jogador aperta so para o jogo perceber que ele esta no
    /// controle. O aviso "ESPACO" trocava para "A", mas ninguem chegava a ver,
    /// porque o mesmo toque ja tinha pulado o video.
    /// </summary>
    public static bool ConfirmouAgora =>
        PulouAgora || InteragiuAgora || AtacouAgora || PausouAgora || Apertou("Submit");

    // GetButtonDown lanca excecao se o eixo nao existir no Input Manager; como os
    // eixos sao criados junto com o projeto, um try/catch aqui evita que um
    // projeto mal configurado quebre o jogo inteiro.
    private static bool Apertou(string nome)
    {
        try { return Input.GetButtonDown(nome); }
        catch (System.ArgumentException) { return false; }
    }

    /// <summary>
    /// Como o Apertou, mas para eixos: um eixo faltando no Input Manager nao
    /// pode derrubar o Update inteiro de quem chamou.
    /// </summary>
    private static float Eixo(string nome)
    {
        try { return Input.GetAxisRaw(nome); }
        catch (System.ArgumentException) { return 0f; }
    }

    private static bool Soltou(string nome)
    {
        try { return Input.GetButtonUp(nome); }
        catch (System.ArgumentException) { return true; }
    }
}
