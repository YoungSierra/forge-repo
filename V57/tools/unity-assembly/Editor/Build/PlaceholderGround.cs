using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Used when a scene declares no blockout: a ground plane sized from <c>package.world.play_area_m</c> (x, z;
    /// default 20 m), MAT_V57_Placeholder, tagged with <see cref="V57Placeholder"/> so reviewers and reports count it.
    /// </summary>
    public static class PlaceholderGround
    {
        #region Fields

        private const float DefaultSizeMeters = 20f;
        private const float UnityPlaneSizeMeters = 10f;

        #endregion

        #region Public Methods

        public static GameObject Create(Transform environment, string sceneId)
        {
            float[] area = GeneratedData.Package?.world?.play_area_m;
            float width = area != null && area.Length >= 1 && area[0] > 0f ? area[0] : DefaultSizeMeters;
            float depth = area != null && area.Length >= 3 && area[2] > 0f ? area[2] : width;
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BLK_Placeholder_Ground";
            ground.transform.SetParent(environment, false);
            ground.transform.localScale = new Vector3(width / UnityPlaneSizeMeters, 1f, depth / UnityPlaneSizeMeters);
            ground.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterial.GetOrCreate();
            if (AssemblyPhysics.Is2D)
            {
                Object.DestroyImmediate(ground.GetComponent<Collider>());
            }

            ground.AddComponent<V57Placeholder>().Configure("BLK_" + sceneId, "BLK_Placeholder_Ground", "scene declares no blockout");
            AssemblyContext.Warn("BuildLevelScenes", $"{sceneId}: no blockout declared; placeholder ground {width}x{depth} m (play_area_m or default)");
            return ground;
        }

        #endregion
    }
}
