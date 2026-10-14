using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;

namespace V57.Assembly.Import
{
    /// <summary>
    /// Runs in OnPostprocessAnimation: looping Generic clips must play in place, because V57 gameplay moves characters
    /// from code (no root motion). A loop whose root node ends away from where it started (e.g. a scuttle exported with
    /// travel) would snap back on every cycle. The shallowest such node loses its linear drift, v(t) − (v(T) − v(0))·t/T,
    /// so the loop closes exactly while its sway and bob are kept. Logged per clip; the provider should export loops in place.
    /// </summary>
    public static class InPlaceLoopClips
    {
        #region Fields

        private const float MinTravel = 0.05f;
        private static readonly string[] Axes = { "x", "y", "z" };

        #endregion

        #region Public Methods

        public static void Apply(AnimationClip clip, string assetPath, ModelImporter importer)
        {
            if (clip == null || importer == null || importer.animationType != ModelImporterAnimationType.Generic
                || !AssetNaming.TrySplitAnimation(Path.GetFileNameWithoutExtension(assetPath), out _, out _)
                || !AnimationUtility.GetAnimationClipSettings(clip).loopTime || clip.length <= 0f)
            {
                return;
            }

            string node = TravellingNode(clip, out Vector3 travel);
            if (node == null)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(travel[i]) < 1e-5f)
                {
                    continue;
                }

                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(node, typeof(Transform), "m_LocalPosition." + Axes[i]);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                Keyframe[] keys = curve.keys;
                float slope = travel[i] / clip.length;
                for (int k = 0; k < keys.Length; k++)
                {
                    keys[k].value -= slope * keys[k].time;
                    keys[k].inTangent -= slope;
                    keys[k].outTangent -= slope;
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            ImportLog.Record(assetPath, "animation-in-place", $"{clip.name}: loop travelled {travel.magnitude:F2} m on '{node}'; drift removed (plays in place). Provider fix: export loops in place.");
        }

        #endregion

        #region Private Methods

        /// <summary>Shallowest node whose local position ends ≥ 5 cm from where it started, or null.</summary>
        private static string TravellingNode(AnimationClip clip, out Vector3 travel)
        {
            travel = Vector3.zero;
            string best = null;
            int bestDepth = int.MaxValue;
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform) || binding.propertyName != "m_LocalPosition.x")
                {
                    continue;
                }

                Vector3 net = Net(clip, binding.path);
                int depth = binding.path.Split('/').Length;
                if (net.magnitude >= MinTravel && depth < bestDepth)
                {
                    best = binding.path;
                    bestDepth = depth;
                    travel = net;
                }
            }

            return best;
        }

        private static Vector3 Net(AnimationClip clip, string nodePath)
        {
            Vector3 net = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(nodePath, typeof(Transform), "m_LocalPosition." + Axes[i]));
                if (curve != null && curve.length > 1)
                {
                    net[i] = curve.Evaluate(clip.length) - curve.Evaluate(0f);
                }
            }

            return net;
        }

        #endregion
    }
}
