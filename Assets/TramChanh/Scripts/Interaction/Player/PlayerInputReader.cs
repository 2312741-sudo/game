using TramChanh.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TramChanh.Interaction.Player
{
    [DefaultExecutionOrder(-300)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        private InputActionAsset _runtimeActions;
        private InputActionMap _gameplay;
        private InputAction _move;
        private InputAction _look;
        private InputAction _interact;
        private InputAction _pause;
        private IGameClock _clock;
        private bool _resumedThisFrame;
        public InputActionAsset Actions => _actions;
        public bool IsCaptured { get; private set; }
        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();
        public bool InteractPressed => IsCaptured && !_resumedThisFrame && _interact.WasPressedThisFrame();
        public bool InteractReleased => _interact.WasReleasedThisFrame();

        public void Initialize(IGameClock clock)
        {
            _clock = clock;
            EnsureActions();
            SetCaptured(true);
        }
        private void EnsureActions()
        {
            if (_runtimeActions != null)
            {
                return;
            }
            _runtimeActions = Instantiate(_actions);
            _gameplay = _runtimeActions.FindActionMap("Gameplay", true);
            _move = _gameplay.FindAction("Move", true);
            _look = _gameplay.FindAction("Look", true);
            _interact = _gameplay.FindAction("Interact", true);
            _pause = _gameplay.FindAction("Pause", true);
        }
        private void OnEnable()
        {
            if (_actions != null)
            {
                EnsureActions();
                _gameplay.Enable();
            }
        }
        private void Update()
        {
            _resumedThisFrame = false;
            if (_pause.WasPressedThisFrame())
            {
                SetCaptured(!IsCaptured);
                _resumedThisFrame = IsCaptured;
            }
            else if (!IsCaptured && _interact.WasPressedThisFrame())
            {
                SetCaptured(true);
                _resumedThisFrame = true;
            }
        }
        public void SetCaptured(bool captured)
        {
            IsCaptured = captured;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
            if (_clock != null)
            {
                if (captured)
                {
                    _clock.Resume();
                }
                else
                {
                    _clock.Pause();
                }
            }
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SetCaptured(false);
            }
        }
        private void OnDisable()
        {
            _gameplay?.Disable();
            SetCaptured(false);
        }
        private void OnDestroy()
        {
            if (_runtimeActions != null)
            {
                Destroy(_runtimeActions);
            }
        }
    }
}
