using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace V57.GoldPath.Tests
{
    /// <summary>
    /// M1+ gate: loads the gold path scene (gold_path.scene, else first slice scene), waits for a game probe,
    /// runs <see cref="GoldPathDriver"/> and asserts <c>checks.json → pass</c>. Unity's LogAssert also fails the test
    /// on any logged error. Run: <c>unity test . --mode PlayMode --filter V57.GoldPath</c>.
    /// </summary>
    public sealed class GoldPathAcceptanceTests
    {
        #region Fields

        private const float ProbeWaitSeconds = 10f;

        #endregion

        #region Tests

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator GoldPath_Passes()
        {
            List<string> errors = new List<string>();
            GoldPathDocument document = GoldPathSource.Load(
                GoldPathPaths.ResolveInput(null, GoldPathPaths.AcceptanceRelative),
                GoldPathPaths.ResolveInput(null, GoldPathPaths.FallbackRelative),
                errors);
            string scenePath = document.ScenePath.Length > 0 ? V57SceneNaming.ToScenePath(document.ScenePath) : GoldPathTestScenes.FirstSliceScenePath();
            Assert.IsFalse(string.IsNullOrEmpty(scenePath), "No gold path scene: set gold_path.scene or a slice scene in package.json/scenes.json.");

#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(scenePath), LoadSceneMode.Single);
#endif
            float waited = 0f;
            while (GoldPathProbeRegistry.Count == 0 && waited < ProbeWaitSeconds)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;
            }

            Assert.Greater(GoldPathProbeRegistry.Count, 0, $"No IGoldPathProbe registered in {scenePath} within {ProbeWaitSeconds}s.");
            GameObject host = new GameObject("V57GoldPathDriver");
            GoldPathDriver driver = host.AddComponent<GoldPathDriver>();
            yield return driver.StartCoroutine(driver.CoRun());
            bool pass = driver.LastPass;
            string checksPath = driver.LastChecksPath;
            Object.Destroy(host);

            Assert.IsTrue(pass, $"Gold path failed — see {checksPath}");
        }

        #endregion
    }
}
