using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.CameraSystem;
using ProfessorSprat.Gameplay.Flies;
using ProfessorSprat.Gameplay.Input;
using ProfessorSprat.Gameplay.Player;
using ProfessorSprat.Gameplay.Sprat;
using ProfessorSprat.Gameplay.UI;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>ZoneFlow lifecycle.</summary>
    public enum ZoneFlowState
    {
        Booting,
        Playing,
        LevelEnd,
        Invalid
    }

    /// <summary>
    /// TDD §B-S ZoneFlow — world owner of every gameplay scene and session bootstrap. Init order: load save → read the
    /// level contract from the level containers → spawn the Professor → bind camera, Sprat, respawn → start zone 1 →
    /// enable input. A zone is cleared when its door is open and the Professor enters its exit; the last exit ends the level.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class ZoneFlow : MonoBehaviour
    {
        #region Fields

        [SerializeField] private string _levelId = "HydroStation";
        [Header("Level containers (never level objects)")]
        [SerializeField] private Transform _markersRoot;
        [SerializeField] private Transform _levelActorsRoot;
        [Header("Systems")]
        [SerializeField] private ProfessorLocomotion _professor;
        [SerializeField] private SpratCompanion _sprat;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private ZoneFlyTracker _flyTracker;
        [SerializeField] private RespawnService _respawn;
        [SerializeField] private InputHandler _input;
        [SerializeField] private SaveService _save;
        [SerializeField] private LevelEndView _levelEnd;

        private LevelContent _content;
        private bool _doorOpen;
        private float _zoneStart;

        #endregion

        #region Public Methods

        public ZoneFlowState State { get; private set; } = ZoneFlowState.Booting;

        public int ActiveZoneIndex { get; private set; } = -1;

        public string ActiveZoneId => ActiveZone?.Id;

        public int ZoneCount => _content != null ? _content.Zones.Count : 0;

        public LevelContent Content => _content;

        public ZoneRuntime ActiveZone => _content != null && ActiveZoneIndex >= 0 && ActiveZoneIndex < _content.Zones.Count ? _content.Zones[ActiveZoneIndex] : null;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DoorUnlockedEvent>(OnDoorUnlocked);
        }

        private void Start()
        {
            if (_markersRoot == null || _levelActorsRoot == null || _professor == null || _flyTracker == null)
            {
                Fail("level containers or systems not bound");
                return;
            }

            if (_save != null)
            {
                _save.Load();
            }
            _content = LevelContent.Scan(_markersRoot, _levelActorsRoot);
            if (_content.Problems.Count > 0)
            {
                Debug.LogWarning($"{nameof(ZoneFlow)}: level contract (TDD §6) issues:\n{_content.Report()}", this);
            }

            if (_content.Spawn == null || _content.Zones.Count == 0)
            {
                Fail("level contract has no player spawn or zone");
                return;
            }

            _professor.Teleport(_content.Spawn.position, _content.Spawn.rotation);
            if (_cameraRig != null)
            {
                _cameraRig.Bind(_professor.Body, _content.DefaultYaw, _content.CameraZones, _content.CameraZoneYaws);
            }
            if (_sprat != null)
            {
                _sprat.Bind(_professor.Body);
            }
            if (_respawn != null)
            {
                _respawn.Bind(_content.KillVolumes, _content.Spawn);
            }
            BeginZone(0);
            if (_input != null)
            {
                _input.SetGameplayEnabled(true);
            }
            State = ZoneFlowState.Playing;
        }

        private void FixedUpdate()
        {
            ZoneRuntime zone = ActiveZone;
            if (State != ZoneFlowState.Playing || zone == null || !_doorOpen || !ZoneRuntime.Contains(zone.Exit, _professor.Center))
            {
                return;
            }

            ClearZone(zone);
        }

        #endregion

        #region Private Methods

        private void BeginZone(int index)
        {
            ActiveZoneIndex = index;
            ZoneRuntime zone = ActiveZone;
            _doorOpen = false;
            _zoneStart = Time.time;
            _flyTracker.BeginZone(zone.Id, zone.Flies);
            foreach (Crab.CrabController crab in zone.Crabs)
            {
                crab.Init(_content.PatrolRange(crab));
            }

            if (zone.Key != null)
            {
                zone.Key.Arm(zone.Id, zone.Door);
            }
            EventBus.Publish(new ZoneStartedEvent(zone.Id, _flyTracker.ZoneTotal));
        }

        private void ClearZone(ZoneRuntime zone)
        {
            bool last = ActiveZoneIndex >= _content.Zones.Count - 1;
            EventBus.Publish(new ZoneClearedEvent(_levelId, zone.Id, _flyTracker.CollectedCount, _flyTracker.ZoneTotal, Time.time - _zoneStart, last));
            if (!last)
            {
                BeginZone(ActiveZoneIndex + 1);
                return;
            }

            State = ZoneFlowState.LevelEnd;
            if (_input != null)
            {
                _input.SetGameplayEnabled(false);
            }
            if (_levelEnd != null && _save != null)
            {
                _levelEnd.Show(_save.Session.completedZones);
            }
        }

        private void OnDoorUnlocked(DoorUnlockedEvent unlocked)
        {
            if (unlocked.ZoneId == ActiveZoneId)
            {
                _doorOpen = true;
            }
        }

        private void Fail(string reason)
        {
            State = ZoneFlowState.Invalid;
            Debug.LogError($"{nameof(ZoneFlow)}: {reason}; gameplay disabled for this scene.", this);
            if (_input != null)
            {
                _input.SetGameplayEnabled(false);
            }
        }

        #endregion
    }
}
