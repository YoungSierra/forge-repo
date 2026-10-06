using System;
using UnityEngine;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>World-space bounds of visible renderers (UCX_/Marker_/Socket_ and disabled renderers excluded).</summary>
    public static class RendererBounds
    {
        public static Bounds Compute(GameObject root)
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                string nodeName = renderer.gameObject.name;
                bool helper = nodeName.StartsWith("UCX_", StringComparison.Ordinal)
                    || nodeName.StartsWith("Socket_", StringComparison.Ordinal)
                    || V57MarkerNaming.IsMarker(nodeName);
                if (!renderer.enabled || helper || renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            return bounds;
        }
    }
}
