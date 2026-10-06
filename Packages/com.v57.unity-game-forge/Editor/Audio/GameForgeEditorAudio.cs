using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Agent-done notification. Preview audio when Unity is focused; OS playback when in the background.
    /// </summary>
    internal static class GameForgeEditorAudio
    {
        public const string AgentDoneClipPath =
            "Packages/com.v57.unity-game-forge/Editor/Audio/Spark.mp3";

        private static AudioClip _agentDoneClip;
        private static bool _clipLoadAttempted;
        private static MethodInfo _playPreviewClip;
        private static MethodInfo _stopAllPreviewClips;
        private static bool _audioUtilReady;

        public static void PlayAgentDone()
        {
            var clip = LoadAgentDoneClip();
            var diskPath = ResolveClipDiskPath(clip);
            var focused = EditorApplication.isFocused;

            // Focused: editor preview worked reliably before background-audio work.
            if (focused && clip != null && TryPlayPreview(clip))
                return;

            // Background (or preview failed): route through the OS so audio is not muted.
            if (!string.IsNullOrEmpty(diskPath) && TryPlayOsBackground(diskPath))
                return;

            // Last resort when unfocused: preview may still work on some setups.
            if (clip != null && TryPlayPreview(clip))
                return;

            try { EditorApplication.Beep(); }
            catch { /* ignore */ }
        }

        private static string ResolveClipDiskPath(AudioClip clip)
        {
            try
            {
                var assetPath = clip != null ? AssetDatabase.GetAssetPath(clip) : AgentDoneClipPath;
                if (string.IsNullOrEmpty(assetPath))
                    assetPath = AgentDoneClipPath;

                var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? "";
                var full = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
                return File.Exists(full) ? full : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryPlayOsBackground(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return false;

            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    return TryPlayWindowsBackground(fullPath);
                if (Application.platform == RuntimePlatform.OSXEditor)
                    return TryPlayAfplay(fullPath);
                if (Application.platform == RuntimePlatform.LinuxEditor)
                    return TryPlayLinuxPlayer(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] OS notification audio failed: " + ex.Message);
            }

            return false;
        }

        private static bool TryPlayWindowsBackground(string fullPath)
        {
            if (TryPlayWindowsMediaPlayer(fullPath))
                return true;

            return TryPlayWindowsMci(fullPath);
        }

        /// <summary>Hidden PowerShell + WPF MediaPlayer — works when Unity is unfocused.</summary>
        private static bool TryPlayWindowsMediaPlayer(string fullPath)
        {
            try
            {
                var uri = new Uri(fullPath).AbsoluteUri.Replace("'", "''");
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments =
                        "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"" +
                        "Add-Type -AssemblyName presentationCore; " +
                        "$p = New-Object System.Windows.Media.MediaPlayer; " +
                        $"$p.Open([Uri]::new('{uri}')); " +
                        "$p.Play(); " +
                        "Start-Sleep -Seconds 2\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                return Process.Start(psi) != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] MediaPlayer notification failed: " + ex.Message);
                return false;
            }
        }

#if UNITY_EDITOR_WIN
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
#endif

        private static bool TryPlayWindowsMci(string fullPath)
        {
#if UNITY_EDITOR_WIN
            try
            {
                var alias = "gf_spark";
                var escaped = fullPath.Replace("\"", "'");
                mciSendString($"stop {alias}", null, 0, IntPtr.Zero);
                mciSendString($"close {alias}", null, 0, IntPtr.Zero);

                if (mciSendString($"open \"{escaped}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero) != 0)
                    return false;

                return mciSendString($"play {alias}", null, 0, IntPtr.Zero) == 0;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private static bool TryPlayAfplay(string fullPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/afplay",
                    Arguments = QuoteForShell(fullPath),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                return Process.Start(psi) != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryPlayLinuxPlayer(string fullPath)
        {
            foreach (var player in new[] { "paplay", "aplay", "ffplay" })
            {
                try
                {
                    var args = player == "ffplay"
                        ? $"-nodisp -autoexit -loglevel quiet {QuoteForShell(fullPath)}"
                        : QuoteForShell(fullPath);
                    var psi = new ProcessStartInfo
                    {
                        FileName = player,
                        Arguments = args,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    };
                    if (Process.Start(psi) != null)
                        return true;
                }
                catch
                {
                    /* try next */
                }
            }

            return false;
        }

        private static string QuoteForShell(string path) =>
            "\"" + path.Replace("\"", "\\\"") + "\"";

        private static AudioClip LoadAgentDoneClip()
        {
            if (_clipLoadAttempted)
                return _agentDoneClip;

            _clipLoadAttempted = true;
            _agentDoneClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AgentDoneClipPath);
            return _agentDoneClip;
        }

        private static bool TryPlayPreview(AudioClip clip)
        {
            if (!EnsureAudioUtil())
                return false;

            try
            {
                _stopAllPreviewClips?.Invoke(null, null);

                if (_playPreviewClip.GetParameters().Length == 3)
                    _playPreviewClip.Invoke(null, new object[] { clip, 0, false });
                else if (_playPreviewClip.GetParameters().Length == 2)
                    _playPreviewClip.Invoke(null, new object[] { clip, 0 });
                else
                    _playPreviewClip.Invoke(null, new object[] { clip });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] Could not play notification clip: " + ex.Message);
                return false;
            }
        }

        private static bool EnsureAudioUtil()
        {
            if (_audioUtilReady)
                return _playPreviewClip != null;

            _audioUtilReady = true;
            try
            {
                var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
                if (audioUtil == null)
                    return false;

                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                _playPreviewClip =
                    audioUtil.GetMethod("PlayPreviewClip", flags, null,
                        new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                    ?? audioUtil.GetMethod("PlayPreviewClip", flags, null,
                        new[] { typeof(AudioClip), typeof(int) }, null)
                    ?? audioUtil.GetMethod("PlayClip", flags, null,
                        new[] { typeof(AudioClip) }, null)
                    ?? audioUtil.GetMethod("PlayClip", flags, null,
                        new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);

                _stopAllPreviewClips =
                    audioUtil.GetMethod("StopAllPreviewClips", flags)
                    ?? audioUtil.GetMethod("StopAllClips", flags);

                return _playPreviewClip != null;
            }
            catch
            {
                return false;
            }
        }

        public static void InvalidateCache()
        {
            _agentDoneClip = null;
            _clipLoadAttempted = false;
        }
    }
}
