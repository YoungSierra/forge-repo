using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Checks built visual prefabs against the manifest: size_m [x,y,z] (±10% per axis) and pivot
    /// (feet/base: bottom at y=0; center; hinge: left/right end at x=0; axle: centered on y/z). Mismatches are warnings.
    /// </summary>
    public static class PrefabValidator
    {
        #region Fields

        private const string Step = "BuildVisualPrefabs";
        private const float SizeTolerance = 0.10f;

        #endregion

        #region Public Methods

        public static void Validate(AssetEntryDto entry, Bounds bounds)
        {
            Vector3 size = bounds.size;
            if (entry.size_m == null || entry.size_m.Length < 3)
            {
                AssemblyContext.Warn(Step, $"{entry.asset_name}: no size_m; size not validated (actual {Format(size)})");
            }
            else
            {
                List<string> mismatches = new List<string>();
                string[] axes = { "x", "y", "z" };
                for (int i = 0; i < 3; i++)
                {
                    float expected = entry.size_m[i];
                    if (expected > 0f && Mathf.Abs(size[i] - expected) / expected > SizeTolerance)
                    {
                        mismatches.Add($"{axes[i]}: {size[i]:0.###} vs {expected:0.###}");
                    }
                }

                if (mismatches.Count > 0)
                {
                    AssemblyContext.Warn(Step, $"{entry.asset_name}: size outside ±10% ({string.Join(", ", mismatches)}) — check export units/scale");
                }
            }

            string pivotProblem = CheckPivot((entry.pivot ?? string.Empty).ToLowerInvariant(), (entry.side ?? string.Empty).ToLowerInvariant(), bounds);
            if (pivotProblem != null)
            {
                AssemblyContext.Warn(Step, $"{entry.asset_name}: pivot '{entry.pivot}' {pivotProblem}");
            }
        }

        #endregion

        #region Private Methods

        private static string CheckPivot(string pivot, string side, Bounds bounds)
        {
            Vector3 size = bounds.size;
            float tolY = Mathf.Max(0.02f, size.y * 0.05f);
            float tolX = Mathf.Max(0.02f, size.x * 0.05f);
            float tolZ = Mathf.Max(0.02f, size.z * 0.05f);
            switch (pivot)
            {
                case "feet":
                case "base":
                    return Mathf.Abs(bounds.min.y) <= tolY ? null : $"expects bottom at y=0, bottom is {bounds.min.y:0.###}";
                case "center":
                    bool centered = Mathf.Abs(bounds.center.x) <= tolX && Mathf.Abs(bounds.center.y) <= tolY && Mathf.Abs(bounds.center.z) <= tolZ;
                    return centered ? null : $"expects centered bounds, center is {Format(bounds.center)}";
                case "hinge":
                    float edge = side == "right" ? bounds.max.x : bounds.min.x;
                    bool atEnd = side == "left" || side == "right" ? Mathf.Abs(edge) <= tolX : Mathf.Abs(bounds.min.x) <= tolX || Mathf.Abs(bounds.max.x) <= tolX;
                    return atEnd ? null : $"(side {side}) expects an x-end at 0, bounds x [{bounds.min.x:0.###}, {bounds.max.x:0.###}]";
                case "axle":
                    return Mathf.Abs(bounds.center.y) <= tolY && Mathf.Abs(bounds.center.z) <= tolZ ? null : $"expects y/z centered, center is {Format(bounds.center)}";
                default:
                    return pivot.Length == 0 ? "missing; not validated" : "unknown value; not validated";
            }
        }

        private static string Format(Vector3 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.###}, {1:0.###}, {2:0.###})", value.x, value.y, value.z);
        }

        #endregion
    }
}
