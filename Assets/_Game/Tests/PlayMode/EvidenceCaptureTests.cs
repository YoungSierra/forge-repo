using System.Collections;
using System.IO;
using NUnit.Framework;
using ProfessorSprat.Gameplay.Flow;
using ProfessorSprat.Gameplay.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>
    /// Review evidence (M3/M4 packs): end-of-frame Game View captures of UI screen states the gold path does not reach
    /// (main menu, pause). Explicit — run on demand with env V57_CAPTURE_DIR set to the pack's shots folder.
    /// </summary>
    [Explicit]
    public sealed class EvidenceCaptureTests
    {
        #region Public Methods

        [UnityTest]
        public IEnumerator CaptureMenuAndPause()
        {
            string folder = System.Environment.GetEnvironmentVariable("V57_CAPTURE_DIR");
            if (string.IsNullOrEmpty(folder))
            {
                folder = Path.Combine(Application.dataPath, "..", "Docs", "V57", "evidence", "capture");
            }

            Directory.CreateDirectory(folder);
            yield return SceneManager.LoadSceneAsync("SCN_MainMenu_Boot");
            yield return new WaitForSeconds(0.5f);
            yield return Capture(Path.Combine(folder, "SCN_MainMenu_Boot_menu_01.png"));
            yield return SceneManager.LoadSceneAsync("SCN_HydroStation_Gameplay");
            ZoneFlow flow = Object.FindAnyObjectByType<ZoneFlow>();
            float waited = 0f;
            while (flow.State != ZoneFlowState.Playing && waited < 5f)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;
            }

            yield return new WaitForSeconds(0.5f);
            PauseController pause = Object.FindAnyObjectByType<PauseController>();
            pause.SetPaused(true);
            yield return null;
            yield return null;
            yield return Capture(Path.Combine(folder, "SCN_HydroStation_Gameplay_pause_01.png"));
            pause.SetPaused(false);
            Assert.IsTrue(File.Exists(Path.Combine(folder, "SCN_HydroStation_Gameplay_pause_01.png")));
        }

        #endregion

        #region Private Methods

        private static IEnumerator Capture(string path)
        {
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
        }

        #endregion
    }
}
