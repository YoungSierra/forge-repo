using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;

namespace V57.Assembly.Import
{
    /// <summary>
    /// Model rules: meters 1:1, no cameras/lights, embedded materials kept in the model (not extracted) and
    /// remapped to V57 <c>MAT_&lt;Asset&gt;</c> by the material builder, rig per prefix/manifest, clip loop/events.
    /// </summary>
    public static class ModelImportRules
    {
        #region Fields

        public const string AnimationEventReceiver = "OnV57AnimationEvent";

        private static readonly string[] LoopingHints = { "idle", "walk", "run", "loop", "cycle" };

        #endregion

        #region Public Methods

        public static void Preprocess(ModelImporter importer, string assetPath)
        {
            string stem = Path.GetFileNameWithoutExtension(assetPath);
            AssetEntryDto entry = ManifestLookup.FindForModel(assetPath);
            bool exact = entry != null && string.Equals(entry.collision, "exact", StringComparison.OrdinalIgnoreCase);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.isReadable = exact;
            string rig = ApplyRig(importer, stem);
            string manifest = entry != null ? entry.asset_id : "none";
            ImportLog.Record(assetPath, "model",
                $"globalScale=1 useFileScale=true cameras=off lights=off materials=ViaDescription/InPrefab readable={exact} rig={rig} manifest={manifest}");
        }

        public static void PreprocessAnimation(ModelImporter importer, string assetPath)
        {
            string stem = Path.GetFileNameWithoutExtension(assetPath);
            if (!stem.StartsWith("ANIM_", StringComparison.Ordinal) && !stem.StartsWith("SK_", StringComparison.Ordinal))
            {
                return;
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            AnimationEntryDto manifest = ManifestLookup.FindAnimation(assetPath);
            int eventCount = 0;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = manifest != null ? manifest.loop : LooksLooping(clip.name);
                if (manifest?.events != null && manifest.events.Length > 0)
                {
                    clip.events = BuildEvents(manifest.events, clip);
                    eventCount += manifest.events.Length;
                }
            }

            importer.clipAnimations = clips;
            string source = manifest != null ? "manifest" : "name heuristic";
            ImportLog.Record(assetPath, "animation", $"clips={clips.Length} loop from {source} events={eventCount} receiver={AnimationEventReceiver}(string)");
        }

        #endregion

        #region Private Methods

        private static string ApplyRig(ModelImporter importer, string stem)
        {
            if (stem.StartsWith("SK_", StringComparison.Ordinal))
            {
                bool humanoid = ManifestLookup.IsHumanoid(stem.Substring(3));
                importer.animationType = humanoid ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                return humanoid ? "Humanoid" : "Generic";
            }

            if (AssetNaming.TrySplitAnimation(stem, out string assetName, out string clip))
            {
                bool humanoid = ManifestLookup.IsHumanoid(assetName);
                importer.animationType = humanoid ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                return (humanoid ? "Humanoid" : "Generic") + $" (as SK_{assetName}, clip {clip}; avatar linked by ApplyImportRules)";
            }

            if (stem.StartsWith("SM_", StringComparison.Ordinal) || stem.StartsWith("BLK_", StringComparison.Ordinal))
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                return "None (static)";
            }

            return "unchanged";
        }

        private static bool LooksLooping(string clipName)
        {
            string lower = (clipName ?? string.Empty).ToLowerInvariant();
            foreach (string hint in LoopingHints)
            {
                if (lower.Contains(hint))
                {
                    return true;
                }
            }

            return false;
        }

        private static AnimationEvent[] BuildEvents(AnimationEventDto[] events, ModelImporterClipAnimation clip)
        {
            float length = Mathf.Max(1f, clip.lastFrame - clip.firstFrame);
            AnimationEvent[] result = new AnimationEvent[events.Length];
            for (int i = 0; i < events.Length; i++)
            {
                result[i] = new AnimationEvent
                {
                    functionName = AnimationEventReceiver,
                    stringParameter = events[i]?.name ?? string.Empty,
                    time = Mathf.Clamp01((events[i] != null ? events[i].frame - clip.firstFrame : 0f) / length)
                };
            }

            return result;
        }

        #endregion
    }
}
