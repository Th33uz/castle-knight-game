using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Ajustes de fisica aplicados automaticamente ao iniciar o jogo e ao carregar
/// cada cena. Ficam aqui, e nao no Project Settings, para o comportamento ser
/// visivel no codigo e valer igual no Editor e no build.
/// </summary>
public static class PhysicsSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configurar()
    {
        int inimigo = LayerMask.NameToLayer("Enemy");

        // Inimigos atravessam uns aos outros. Sem isso, dois inimigos que se
        // encontram numa plataforma ficam se empurrando ate cair.
        if (inimigo >= 0)
            Physics2D.IgnoreLayerCollision(inimigo, inimigo, true);

        // Inimigos tambem atravessam trampolins (camada OneWayPlatform): so o
        // jogador interage com eles.
        int trampolim = LayerMask.NameToLayer("OneWayPlatform");
        if (inimigo >= 0 && trampolim >= 0)
            Physics2D.IgnoreLayerCollision(inimigo, trampolim, true);

        // O gato so colide com o cenario: atravessa o heroi (sem empurrar), os
        // inimigos (nao apanha nem atrapalha) e as armadilhas (nao morre).
        int gato = LayerMask.NameToLayer("Companion");
        if (gato >= 0)
        {
            int jogador = LayerMask.NameToLayer("Player");
            int perigo = LayerMask.NameToLayer("Hazard");

            if (jogador >= 0) Physics2D.IgnoreLayerCollision(gato, jogador, true);
            if (inimigo >= 0) Physics2D.IgnoreLayerCollision(gato, inimigo, true);
            if (perigo >= 0) Physics2D.IgnoreLayerCollision(gato, perigo, true);
            Physics2D.IgnoreLayerCollision(gato, gato, true);
        }

        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    /// <summary>
    /// Reconstroi os colisores dos tilemaps assim que a cena carrega.
    ///
    /// As cenas sao geradas em modo batch, onde a fisica nao da nenhum passo:
    /// o TilemapCollider2D nao processa os tiles e o CompositeCollider2D e salvo
    /// com a geometria vazia. Em jogo o Unity confia no que foi salvo, e o chao
    /// fica sem colisao, tudo cai. Forcar a geracao aqui garante o colisor
    /// independentemente de como a cena foi salva.
    /// </summary>
    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        foreach (TilemapCollider2D colisor in Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
            colisor.ProcessTilemapChanges();

        foreach (CompositeCollider2D composto in Object.FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
            composto.GenerateGeometry();
    }
}
