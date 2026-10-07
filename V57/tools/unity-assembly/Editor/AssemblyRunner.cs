using System;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Build;
using V57.Assembly.Data;
using V57.Assembly.Diff;
using V57.Assembly.Import;
using V57.Assembly.Report;

namespace V57.Assembly
{
    /// <summary>
    /// Stage I2 entry points. Warm Editor: <c>unity command eval "V57.Assembly.AssemblyRunner.RunAll()"</c>.
    /// Batch: <c>-executeMethod V57.Assembly.AssemblyRunner.RunAllBatch</c> (exit 0 = report pass, 1 = fail).
    /// Every single-step method reloads Docs/Generated/json, runs the step and rewrites the report.
    /// </summary>
    public static class AssemblyRunner
    {
        #region Public Methods

        /// <summary>ApplyImportRules → BuildMaterials → BuildSkybox → BuildUiAtlases → BuildAnimators → BuildVisualPrefabs →
        /// BuildLevelScenes → WriteReport. Missing art is never replaced by placeholders: slots stay empty and are reported.</summary>
        public static void RunAll()
        {
            AssemblyContext.Reset();
            GeneratedData.Reload();
            RecordDataProblems();
            RunStep("ApplyImportRules", ImportRuleApplier.Apply);
            RunStep("BuildMaterials", BuildAllMaterials);
            RunStep("BuildSkybox", SkyboxBuilder.BuildAll);
            RunStep("BuildUiAtlases", AtlasBuilder.BuildAll);
            RunStep("BuildAnimators", AnimatorControllerBuilder.BuildAll);
            RunStep("BuildVisualPrefabs", VisualPrefabBuilder.BuildAll);
            RunStep("BuildLevelScenes", LevelSceneBuilder.BuildAll);
            WriteReport();
        }

        /// <summary>RunAll with <see cref="AssemblyOptions.Force"/>: regenerates existing real prefabs and scenes.</summary>
        public static void RunAllForce()
        {
            AssemblyOptions.Force = true;
            try
            {
                RunAll();
            }
            finally
            {
                AssemblyOptions.Force = false;
            }
        }

        /// <summary>For <c>-batchmode -executeMethod</c>: exits the Editor with 0 when the report passes, else 1.</summary>
        public static void RunAllBatch()
        {
            int exitCode = 1;
            try
            {
                RunAll();
                exitCode = AssemblyContext.LastReportPass ? 0 : 1;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            EditorApplication.Exit(exitCode);
        }

        public static void ApplyImportRules() => RunSingle("ApplyImportRules", ImportRuleApplier.Apply);

        public static void BuildMaterials() => RunSingle("BuildMaterials", BuildAllMaterials);

        public static void BuildSkybox() => RunSingle("BuildSkybox", SkyboxBuilder.BuildAll);

        public static void BuildUiAtlases() => RunSingle("BuildUiAtlases", AtlasBuilder.BuildAll);

        public static void BuildAnimators() => RunSingle("BuildAnimators", AnimatorControllerBuilder.BuildAll);

        public static void BuildVisualPrefabs() => RunSingle("BuildVisualPrefabs", VisualPrefabBuilder.BuildAll);

        public static void BuildLevelScenes() => RunSingle("BuildLevelScenes", LevelSceneBuilder.BuildAll);

        /// <summary>
        /// M2 gate. Edit Mode: snapshots the active scene, enters Play Mode, and after a few seconds writes
        /// <c>Docs/V57/reports/hierarchy-diff.json</c> and exits Play Mode (asynchronous — poll the file).
        /// Play Mode: compares against the last edit snapshot immediately.
        /// </summary>
        public static void CaptureHierarchyDiff()
        {
            HierarchyDiffRunner.Begin();
        }

        /// <summary>Writes <c>Docs/V57/reports/assembly-report.json</c> (includes a missing-reference scan).</summary>
        public static string WriteReport()
        {
            return ReportWriter.Write();
        }

        #endregion

        #region Private Methods

        /// <summary>Texture-driven materials first, then the level-layout manifest values (flat colours, metallic, culling).</summary>
        private static void BuildAllMaterials()
        {
            MaterialBuilder.BuildAll();
            LayoutMaterialBuilder.BuildAll();
        }

        private static void RunSingle(string name, Action step)
        {
            AssemblyContext.ClearStep(name);
            AssemblyContext.ClearStep("GeneratedData");
            GeneratedData.Reload();
            RecordDataProblems();
            RunStep(name, step);
            WriteReport();
        }

        private static void RunStep(string name, Action step)
        {
            double start = EditorApplication.timeSinceStartup;
            int errorsBefore = AssemblyContext.Errors.Count;
            string detail = string.Empty;
            try
            {
                step();
            }
            catch (Exception exception)
            {
                detail = exception.GetType().Name + ": " + exception.Message;
                AssemblyContext.Error(name, exception.ToString());
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            AssemblyContext.AddStep(new AssemblyStepResult
            {
                name = name,
                ok = AssemblyContext.Errors.Count == errorsBefore,
                duration_s = (float)(EditorApplication.timeSinceStartup - start),
                detail = detail
            });
        }

        private static void RecordDataProblems()
        {
            if (!string.IsNullOrEmpty(GeneratedData.LastLoadProblems))
            {
                AssemblyContext.Warn("GeneratedData", GeneratedData.LastLoadProblems);
            }
        }

        #endregion
    }
}
