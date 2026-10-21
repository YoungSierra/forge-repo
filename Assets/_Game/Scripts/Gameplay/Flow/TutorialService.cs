using System;
using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.CameraSystem;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Flies;
using ProfessorSprat.Gameplay.Options;
using ProfessorSprat.Gameplay.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// Step-by-step training at the start of a run (owner request, D-287, after the reference build): move, look, jump,
    /// catch a fly, stomp. Each step completes from real play; the run clock waits until training ends. Tab or the
    /// gamepad's Select skips it; finishing or skipping clears the "training on next start" option.
    /// </summary>
    public sealed class TutorialService : MonoBehaviour
    {
        #region Fields

        private const float StepDoneHold = 1f;
        private const float MoveDistance = 4f;
        private const float LookDegrees = 60f;

        [SerializeField] private PresentationView _view;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private RunStats _stats;
        [SerializeField] private MissionCues _cues;

        private readonly List<Step> _steps = new List<Step>();
        private int _index = -1;
        private float _doneHold;
        private float _distance;
        private Vector3? _lastPosition;
        private float _look;
        private float _lastLookYaw;
        private bool _jumped;
        private bool _caughtFly;
        private bool _stomped;
        private bool _pad;
        private bool _shownPad;

        private sealed class Step
        {
            public string Title;
            public string Keyboard;
            public string Gamepad;
            public Func<bool> Done;
            public Func<Vector3?> Focus;
        }

        #endregion

        #region Public Methods

        public bool Active => _index >= 0;

        public int StepIndex => _index;

        public void Begin()
        {
            BuildSteps();
            _index = 0;
            _doneHold = 0f;
            _lastLookYaw = _cameraRig != null ? _cameraRig.LookYaw : 0f;
            _stats?.Hold(true);
            _view?.SetTimerTraining(true);
            Render();
        }

        public void Skip() => End(true);

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerMovedEvent>(OnMoved);
            EventBus.Subscribe<JumpStartedEvent>(OnJump);
            EventBus.Subscribe<FlyCollectedEvent>(OnFly);
            EventBus.Subscribe<StompLandedEvent>(OnStomp);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerMovedEvent>(OnMoved);
            EventBus.Unsubscribe<JumpStartedEvent>(OnJump);
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFly);
            EventBus.Unsubscribe<StompLandedEvent>(OnStomp);
        }

        private void Update()
        {
            if (!Active)
            {
                return;
            }

            TrackDevice();
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame)
            {
                Skip();
                return;
            }

            if (_cameraRig != null)
            {
                float yaw = _cameraRig.LookYaw;
                _look += Mathf.Abs(Mathf.DeltaAngle(_lastLookYaw, yaw));
                _lastLookYaw = yaw;
            }

            Step step = _steps[_index];
            if (_doneHold > 0f)
            {
                _doneHold -= Time.unscaledDeltaTime;
                if (_doneHold <= 0f)
                {
                    Advance();
                }
                return;
            }

            if (step.Done())
            {
                _doneHold = StepDoneHold;
                _view?.MarkTutorialStepDone(_index);
                _view?.SetMarker(null);
                return;
            }

            _view?.SetMarker(step.Focus?.Invoke());
        }

        #endregion

        #region Private Methods

        private void BuildSteps()
        {
            _steps.Clear();
            _steps.Add(new Step
            {
                Title = "MOVE",
                Keyboard = "W A S D or the arrow keys walk around the deck.",
                Gamepad = "The left stick walks around the deck.",
                Done = () => _distance > MoveDistance,
            });
            _steps.Add(new Step
            {
                Title = "LOOK AROUND",
                Keyboard = "Move the mouse to turn the camera. The wheel zooms.",
                Gamepad = "The right stick turns the camera. The d-pad zooms.",
                Done = () => _look > LookDegrees,
            });
            _steps.Add(new Step
            {
                Title = "JUMP",
                Keyboard = "Press SPACE to jump. Your shadow shows where you'll land.",
                Gamepad = "Press A to jump. Your shadow shows where you'll land.",
                Done = () => _jumped,
            });
            _steps.Add(new Step
            {
                Title = "CATCH A NEON FLY",
                Keyboard = "Touch a glowing cyan fly to catch it. The dotted trail leads to the next one.",
                Gamepad = "Touch a glowing cyan fly to catch it. The dotted trail leads to the next one.",
                Done = () => _caughtFly,
                Focus = NearestFly,
            });
            _steps.Add(new Step
            {
                Title = "STOMP",
                Keyboard = "Jump, then press SPACE again (or CTRL) in the air to STOMP. Land on a crab to flatten it; touching it from the side knocks you back.",
                Gamepad = "Jump, then press A again (or B) in the air to STOMP. Land on a crab to flatten it; touching it from the side knocks you back.",
                Done = () => _stomped,
                Focus = NearestCrab,
            });
        }

        private void Advance()
        {
            do
            {
                _index++;
            }
            while (_index < _steps.Count && _steps[_index].Done());

            if (_index >= _steps.Count)
            {
                End(false);
                return;
            }

            Render();
        }

        private void Render()
        {
            if (_view == null || _index < 0 || _index >= _steps.Count)
            {
                return;
            }

            Step step = _steps[_index];
            _shownPad = _pad;
            _view.ShowTutorial(_index, _steps.Count, step.Title, _pad ? step.Gamepad : step.Keyboard, _pad ? "SELECT · skip training" : "TAB · skip training");
        }

        private void End(bool skipped)
        {
            if (!Active)
            {
                return;
            }

            _index = -1;
            GameSettings.Tutorial = false;
            _view?.HideTutorial();
            _view?.SetTimerTraining(false);
            _stats?.Hold(false);
            _cues?.RunStarted(!skipped);
        }

        private void TrackDevice()
        {
            Gamepad pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.1f || pad.rightStick.ReadValue().sqrMagnitude > 0.1f || pad.buttonSouth.isPressed))
            {
                _pad = true;
            }
            else if (Keyboard.current != null && Keyboard.current.anyKey.isPressed || Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 4f)
            {
                _pad = false;
            }

            if (_pad != _shownPad && _doneHold <= 0f)
            {
                Render();
            }
        }

        private void OnMoved(PlayerMovedEvent moved)
        {
            if (Active && _lastPosition.HasValue && !moved.IsStunned)
            {
                Vector3 step = moved.Position - _lastPosition.Value;
                step.y = 0f;
                _distance += step.magnitude;
            }

            _lastPosition = moved.Position;
        }

        private void OnJump(JumpStartedEvent jump) => _jumped |= Active;

        private void OnFly(FlyCollectedEvent fly) => _caughtFly = true;

        private void OnStomp(StompLandedEvent stomp) => _stomped |= Active;

        private Vector3? NearestFly()
        {
            Vector3 from = _lastPosition ?? Vector3.zero;
            Vector3? best = null;
            float bestDistance = float.MaxValue;
            foreach (FlyController fly in FindObjectsByType<FlyController>(FindObjectsSortMode.None))
            {
                if (!fly.isActiveAndEnabled || fly.Collected)
                {
                    continue;
                }

                float d = (fly.transform.position - from).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = fly.transform.position;
                }
            }

            return best;
        }

        private Vector3? NearestCrab()
        {
            Vector3 from = _lastPosition ?? Vector3.zero;
            Vector3? best = null;
            float bestDistance = 30f * 30f;
            foreach (CrabController crab in FindObjectsByType<CrabController>(FindObjectsSortMode.None))
            {
                if (!crab.isActiveAndEnabled || crab.IsDefeated)
                {
                    continue;
                }

                float d = (crab.transform.position - from).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = crab.transform.position;
                }
            }

            return best;
        }

        #endregion
    }
}
