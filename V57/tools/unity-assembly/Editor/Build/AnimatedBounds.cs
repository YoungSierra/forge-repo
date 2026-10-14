using System.Collections.Generic;
using UnityEngine;
using V57.Assembly.Data;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Culling for animated models, so off-screen and far rigs stop animating without a visible freeze:
    /// <list type="bullet">
    /// <item>Each SkinnedMeshRenderer's bounds cover every pose of every clip of its controller (sampled, +5 %). With
    /// rest-pose bounds a swinging tail leaves the box, Unity judges the renderer off screen while it is visible, and a
    /// <c>CullUpdateTransforms</c> Animator freezes in view.</item>
    /// <item>With correct bounds the Animator uses <c>CullUpdateTransforms</c>: no animation while nothing is drawn.</item>
    /// <item><c>cull_below</c> (asset rule): the model is not drawn below that screen-height fraction (a one-level LODGroup,
    /// or the last level of an existing one), so far away it neither renders nor animates.</item>
    /// </list>
    /// </summary>
    public static class AnimatedBounds
    {
        #region Fields

        private const int SamplesPerClip = 16;
        private const float Margin = 0.05f;

        #endregion

        #region Public Methods

        /// <summary>Fits the skinned bounds to all clips of the instance's Animator. Returns true when any bounds changed.</summary>
        public static bool Fit(GameObject modelInstance)
        {
            Animator animator = modelInstance.GetComponentInChildren<Animator>(true);
            SkinnedMeshRenderer[] skinned = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (animator == null || animator.runtimeAnimatorController == null || skinned.Length == 0)
            {
                return false;
            }

            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            Transform[] all = modelInstance.GetComponentsInChildren<Transform>(true);
            Vector3[] positions = new Vector3[all.Length];
            Quaternion[] rotations = new Quaternion[all.Length];
            Vector3[] scales = new Vector3[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                positions[i] = all[i].localPosition;
                rotations[i] = all[i].localRotation;
                scales[i] = all[i].localScale;
            }

            Dictionary<SkinnedMeshRenderer, Bounds> fitted = new Dictionary<SkinnedMeshRenderer, Bounds>();
            Mesh baked = new Mesh();
            try
            {
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip == null)
                    {
                        continue;
                    }

                    for (int s = 0; s <= SamplesPerClip; s++)
                    {
                        clip.SampleAnimation(animator.gameObject, clip.length * s / SamplesPerClip);
                        foreach (SkinnedMeshRenderer renderer in skinned)
                        {
                            if (renderer.sharedMesh == null)
                            {
                                continue;
                            }

                            renderer.BakeMesh(baked, true);
                            Transform space = renderer.rootBone != null ? renderer.rootBone : renderer.transform;
                            Transform own = renderer.transform;
                            foreach (Vector3 vertex in baked.vertices)
                            {
                                Vector3 local = space.InverseTransformPoint(own.position + own.rotation * vertex);
                                if (fitted.TryGetValue(renderer, out Bounds bounds))
                                {
                                    bounds.Encapsulate(local);
                                    fitted[renderer] = bounds;
                                }
                                else
                                {
                                    fitted[renderer] = new Bounds(local, Vector3.zero);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(baked);
                for (int i = 0; i < all.Length; i++)
                {
                    all[i].localPosition = positions[i];
                    all[i].localRotation = rotations[i];
                    all[i].localScale = scales[i];
                }
            }

            bool changed = false;
            foreach (KeyValuePair<SkinnedMeshRenderer, Bounds> pair in fitted)
            {
                Bounds bounds = pair.Value;
                bounds.Expand(bounds.size * Margin);
                Bounds current = pair.Key.localBounds;
                if ((current.center - bounds.center).sqrMagnitude > 1e-4f || (current.size - bounds.size).sqrMagnitude > 1e-4f)
                {
                    pair.Key.localBounds = bounds;
                    pair.Key.updateWhenOffscreen = false;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>Applies the asset's <c>cull_below</c> rule. Returns true when the LODGroup changed.</summary>
        public static bool ApplyCullRule(AssetEntryDto entry, GameObject root)
        {
            if (entry == null || entry.cull_below <= 0f || entry.cull_below >= 1f)
            {
                return false;
            }

            LODGroup group = root.GetComponent<LODGroup>();
            if (group == null)
            {
                group = root.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(entry.cull_below, root.GetComponentsInChildren<Renderer>(true)) });
                group.RecalculateBounds();
                return true;
            }

            LOD[] lods = group.GetLODs();
            int last = lods.Length - 1;
            float floor = last > 0 ? lods[last - 1].screenRelativeTransitionHeight * 0.9f : 1f;
            float height = Mathf.Min(entry.cull_below, floor);
            if (last < 0 || Mathf.Approximately(lods[last].screenRelativeTransitionHeight, height))
            {
                return false;
            }

            lods[last].screenRelativeTransitionHeight = height;
            group.SetLODs(lods);
            return true;
        }

        #endregion
    }
}
