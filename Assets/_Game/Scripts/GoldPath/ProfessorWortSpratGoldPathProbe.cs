using ProfessorSprat.Gameplay.CameraSystem;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Flies;
using ProfessorSprat.Gameplay.Flow;
using ProfessorSprat.Gameplay.Player;
using ProfessorSprat.Gameplay.Sprat;
using ProfessorSprat.Gameplay.UI;
using UnityEngine;
using V57.GoldPath;

namespace ProfessorSprat.GoldPath
{
    /// <summary>
    /// Read-only gold path probe for SCN_HydroStation_Gameplay. Answers observable state (HUD text, level-end panel,
    /// Professor pose) and system state; never mutates gameplay. Unknown keys return false.
    /// </summary>
    public sealed class ProfessorWortSpratGoldPathProbe : MonoBehaviour, IGoldPathProbe
    {
        #region Fields

        [SerializeField] private ZoneFlow _flow;
        [SerializeField] private ProfessorLocomotion _professor;
        [SerializeField] private ZoneFlyTracker _flies;
        [SerializeField] private HUDController _hud;
        [SerializeField] private LevelEndView _levelEnd;
        [SerializeField] private RespawnService _respawn;
        [SerializeField] private SpratCompanion _sprat;
        [SerializeField] private CameraRig _camera;

        #endregion

        #region Public Methods

        public bool TryGetBool(string key, out bool value)
        {
            ZoneRuntime zone = _flow != null ? _flow.ActiveZone : null;
            switch (key)
            {
                case "player.grounded": value = _professor.IsGrounded; return true;
                case "player.stunned": value = _professor.IsStunned; return true;
                case "zone.complete": value = _flies.IsZoneComplete; return true;
                case "key.carried": value = zone?.Key != null && zone.Key.IsKeyCarried; return true;
                case "door.open": value = zone?.Door != null && zone.Door.IsOpen; return true;
                case "hud.key_visible": value = _hud != null && _hud.KeyGlyphVisible; return true;
                case "levelend.visible": value = _levelEnd != null && _levelEnd.IsVisible; return true;
                default: value = false; return false;
            }
        }

        public bool TryGetFloat(string key, out float value)
        {
            ZoneRuntime zone = _flow != null ? _flow.ActiveZone : null;
            Vector3 position = _professor.transform.position;
            switch (key)
            {
                case "player.x": value = position.x; return true;
                case "player.y": value = position.y; return true;
                case "player.z": value = position.z; return true;
                case "player.speed": value = _professor.HorizontalVelocity.magnitude; return true;
                case "flies.collected": value = _flies.CollectedCount; return true;
                case "flies.total": value = _flies.ZoneTotal; return true;
                case "crabs.defeated": value = CountDefeated(zone); return true;
                case "crab.distance": value = NearestCrabDistance(zone, position); return true;
                case "zone.index": value = _flow.ActiveZoneIndex; return true;
                case "respawn.count": value = _respawn != null ? _respawn.RespawnCount : 0; return true;
                case "camera.yaw": value = _camera != null ? _camera.ActiveYaw : 0f; return true;
                default: value = 0f; return false;
            }
        }

        public bool TryGetString(string key, out string value)
        {
            ZoneRuntime zone = _flow != null ? _flow.ActiveZone : null;
            switch (key)
            {
                case "zone.state": value = _flow.State.ToString(); return true;
                case "zone.id": value = zone?.Id ?? string.Empty; return true;
                case "key.state": value = zone?.Key != null ? zone.Key.State.ToString() : "None"; return true;
                case "player.state": value = _professor.State.ToString(); return true;
                case "hud.counter": value = _hud != null ? _hud.CounterText : string.Empty; return true;
                case "sprat.state": value = _sprat != null ? _sprat.State.ToString() : string.Empty; return true;
                default: value = string.Empty; return false;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            GoldPathProbeRegistry.Register(this);
        }

        private void OnDisable()
        {
            GoldPathProbeRegistry.Unregister(this);
        }

        #endregion

        #region Private Methods

        /// <summary>Horizontal distance to the nearest undefeated crab of the zone (999 when none).</summary>
        private static float NearestCrabDistance(ZoneRuntime zone, Vector3 position)
        {
            float nearest = 999f;
            if (zone == null)
            {
                return nearest;
            }

            foreach (CrabController crab in zone.Crabs)
            {
                if (crab != null && !crab.IsDefeated)
                {
                    Vector3 offset = crab.transform.position - position;
                    offset.y = 0f;
                    nearest = Mathf.Min(nearest, offset.magnitude);
                }
            }

            return nearest;
        }

                private static float CountDefeated(ZoneRuntime zone)
        {
            int count = 0;
            if (zone == null)
            {
                return count;
            }

            foreach (CrabController crab in zone.Crabs)
            {
                count += crab != null && crab.IsDefeated ? 1 : 0;
            }

            return count;
        }

        #endregion
    }
}
