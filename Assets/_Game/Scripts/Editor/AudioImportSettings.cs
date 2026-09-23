using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura a importacao das musicas: streaming e compressao, para um MP3 de
/// 4 MB nao virar dezenas de MB de PCM na memoria ao carregar a fase.
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    private const string PastaMusica = "Assets/_Game/Audio/Music";

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(PastaMusica))
            return;

        AudioImporter importador = (AudioImporter)assetImporter;

        // So na primeira importacao, para respeitar ajustes manuais depois.
        if (!importador.importSettingsMissing)
            return;

        AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
        ajustes.loadType = AudioClipLoadType.Streaming;          // toca direto do disco
        ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
        ajustes.quality = 0.7f;
        // No Unity 6 o preload deixou de ser do importador e passou a ser uma
        // configuracao por plataforma, aqui dentro de SampleSettings.
        ajustes.preloadAudioData = false;
        importador.defaultSampleSettings = ajustes;

        importador.forceToMono = false;
    }
}
