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

    /// <summary>-1 a 1. Analogico esquerdo, direcional do controle, setas ou A/D.</summary>
    public static float Horizontal
    {
        get
        {
            float valor = Input.GetAxisRaw("Horizontal");

            // O analogico devolve valores intermediarios; o jogo e digital.
            if (Mathf.Abs(valor) < 0.35f)
                return 0f;

            return Mathf.Sign(valor);
        }
    }

    public static bool PulouAgora => Apertou("Jump") || Apertou("Pular2");
    public static bool SoltouPulo => Soltou("Jump") && Soltou("Pular2");

    public static bool AtacouAgora => Apertou("Atacar") || Input.GetMouseButtonDown(0);
    public static bool InteragiuAgora => Apertou("Interagir") || Apertou("Submit");
    public static bool PausouAgora => Apertou("Pausar");

    /// <summary>Qualquer botao de "seguir em frente": pular cutscene, fechar aviso.</summary>
    public static bool ConfirmouAgora =>
        PulouAgora || InteragiuAgora || AtacouAgora || PausouAgora || Input.anyKeyDown;

    // GetButtonDown lanca excecao se o eixo nao existir no Input Manager; como os
    // eixos sao criados junto com o projeto, um try/catch aqui evita que um
    // projeto mal configurado quebre o jogo inteiro.
    private static bool Apertou(string nome)
    {
        try { return Input.GetButtonDown(nome); }
        catch (System.ArgumentException) { return false; }
    }

    private static bool Soltou(string nome)
    {
        try { return Input.GetButtonUp(nome); }
        catch (System.ArgumentException) { return true; }
    }
}
