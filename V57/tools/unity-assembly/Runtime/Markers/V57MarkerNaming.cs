using System;

namespace V57.GoldPath
{
    /// <summary>
    /// Parses <c>Marker_&lt;Type&gt;_&lt;Id&gt;</c> (e.g. <c>Marker_Spawn_Player</c>, <c>Marker_Patrol_Guard_02</c>,
    /// <c>Marker_Bounds</c>). Unknown types map to <see cref="V57MarkerKind.Other"/> with the full remainder as id.
    /// </summary>
    public static class V57MarkerNaming
    {
        public const string Prefix = "Marker_";

        public static bool IsMarker(string nodeName)
        {
            return !string.IsNullOrEmpty(nodeName) && nodeName.StartsWith(Prefix, StringComparison.Ordinal);
        }

        public static bool TryParse(string nodeName, out V57MarkerKind kind, out string id)
        {
            kind = V57MarkerKind.Other;
            id = string.Empty;
            if (!IsMarker(nodeName))
            {
                return false;
            }

            string rest = nodeName.Substring(Prefix.Length);
            int separator = rest.IndexOf('_');
            string typeToken = separator < 0 ? rest : rest.Substring(0, separator);
            bool isName = typeToken.Length > 0 && char.IsLetter(typeToken[0]);
            if (isName && Enum.TryParse(typeToken, true, out V57MarkerKind parsed) && parsed != V57MarkerKind.Other)
            {
                kind = parsed;
                id = separator < 0 ? typeToken : rest.Substring(separator + 1);
                return true;
            }

            id = rest;
            return true;
        }
    }
}
