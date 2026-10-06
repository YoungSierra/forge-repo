using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using V57.Assembly.Report;

namespace V57.Assembly.Diff
{
    /// <summary>
    /// Edit-vs-Play hierarchy diff. <see cref="Begin"/> in Edit Mode snapshots the active scene, then enters Play Mode;
    /// <see cref="HierarchyDiffPlayModeHook"/> captures the Play snapshot after <see cref="PlaySettleSeconds"/>, writes
    /// <c>hierarchy-diff.json</c> and exits Play Mode. Called in Play Mode, it compares immediately and stays in Play Mode.
    /// </summary>
    public static class HierarchyDiffRunner
    {
        #region Fields

        public const string PendingKey = "V57.HierarchyDiff.Pending";
        public const float PlaySettleSeconds = 3f;
        private const string Step = "CaptureHierarchyDiff";

        #endregion

        #region Public Methods

        public static string EditSnapshotPath => Path.Combine(AssemblyPaths.ReportsDirectory, "hierarchy-edit.json");

        public static string PlaySnapshotPath => Path.Combine(AssemblyPaths.ReportsDirectory, "hierarchy-play.json");

        public static string DiffPath => Path.Combine(AssemblyPaths.ReportsDirectory, "hierarchy-diff.json");

        public static void Begin()
        {
            if (EditorApplication.isPlaying)
            {
                CompleteInPlayMode(false);
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                AssemblyContext.Error(Step, "active scene is not saved; open a SCN_ scene first");
                return;
            }

            Write(EditSnapshotPath, JsonUtility.ToJson(HierarchyCapture.Capture(scene, false), true));
            SessionState.SetBool(PendingKey, true);
            Debug.Log($"V57.Assembly: edit snapshot of {scene.path} written; entering Play Mode for {PlaySettleSeconds}s → {DiffPath}");
            EditorApplication.EnterPlaymode();
        }

        public static HierarchyDiffReport CompleteInPlayMode(bool exitPlayMode)
        {
            SessionState.EraseBool(PendingKey);
            HierarchySnapshot edit = Read(EditSnapshotPath);
            if (edit == null)
            {
                AssemblyContext.Error(Step, $"no edit snapshot at {EditSnapshotPath}; call CaptureHierarchyDiff() in Edit Mode first");
                return null;
            }

            HierarchySnapshot play = HierarchyCapture.Capture(SceneManager.GetActiveScene(), true);
            if (play.scene != edit.scene)
            {
                AssemblyContext.Warn(Step, $"play scene {play.scene} differs from edit scene {edit.scene}");
            }

            Write(PlaySnapshotPath, JsonUtility.ToJson(play, true));
            HierarchyDiffReport report = HierarchyDiffer.Compare(edit, play, HierarchyDiffer.DefaultSpawnRoots);
            Write(DiffPath, JsonUtility.ToJson(report, true));
            Debug.Log($"V57.Assembly: hierarchy diff pass={report.pass} violations={report.created_violations.Length} → {DiffPath}");
            if (exitPlayMode)
            {
                EditorApplication.ExitPlaymode();
            }

            return report;
        }

        /// <summary>Compares two previously written snapshot files and writes hierarchy-diff.json.</summary>
        public static HierarchyDiffReport CompareFiles(string editSnapshotPath, string playSnapshotPath)
        {
            HierarchySnapshot edit = Read(editSnapshotPath);
            HierarchySnapshot play = Read(playSnapshotPath);
            if (edit == null || play == null)
            {
                return null;
            }

            HierarchyDiffReport report = HierarchyDiffer.Compare(edit, play, HierarchyDiffer.DefaultSpawnRoots);
            Write(DiffPath, JsonUtility.ToJson(report, true));
            return report;
        }

        #endregion

        #region Private Methods

        private static HierarchySnapshot Read(string path)
        {
            return File.Exists(path) ? JsonUtility.FromJson<HierarchySnapshot>(File.ReadAllText(path)) : null;
        }

        private static void Write(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        #endregion
    }
}
