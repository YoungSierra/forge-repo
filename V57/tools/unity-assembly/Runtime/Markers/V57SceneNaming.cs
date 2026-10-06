using System;
using System.Text;

namespace V57.GoldPath
{
    /// <summary>Maps scene ids from <c>scenes.json</c>/<c>package.json</c> to <c>Assets/_Game/Scenes/SCN_&lt;Id&gt;.unity</c>.</summary>
    public static class V57SceneNaming
    {
        public const string ScenesFolder = "Assets/_Game/Scenes";
        public const string Prefix = "SCN_";

        public static string ToSceneName(string id)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in (id ?? string.Empty).Trim())
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }

            string clean = builder.ToString();
            return clean.StartsWith(Prefix, StringComparison.Ordinal) ? clean : Prefix + clean;
        }

        public static string ToScenePath(string idOrPath)
        {
            string source = (idOrPath ?? string.Empty).Trim();
            if (source.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return source.Replace('\\', '/');
            }

            if (source.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return source.Replace('\\', '/') + ".unity";
            }

            return $"{ScenesFolder}/{ToSceneName(source)}.unity";
        }
    }
}
