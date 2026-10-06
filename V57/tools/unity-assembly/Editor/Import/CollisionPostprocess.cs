using System;
using UnityEngine;
using V57.GoldPath;

namespace V57.Assembly.Import
{
    /// <summary>
    /// Runs in OnPostprocessModel: <c>UCX_*</c> nodes get a convex MeshCollider from their own mesh and
    /// their renderer disabled; <c>Marker_*</c> and <c>Socket_*</c> renderers are disabled (nodes kept).
    /// </summary>
    public static class CollisionPostprocess
    {
        #region Public Methods

        public static void Apply(GameObject root, string assetPath)
        {
            int colliders = 0;
            int markers = 0;
            int sockets = 0;
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                string nodeName = node.name;
                if (nodeName.StartsWith("UCX_", StringComparison.Ordinal))
                {
                    MeshFilter filter = node.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null && node.GetComponent<MeshCollider>() == null)
                    {
                        MeshCollider collider = node.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = filter.sharedMesh;
                        collider.convex = true;
                        colliders++;
                    }

                    DisableRenderer(node);
                }
                else if (V57MarkerNaming.IsMarker(nodeName))
                {
                    DisableRenderer(node);
                    markers++;
                }
                else if (nodeName.StartsWith("Socket_", StringComparison.Ordinal))
                {
                    DisableRenderer(node);
                    sockets++;
                }
            }

            if (colliders + markers + sockets > 0)
            {
                ImportLog.Record(assetPath, "model-nodes", $"UCX convex colliders={colliders} markers={markers} sockets={sockets} (renderers disabled)");
            }
        }

        #endregion

        #region Private Methods

        private static void DisableRenderer(Transform node)
        {
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        #endregion
    }
}
