using System;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Colliders per manifest <c>collision</c>: exact → UCX_ convex MeshColliders from import (fallback simple);
    /// simple → Box (Capsule for characters) from renderer bounds, their 2D variants when package.physics is 2d; none → nothing.
    /// Blockouts without UCX_ get non-convex MeshColliders on their visible meshes instead of one giant box.
    /// </summary>
    public static class PrefabColliderBuilder
    {
        #region Fields

        private const string Step = "BuildVisualPrefabs";

        #endregion

        #region Public Methods

        public static string Apply(AssetEntryDto entry, GameObject root, Bounds bounds)
        {
            string mode = (entry.collision ?? string.Empty).ToLowerInvariant();
            int ucx = root.GetComponentsInChildren<MeshCollider>(true).Length;
            bool blockout = string.Equals(entry.type, "blockout", StringComparison.OrdinalIgnoreCase);
            if (mode != "exact" && mode != "simple")
            {
                if (ucx > 0)
                {
                    AssemblyContext.Warn(Step, $"{entry.asset_name}: collision '{mode}' but {ucx} UCX_ colliders present (kept)");
                }

                return ucx > 0 ? "ucx(kept)" : "none";
            }

            if (ucx > 0)
            {
                if (AssemblyPhysics.Is2D)
                {
                    AssemblyContext.Warn(Step, $"{entry.asset_name}: physics is 2d but UCX_ gives 3D MeshColliders (kept; PolygonCollider2D not generated) — author 2D colliders in the gameplay variant");
                }

                return $"exact({ucx} UCX)";
            }

            if (mode == "exact")
            {
                AssemblyContext.Warn(Step, $"{entry.asset_name}: collision exact but no UCX_ nodes; using fallback");
            }

            if (blockout && AssemblyPhysics.Is2D)
            {
                AssemblyContext.Warn(Step, $"{entry.asset_name}: 2d physics blockout without UCX_; no 3D mesh colliders added — author 2D colliders");
                return "none(2d blockout)";
            }

            return blockout ? AddMeshColliders(root) : AddSimple(entry, root, bounds);
        }

        #endregion

        #region Private Methods

        private static string AddSimple(AssetEntryDto entry, GameObject root, Bounds bounds)
        {
            if (bounds.size == Vector3.zero)
            {
                AssemblyContext.Warn(Step, $"{entry.asset_name}: empty renderer bounds; no collider");
                return "none(empty bounds)";
            }

            bool character = string.Equals(entry.type, "character", StringComparison.OrdinalIgnoreCase);
            if (AssemblyPhysics.Is2D)
            {
                return AddSimple2D(root, bounds, character);
            }

            if (character)
            {
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                capsule.center = bounds.center;
                capsule.direction = 1;
                capsule.height = bounds.size.y;
                capsule.radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
                return "capsule";
            }

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            return "box";
        }

        private static string AddSimple2D(GameObject root, Bounds bounds, bool character)
        {
            Vector2 offset = new Vector2(bounds.center.x, bounds.center.y);
            Vector2 size = new Vector2(bounds.size.x, bounds.size.y);
            if (character)
            {
                CapsuleCollider2D capsule = root.AddComponent<CapsuleCollider2D>();
                capsule.offset = offset;
                capsule.size = size;
                capsule.direction = CapsuleDirection2D.Vertical;
                return "capsule2d";
            }

            BoxCollider2D box = root.AddComponent<BoxCollider2D>();
            box.offset = offset;
            box.size = size;
            return "box2d";
        }

        private static string AddMeshColliders(GameObject root)
        {
            int added = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string nodeName = filter.gameObject.name;
                Renderer renderer = filter.GetComponent<Renderer>();
                bool helper = V57MarkerNaming.IsMarker(nodeName) || nodeName.StartsWith("Socket_", StringComparison.Ordinal);
                if (helper || renderer == null || !renderer.enabled || filter.sharedMesh == null)
                {
                    continue;
                }

                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                added++;
            }

            return $"mesh({added})";
        }

        #endregion
    }
}
