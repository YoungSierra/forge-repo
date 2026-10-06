using UnityEngine;

namespace V57.GameForge
{
    /// <summary>
    /// Graybox scene kit: ground, grid, hemisphere + directional light, day/night.
    /// </summary>
    public class GameForgeSceneKit : MonoBehaviour
    {
        public static GameForgeSceneKit Active { get; private set; }

        [SerializeField] private Light sun;
        [SerializeField] private Color daySky = new(0.53f, 0.72f, 0.88f);
        [SerializeField] private Color nightSky = new(0.11f, 0.16f, 0.28f);
        [SerializeField] private bool isDay = true;

        public bool IsDay => isDay;

        private void OnEnable() => Active = this;
        private void OnDisable()
        {
            if (Active == this) Active = null;
        }

        private void Start()
        {
            EnsureBasics();
            ApplySky();
        }

        public void ToggleDayNight()
        {
            isDay = !isDay;
            ApplySky();
        }

        public void SetDay(bool day)
        {
            isDay = day;
            ApplySky();
        }

        private void EnsureBasics()
        {
            if (sun == null)
            {
                var go = new GameObject("GameForge_Sun");
                go.transform.SetParent(transform, false);
                go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.1f;
            }

            if (transform.Find("GameForge_Ground") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "GameForge_Ground";
                ground.transform.SetParent(transform, false);
                ground.transform.localScale = new Vector3(5f, 1f, 5f);
            }
        }

        private void ApplySky()
        {
            var sky = isDay ? daySky : nightSky;
            if (Camera.main != null)
                Camera.main.backgroundColor = sky;
            RenderSettings.ambientLight = isDay ? sky * 0.6f : sky * 0.35f;
            if (sun != null)
                sun.intensity = isDay ? 1.1f : 0.35f;
        }
    }
}
