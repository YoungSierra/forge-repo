using System;

namespace V57.GoldPath
{
    /// <summary>
    /// Normalizes control targets: <c>space</c> → <c>&lt;Keyboard&gt;/space</c>, <c>Gamepad/buttonSouth</c> →
    /// <c>&lt;Gamepad&gt;/buttonSouth</c>; paths already using &lt;Layout&gt; are returned unchanged.
    /// </summary>
    public static class GoldPathControlPath
    {
        private static readonly string[] KnownLayouts = { "Keyboard", "Gamepad", "Mouse", "Touchscreen" };

        public static string Normalize(string target)
        {
            string source = (target ?? string.Empty).Trim();
            if (source.Length == 0 || source.StartsWith("<", StringComparison.Ordinal))
            {
                return source;
            }

            int slash = source.IndexOf('/');
            if (slash < 0)
            {
                return "<Keyboard>/" + source;
            }

            string head = source.Substring(0, slash);
            foreach (string layout in KnownLayouts)
            {
                if (string.Equals(head, layout, StringComparison.OrdinalIgnoreCase))
                {
                    return $"<{layout}>{source.Substring(slash)}";
                }
            }

            return source;
        }

        public static bool IsTouch(string normalizedPath)
        {
            return normalizedPath.StartsWith("<Touchscreen>", StringComparison.OrdinalIgnoreCase);
        }
    }
}
