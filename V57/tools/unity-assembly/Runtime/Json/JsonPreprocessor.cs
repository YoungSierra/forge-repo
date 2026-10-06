using System;
using System.Text;

namespace V57.GoldPath
{
    /// <summary>
    /// Makes generated JSON JsonUtility-friendly without a full JSON library:
    /// 1) drops object members whose value is <c>null</c> (JsonUtility has no null semantics for
    ///    primitives/nested classes), 2) wraps bare scalars (number/true/false) of selected keys in
    ///    quotes so mixed-type fields (gold path <c>"value": 3 | true | "Playing"</c>) fit a string field.
    /// Nulls inside arrays are left untouched. Keys containing escaped quotes are not supported.
    /// </summary>
    public static class JsonPreprocessor
    {
        #region Public Methods

        public static string Process(string json, params string[] quoteScalarKeys)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }

            StringBuilder output = new StringBuilder(json.Length);
            bool inString = false;
            bool escaped = false;
            bool skipNextComma = false;
            int stringStart = 0;
            string lastString = string.Empty;
            int i = 0;
            while (i < json.Length)
            {
                char c = json[i];
                if (inString)
                {
                    output.Append(c);
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                        lastString = json.Substring(stringStart, i - stringStart);
                    }

                    i++;
                    continue;
                }

                if (skipNextComma)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        i++;
                        continue;
                    }

                    skipNextComma = false;
                    if (c == ',')
                    {
                        i++;
                        continue;
                    }
                }

                if (c == '"')
                {
                    inString = true;
                    stringStart = i + 1;
                    output.Append(c);
                    i++;
                    continue;
                }

                if (!char.IsWhiteSpace(c) && LastNonWhitespace(output) == ':')
                {
                    if (IsLiteral(json, i, "null"))
                    {
                        skipNextComma = RemoveCurrentMember(output);
                        i += 4;
                        continue;
                    }

                    if (c != '{' && c != '[' && Array.IndexOf(quoteScalarKeys, lastString) >= 0)
                    {
                        int end = i;
                        while (end < json.Length && ",}] \t\r\n".IndexOf(json[end]) < 0)
                        {
                            end++;
                        }

                        output.Append('"').Append(json, i, end - i).Append('"');
                        i = end;
                        continue;
                    }
                }

                output.Append(c);
                i++;
            }

            return output.ToString();
        }

        #endregion

        #region Private Methods

        private static bool IsLiteral(string json, int index, string literal)
        {
            if (index + literal.Length > json.Length || string.CompareOrdinal(json, index, literal, 0, literal.Length) != 0)
            {
                return false;
            }

            int after = index + literal.Length;
            return after >= json.Length || ",}] \t\r\n".IndexOf(json[after]) >= 0;
        }

        private static char LastNonWhitespace(StringBuilder builder)
        {
            for (int i = builder.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(builder[i]))
                {
                    return builder[i];
                }
            }

            return '\0';
        }

        private static void TrimEnd(StringBuilder builder)
        {
            while (builder.Length > 0 && char.IsWhiteSpace(builder[builder.Length - 1]))
            {
                builder.Length--;
            }
        }

        /// <summary>Removes <c>"key":</c> already written. Returns true when the following comma must be skipped.</summary>
        private static bool RemoveCurrentMember(StringBuilder output)
        {
            TrimEnd(output);
            output.Length--; // ':'
            TrimEnd(output);
            int keyStart = output.Length - 2;
            while (keyStart >= 0 && output[keyStart] != '"')
            {
                keyStart--;
            }

            output.Length = Math.Max(0, keyStart);
            TrimEnd(output);
            if (output.Length > 0 && output[output.Length - 1] == ',')
            {
                output.Length--;
                return false;
            }

            return true;
        }

        #endregion
    }
}
