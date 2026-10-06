using UnityEditor;
using UnityEngine;

namespace V57.Assembly.Import
{
    /// <summary>
    /// Audio rules by folder: Music/Ambience → Streaming (Vorbis), SFX → DecompressOnLoad,
    /// Voice → CompressedInMemory (Vorbis). Other folders keep importer defaults.
    /// </summary>
    public static class AudioImportRules
    {
        public static void Apply(AudioImporter importer, string assetPath)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            string category;
            if (assetPath.Contains("/Audio/Music/") || assetPath.Contains("/Audio/Ambience/"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.loadInBackground = true;
                category = "Music/Ambience: Streaming Vorbis q0.7";
            }
            else if (assetPath.Contains("/Audio/SFX/"))
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                category = "SFX: DecompressOnLoad";
            }
            else if (assetPath.Contains("/Audio/Voice/"))
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                category = "Voice: CompressedInMemory Vorbis";
            }
            else
            {
                ImportLog.Record(assetPath, "audio", "no folder rule (importer defaults kept)");
                return;
            }

            importer.defaultSampleSettings = settings;
            ImportLog.Record(assetPath, "audio", category);
        }
    }
}
