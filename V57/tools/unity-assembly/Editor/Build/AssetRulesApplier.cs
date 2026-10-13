using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using V57.Assembly.Data;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Per-asset rules (<c>Docs/V57/asset_rules.json</c> → manifest) on Visual prefabs: shadow casting on every renderer,
    /// and, for a kept prefab of a ruled asset, the generated colliders rebuilt from the current model so a re-delivered
    /// mesh never keeps stale collision. Colliders imported with the model (UCX_) are never touched.
    /// </summary>
    public static class AssetRulesApplier
    {
        #region Public Methods

        /// <summary>Applies <c>shadows</c> to every renderer; returns true when something changed.</summary>
        public static bool ApplyShadows(AssetEntryDto entry, GameObject root)
        {
            if (!TryShadowMode(entry != null ? entry.shadows : null, out ShadowCastingMode mode))
            {
                return false;
            }

            bool changed = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.shadowCastingMode != mode)
                {
                    renderer.shadowCastingMode = mode;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Kept prefab of a ruled asset: removes the colliders V57 generated (on the root, or added onto the model
        /// instance) and builds them again from the current meshes. Returns true when the collision changed.
        /// </summary>
        public static bool RefreshColliders(AssetEntryDto entry, GameObject root)
        {
            string before = Signature(root);
            foreach (Collider collider in Generated(root))
            {
                Object.DestroyImmediate(collider);
            }

            PrefabColliderBuilder.Apply(entry, root, RendererBounds.Compute(root));
            return Signature(root) != before;
        }

        #endregion

        #region Private Methods

        private static bool TryShadowMode(string value, out ShadowCastingMode mode)
        {
            switch ((value ?? string.Empty).ToLowerInvariant())
            {
                case "on": mode = ShadowCastingMode.On; return true;
                case "off": mode = ShadowCastingMode.Off; return true;
                case "two_sided": mode = ShadowCastingMode.TwoSided; return true;
                default: mode = ShadowCastingMode.On; return false;
            }
        }

        /// <summary>Colliders added by V57: on the prefab root, or added components on the nested model instance.</summary>
        private static List<Component> Generated(GameObject root)
        {
            List<Component> generated = new List<Component>();
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                bool collider = component is Collider || component is Collider2D;
                if (collider && (component.gameObject == root || PrefabUtility.IsAddedComponentOverride(component)))
                {
                    generated.Add(component);
                }
            }

            return generated;
        }

        private static string Signature(GameObject root)
        {
            StringBuilder builder = new StringBuilder();
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                switch (component)
                {
                    case BoxCollider box:
                        builder.Append("box:").Append(component.name).Append(Vec(box.center)).Append(Vec(box.size)).Append(';');
                        break;
                    case CapsuleCollider capsule:
                        builder.Append("capsule:").Append(component.name).Append(Vec(capsule.center)).Append(F(capsule.radius)).Append(F(capsule.height)).Append(';');
                        break;
                    case MeshCollider mesh:
                        builder.Append("mesh:").Append(component.name).Append(mesh.sharedMesh != null ? mesh.sharedMesh.name : "-").Append(mesh.convex).Append(';');
                        break;
                    case Collider other:
                        builder.Append(other.GetType().Name).Append(':').Append(component.name).Append(';');
                        break;
                    case Collider2D other2D:
                        builder.Append(other2D.GetType().Name).Append(':').Append(component.name).Append(';');
                        break;
                }
            }

            return builder.ToString();
        }

        private static string Vec(Vector3 v)
        {
            return $"({F(v.x)},{F(v.y)},{F(v.z)})";
        }

        private static string F(float value)
        {
            return value.ToString("F3", CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
