using System.Collections;
using System.IO;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// Writes PNG evidence. Normal (graphics) runs: end-of-frame Game View capture via
    /// <see cref="ScreenCapture.CaptureScreenshotAsTexture()"/> (includes overlay UI).
    /// Batch mode: WaitForEndOfFrame is unreliable there, so Camera.main is rendered into a RenderTexture
    /// (overlay UI is NOT included; detail says so). With -nographics no capture is possible.
    /// </summary>
    public sealed class GoldPathScreenshot
    {
        #region Public Methods

        public string LastError { get; private set; }

        public string LastMethod { get; private set; }

        public IEnumerator CoCapture(string fullPath)
        {
            LastError = null;
            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                LastMethod = "screen";
                Write(ScreenCapture.CaptureScreenshotAsTexture(), fullPath);
                yield break;
            }

            LastMethod = "camera (batchmode, no overlay UI)";
            Write(RenderMainCamera(), fullPath);
        }

        #endregion

        #region Private Methods

        private Texture2D RenderMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                LastError = "no Camera.main to render";
                return null;
            }

            int width = Mathf.Max(64, Screen.width);
            int height = Mathf.Max(64, Screen.height);
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render(); // URP: supported for a single camera; unverified in every URP 17 renderer setup.
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }

            return texture;
        }

        private void Write(Texture2D texture, string fullPath)
        {
            if (texture == null)
            {
                LastError = LastError ?? "capture returned no texture";
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            }
            catch (IOException exception)
            {
                LastError = exception.Message;
            }
            finally
            {
                Object.Destroy(texture);
            }
        }

        #endregion
    }
}
