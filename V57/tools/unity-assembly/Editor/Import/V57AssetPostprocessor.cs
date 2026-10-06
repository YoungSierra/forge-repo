using System;
using UnityEditor;
using UnityEngine;

namespace V57.Assembly.Import
{
    /// <summary>
    /// V57 import rules, applied only under <c>Assets/_Game/Art/</c> and <c>Assets/_Game/Audio/</c>.
    /// Rules are authoritative (re-applied on every import) so results are deterministic.
    /// Bump <see cref="RulesVersion"/> whenever a rule changes to force a reimport.
    /// </summary>
    public sealed class V57AssetPostprocessor : AssetPostprocessor
    {
        #region Fields

        private const uint RulesVersion = 1;

        #endregion

        #region Public Methods

        public override uint GetVersion()
        {
            return RulesVersion;
        }

        public override int GetPostprocessOrder()
        {
            return 100;
        }

        #endregion

        #region Private Methods

        private void OnPreprocessModel()
        {
            if (AssemblyPaths.IsArtPath(assetPath) && assetImporter is ModelImporter importer)
            {
                ModelImportRules.Preprocess(importer, assetPath);
            }
        }

        private void OnPreprocessAnimation()
        {
            if (AssemblyPaths.IsArtPath(assetPath) && assetImporter is ModelImporter importer)
            {
                ModelImportRules.PreprocessAnimation(importer, assetPath);
            }
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (AssemblyPaths.IsArtPath(assetPath))
            {
                CollisionPostprocess.Apply(root, assetPath);
            }
        }

        private void OnPreprocessTexture()
        {
            if (AssemblyPaths.IsArtPath(assetPath) && assetImporter is TextureImporter importer)
            {
                TextureImportRules.Apply(importer, assetPath);
            }
        }

        private void OnPreprocessAudio()
        {
            if (AssemblyPaths.IsAudioPath(assetPath) && assetImporter is AudioImporter importer)
            {
                AudioImportRules.Apply(importer, assetPath);
            }
        }

        private void OnPreprocessAsset()
        {
            bool isFont = assetPath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                || assetPath.EndsWith(".otf", StringComparison.OrdinalIgnoreCase);
            if (isFont && AssemblyPaths.IsArtPath(assetPath))
            {
                ImportLog.Record(assetPath, "font", "no rule (importer defaults kept)");
            }
        }

        #endregion
    }
}
