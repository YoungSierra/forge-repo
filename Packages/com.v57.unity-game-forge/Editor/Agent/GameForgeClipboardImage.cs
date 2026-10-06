using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Read an image from the Windows clipboard (PNG / DIB), like Cursor paste.
    /// Falls back to a file path in <see cref="UnityEditor.EditorGUIUtility.systemCopyBuffer"/>.
    /// </summary>
    internal static class GameForgeClipboardImage
    {
        private const uint CfDib = 8;
        private const uint CfHdrop = 15;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint format);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint RegisterClipboardFormat(string lpszFormat);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, System.Text.StringBuilder lpszFile, int cch);

        /// <summary>
        /// Try to get image bytes + a suggested file name from the clipboard.
        /// </summary>
        public static bool TryGetImage(out byte[] bytes, out string fileName)
        {
            bytes = null;
            fileName = "clipboard.png";

            if (TryGetPngOrDib(out bytes, out fileName))
                return bytes != null && bytes.Length > 0;

            if (TryGetHdropImagePath(out var path) && File.Exists(path))
            {
                try
                {
                    bytes = File.ReadAllBytes(path);
                    fileName = Path.GetFileName(path);
                    return bytes is { Length: > 0 };
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static bool TryGetPngOrDib(out byte[] bytes, out string fileName)
        {
            bytes = null;
            fileName = "clipboard.png";
            if (!OpenClipboard(IntPtr.Zero))
                return false;
            try
            {
                var pngFmt = RegisterClipboardFormat("PNG");
                if (pngFmt != 0 && IsClipboardFormatAvailable(pngFmt))
                {
                    bytes = CopyGlobal(GetClipboardData(pngFmt));
                    if (bytes is { Length: > 0 })
                    {
                        fileName = "clipboard.png";
                        return true;
                    }
                }

                if (IsClipboardFormatAvailable(CfDib))
                {
                    var dib = CopyGlobal(GetClipboardData(CfDib));
                    if (dib is { Length: > 0 })
                    {
                        bytes = DibToPngBytes(dib);
                        fileName = "clipboard.png";
                        return bytes is { Length: > 0 };
                    }
                }
            }
            finally
            {
                CloseClipboard();
            }
            return false;
        }

        private static bool TryGetHdropImagePath(out string path)
        {
            path = null;
            if (!IsClipboardFormatAvailable(CfHdrop))
                return false;
            if (!OpenClipboard(IntPtr.Zero))
                return false;
            try
            {
                var hDrop = GetClipboardData(CfHdrop);
                if (hDrop == IntPtr.Zero) return false;
                var count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                if (count == 0) return false;
                var sb = new System.Text.StringBuilder(1024);
                DragQueryFile(hDrop, 0, sb, sb.Capacity);
                path = sb.ToString();
                if (string.IsNullOrEmpty(path)) return false;
                if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
                string e;
                try
                {
                    e = Path.GetExtension(path).ToLowerInvariant();
                }
                catch (ArgumentException)
                {
                    return false;
                }
                return e is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp";
            }
            finally
            {
                CloseClipboard();
            }
        }

        private static byte[] CopyGlobal(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return null;
            var ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero) return null;
            try
            {
                var size = (int)(uint)GlobalSize(handle);
                if (size <= 0) return null;
                var data = new byte[size];
                Marshal.Copy(ptr, data, 0, size);
                return data;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }

        /// <summary>Convert CF_DIB (BITMAPINFOHEADER + pixels) to PNG via Texture2D.</summary>
        private static byte[] DibToPngBytes(byte[] dib)
        {
            if (dib == null || dib.Length < 40) return null;
            try
            {
                int headerSize = BitConverter.ToInt32(dib, 0);
                int width = BitConverter.ToInt32(dib, 4);
                int height = BitConverter.ToInt32(dib, 8);
                short bitCount = BitConverter.ToInt16(dib, 14);
                int compression = BitConverter.ToInt32(dib, 16);
                if (width <= 0 || height == 0 || compression != 0) return null;
                var absH = Math.Abs(height);
                var topDown = height < 0;
                if (bitCount != 24 && bitCount != 32) return null;
                if (width > 8192 || absH > 8192) return null;

                var stride = ((width * bitCount + 31) / 32) * 4;
                var pixelsOffset = headerSize;
                if (dib.Length < pixelsOffset + stride * absH) return null;

                var tex = new Texture2D(width, absH, TextureFormat.RGBA32, false);
                try
                {
                    var colors = new Color32[width * absH];
                    for (var y = 0; y < absH; y++)
                    {
                        var srcY = topDown ? y : absH - 1 - y;
                        var row = pixelsOffset + srcY * stride;
                        for (var x = 0; x < width; x++)
                        {
                            var i = row + x * (bitCount / 8);
                            if (i + (bitCount / 8) > dib.Length) return null;
                            byte b = dib[i];
                            byte g = dib[i + 1];
                            byte r = dib[i + 2];
                            byte a = bitCount == 32 ? dib[i + 3] : (byte)255;
                            colors[y * width + x] = new Color32(r, g, b, a);
                        }
                    }
                    tex.SetPixels32(colors);
                    tex.Apply(false, false);
                    return ImageConversion.EncodeToPNG(tex);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(tex);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
