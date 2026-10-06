using UnityEditor;

namespace V57.Assembly.Diff
{
    /// <summary>
    /// Completes a pending hierarchy diff after entering Play Mode. The pending flag lives in SessionState because
    /// entering Play Mode (with Domain Reload) wipes statics.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyDiffPlayModeHook
    {
        private static double _deadline;

        static HierarchyDiffPlayModeHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(HierarchyDiffRunner.PendingKey, false))
            {
                _deadline = EditorApplication.timeSinceStartup + HierarchyDiffRunner.PlaySettleSeconds;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (EditorApplication.timeSinceStartup < _deadline)
            {
                return;
            }

            EditorApplication.update -= Tick;
            HierarchyDiffRunner.CompleteInPlayMode(true);
        }
    }
}
