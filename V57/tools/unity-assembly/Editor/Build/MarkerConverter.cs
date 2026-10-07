using UnityEngine;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Creates one typed <see cref="V57Marker"/> under <c>_Environment/_Markers</c> per <c>Marker_*</c> node of the
    /// blockout or <c>Marker_*</c> object of a LevelMaps layout (same world pose). A marker is a volume when its kind is one
    /// (Zone, Bounds, Exit, Kill, CameraZone) or when the delivery says so (layout <c>shape: box</c>; a blockout marker of an
    /// unknown type that has a mesh): it gets a trigger BoxCollider (BoxCollider2D when package.physics is 2d) from the mesh
    /// bounds, or a 1 m cube scaled by the marker for layout markers. Camera markers also spawn a camera under <c>_Cameras</c>.
    /// The blockout's own marker nodes stay untouched (renderers already off).
    /// </summary>
    public static class MarkerConverter
    {
        #region Fields

        /// <summary>Layout markers have no mesh: their volume is a 1 m cube scaled by the exported scale.</summary>
        public static readonly Bounds UnitVolume = new Bounds(Vector3.zero, Vector3.one);

        #endregion

        #region Public Methods

        public static int Convert(GameObject blockout, SceneContainers containers, string sceneId)
        {
            int count = 0;
            foreach (Transform node in blockout.GetComponentsInChildren<Transform>(true))
            {
                MeshFilter filter = node.GetComponent<MeshFilter>();
                Bounds local = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.zero);
                bool unknownWithMesh = local.size != Vector3.zero && V57MarkerNaming.TryParse(node.name, out V57MarkerKind kind, out _) && kind == V57MarkerKind.Other;
                if (Create(containers, node.name, node.position, node.rotation, node.lossyScale, local, sceneId, unknownWithMesh))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Creates the typed marker; returns false when <paramref name="name"/> is not a <c>Marker_*</c> name.
        /// <paramref name="volume"/> forces a trigger volume regardless of the kind.</summary>
        public static bool Create(SceneContainers containers, string name, Vector3 position, Quaternion rotation, Vector3 scale, Bounds local, string sceneId, bool volume)
        {
            if (!V57MarkerNaming.TryParse(name, out V57MarkerKind kind, out string id))
            {
                return false;
            }

            GameObject markerObject = new GameObject(name);
            markerObject.transform.SetParent(containers.Markers, false);
            markerObject.transform.SetPositionAndRotation(position, rotation);
            markerObject.transform.localScale = scale;
            markerObject.AddComponent<V57Marker>().Configure(kind, id, local);
            if (volume || IsVolume(kind))
            {
                AddVolumeTrigger(markerObject, local, sceneId);
            }
            else if (kind == V57MarkerKind.Camera && !AssemblyOptions.LevelContentOnly)
            {
                CameraPlacement.Create(containers.Cameras, markerObject.transform, id, sceneId);
            }

            return true;
        }

        #endregion

        #region Private Methods

        private static bool IsVolume(V57MarkerKind kind)
        {
            return kind == V57MarkerKind.Zone || kind == V57MarkerKind.Bounds || kind == V57MarkerKind.Exit
                || kind == V57MarkerKind.Kill || kind == V57MarkerKind.CameraZone;
        }

        private static void AddVolumeTrigger(GameObject markerObject, Bounds local, string sceneId)
        {
            bool hasMesh = local.size != Vector3.zero;
            if (!hasMesh)
            {
                AssemblyContext.Warn("BuildLevelScenes", $"{sceneId}: {markerObject.name} has no mesh; 1 m trigger used — provider should model the volume");
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
