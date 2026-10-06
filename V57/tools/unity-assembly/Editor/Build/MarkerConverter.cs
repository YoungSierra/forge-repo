using UnityEngine;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Creates one typed <see cref="V57Marker"/> under <c>_Environment/_Markers</c> per <c>Marker_*</c> node of the
    /// blockout (same world pose). Zones get a trigger BoxCollider (BoxCollider2D when package.physics is 2d) from the marker mesh bounds; camera markers also
    /// spawn a camera under <c>_Cameras</c>. The blockout's own marker nodes stay untouched (renderers already off).
    /// </summary>
    public static class MarkerConverter
    {
        #region Public Methods

        public static int Convert(GameObject blockout, SceneContainers containers, string sceneId)
        {
            int count = 0;
            foreach (Transform node in blockout.GetComponentsInChildren<Transform>(true))
            {
                if (!V57MarkerNaming.TryParse(node.name, out V57MarkerKind kind, out string id))
                {
                    continue;
                }

                GameObject markerObject = new GameObject(node.name);
                markerObject.transform.SetParent(containers.Markers, false);
                markerObject.transform.SetPositionAndRotation(node.position, node.rotation);
                markerObject.transform.localScale = node.lossyScale;
                MeshFilter filter = node.GetComponent<MeshFilter>();
                Bounds local = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.zero);
                markerObject.AddComponent<V57Marker>().Configure(kind, id, local);
                if (kind == V57MarkerKind.Zone)
                {
                    AddZoneTrigger(markerObject, local, sceneId);
                }
                else if (kind == V57MarkerKind.Camera)
                {
                    CameraPlacement.Create(containers.Cameras, markerObject.transform, id, sceneId);
                }

                count++;
            }

            return count;
        }

        #endregion

        #region Private Methods

        private static void AddZoneTrigger(GameObject markerObject, Bounds local, string sceneId)
        {
            bool hasMesh = local.size != Vector3.zero;
            if (!hasMesh)
            {
                AssemblyContext.Warn("BuildLevelScenes", $"{sceneId}: {markerObject.name} has no mesh; 1 m trigger used — provider should model the zone volume");
            }

            Vector3 center = hasMesh ? local.center : Vector3.zero;
            Vector3 size = hasMesh ? local.size : Vector3.one;
            if (AssemblyPhysics.Is2D)
            {
                BoxCollider2D trigger2D = markerObject.AddComponent<BoxCollider2D>();
                trigger2D.isTrigger = true;
                trigger2D.offset = new Vector2(center.x, center.y);
                trigger2D.size = new Vector2(size.x, size.y);
                return;
            }

            BoxCollider trigger = markerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = center;
            trigger.size = size;
        }

        #endregion
    }
}
