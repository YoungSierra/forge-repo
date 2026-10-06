using System;
using System.Globalization;

namespace V57.GoldPath
{
    /// <summary>
    /// A probe condition: <c>key</c>, <c>!key</c>, or <c>key op value</c> with op in == != &gt; &gt;= &lt; &lt;=.
    /// Value may be a number, true/false or a string state name (strings support == and != only).
    /// </summary>
    public sealed class GoldPathCondition
    {
        #region Private Fields

        private static readonly string[] Operators = { ">=", "<=", "==", "!=", ">", "<" };

        #endregion

        #region Public Methods

        public GoldPathCondition(string key, string op, string value)
        {
            string trimmedKey = (key ?? string.Empty).Trim();
            Negate = trimmedKey.StartsWith("!", StringComparison.Ordinal) && string.IsNullOrEmpty(op);
            Key = Negate ? trimmedKey.Substring(1).Trim() : trimmedKey;
            Op = (op ?? string.Empty).Trim();
            Value = (value ?? string.Empty).Trim().Trim('"', '\'');
        }

        public string Key { get; }

        public string Op { get; }

        public string Value { get; }

        public bool Negate { get; }

        public bool IsValid => Key.Length > 0 && (Op.Length == 0 || Array.IndexOf(Operators, Op) >= 0);

        public static GoldPathCondition Parse(string text)
        {
            string source = (text ?? string.Empty).Trim();
            foreach (string candidate in Operators)
            {
                int index = source.IndexOf(candidate, StringComparison.Ordinal);
                if (index > 0)
                {
                    return new GoldPathCondition(source.Substring(0, index), candidate, source.Substring(index + candidate.Length));
                }
            }

            return new GoldPathCondition(source, string.Empty, string.Empty);
        }

        public bool Evaluate(out string detail)
        {
            if (Op.Length == 0)
            {
                return EvaluateTruthy(out detail);
            }

            if (float.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float expected))
            {
                if (GoldPathProbeRegistry.TryGetFloat(Key, out float actual)
                    || (GoldPathProbeRegistry.TryGetBool(Key, out bool flag) && SetFloat(flag ? 1f : 0f, out actual)))
                {
                    detail = $"{Key}={actual.ToString(CultureInfo.InvariantCulture)} {Op} {Value}";
                    return Compare(actual.CompareTo(expected));
                }
            }

            if (bool.TryParse(Value, out bool expectedBool) && GoldPathProbeRegistry.TryGetBool(Key, out bool actualBool))
            {
                detail = $"{Key}={actualBool} {Op} {Value}";
                return Op == "==" ? actualBool == expectedBool : Op == "!=" && actualBool != expectedBool;
            }

            if (GoldPathProbeRegistry.TryGetString(Key, out string actualText))
            {
                bool equal = string.Equals(actualText, Value, StringComparison.OrdinalIgnoreCase);
                detail = $"{Key}='{actualText}' {Op} '{Value}'";
                return Op == "==" ? equal : Op == "!=" && !equal;
            }

            detail = $"unknown: no probe answered '{Key}'";
            return false;
        }

        public override string ToString()
        {
            return Op.Length == 0 ? (Negate ? "!" : string.Empty) + Key : $"{Key} {Op} {Value}";
        }

        #endregion

        #region Private Methods

        private bool EvaluateTruthy(out string detail)
        {
            bool truth;
            if (GoldPathProbeRegistry.TryGetBool(Key, out bool flag))
            {
                truth = flag;
            }
            else if (GoldPathProbeRegistry.TryGetFloat(Key, out float number))
            {
                truth = Math.Abs(number) > float.Epsilon;
            }
            else
            {
                detail = $"unknown: no probe answered '{Key}'";
                return false;
            }

            detail = $"{Key}={truth}";
            return Negate ? !truth : truth;
        }

        private static bool SetFloat(float source, out float target)
        {
            target = source;
            return true;
        }

        private bool Compare(int sign)
        {
            switch (Op)
            {
                case "==": return sign == 0;
                case "!=": return sign != 0;
                case ">": return sign > 0;
                case ">=": return sign >= 0;
                case "<": return sign < 0;
                case "<=": return sign <= 0;
                default: return false;
            }
        }

        #endregion
    }
}
