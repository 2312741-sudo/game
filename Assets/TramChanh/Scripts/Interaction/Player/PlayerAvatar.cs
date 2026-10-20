using System;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using UnityEngine;

namespace TramChanh.Interaction.Player
{
    public enum PlayerViewMode { First, Third }

    /// <summary>
    /// The vendor's visible body. First person keeps the eye camera and shows the torso and arms (head hidden); third person pulls the
    /// camera back over the shoulder. Hand actions come from the action driver's <see cref="PlayerActionEvent"/>, so every station
    /// that goes through the driver (aimed or keyed) gets a matching gesture without station code knowing about the avatar.
    /// </summary>
    public sealed class PlayerAvatar : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _holdAnchor;
        [SerializeField, Tooltip("Camera offset from the eye in third person (right, up, back).")] private Vector3 _thirdPersonOffset = new Vector3(0.35f, 0.25f, -1.9f);
        [SerializeField] private Vector3 _firstPersonEye = new Vector3(0f, 1.62f, 0.1f);
        [SerializeField] private Vector3 _handGripOffset = new Vector3(0f, -0.04f, 0.06f);

        private readonly PlayerArmPose _pose = new PlayerArmPose();
        private PlayerInputReader _input;
        private IGameClock _clock;
        private IHeldItemSlot _hands;
        private IDisposable _actions, _held;
        private CharacterController _controller;
        private Animator _animator;
        private Transform _head, _rightHand, _eye;
        private Transform _firstPersonAnchorParent;
        private Vector3 _anchorLocalPosition;
        private Quaternion _anchorLocalRotation;
        private float _thirdDistance;
        private bool _hasSpeed;

        public PlayerViewMode Mode { get; private set; } = PlayerViewMode.First;
        /// <summary>Where the interaction ray starts: the eye, whichever way the camera is placed.</summary>
        public Transform Eye => _eye;

        public void Initialize(PlayerInputReader input, IGameClock clock, IEventBus events, IHeldItemSlot hands)
        {
            _input = input; _clock = clock; _hands = hands;
            _controller = GetComponent<CharacterController>();
            if (_body == null) { return; }
            if (_holdAnchor == null) { _holdAnchor = GetComponent<HeldItemView>()?.HoldAnchor; }
            _animator = _body.GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                foreach (AnimatorControllerParameter p in _animator.parameters) { if (p.nameHash == SpeedParameter) { _hasSpeed = true; } }
            }
            foreach (Collider c in _body.GetComponentsInChildren<Collider>(true)) { c.enabled = false; }
            foreach (Transform t in _body.GetComponentsInChildren<Transform>(true)) { if (t.name == "Head") { _head = t; } else if (t.name == "RightHand") { _rightHand = t; } }
            _pose.Bind(_body);

            var eye = new GameObject("AimEye").transform;
            eye.SetParent(transform, false);
            _eye = eye;
            if (_holdAnchor != null)
            {
                _firstPersonAnchorParent = _holdAnchor.parent;
                _anchorLocalPosition = _holdAnchor.localPosition;
                _anchorLocalRotation = _holdAnchor.localRotation;
            }
            _thirdDistance = _thirdPersonOffset.magnitude;
            _actions = events.Subscribe<PlayerActionEvent>(OnAction);
            _held = events.Subscribe<HeldItemChanged>(change => _pose.SetCarrying(change.Current != null));
            _pose.SetCarrying(hands.Current != null);
            SetMode(PlayerViewMode.First);
        }

        public void SetMode(PlayerViewMode mode)
        {
            Mode = mode;
            if (_holdAnchor == null) { return; }
            if (mode == PlayerViewMode.Third && _rightHand != null)
            {
                _holdAnchor.SetParent(_rightHand, false);
                _holdAnchor.localPosition = _handGripOffset;
                _holdAnchor.localRotation = Quaternion.identity;
            }
            else if (_firstPersonAnchorParent != null)
            {
                _holdAnchor.SetParent(_firstPersonAnchorParent, false);
                _holdAnchor.localPosition = _anchorLocalPosition;
                _holdAnchor.localRotation = _anchorLocalRotation;
            }
        }

        private void OnAction(PlayerActionEvent e)
        {
            PlayerArmPose.Gesture gesture = PlayerArmPose.ForPrompt(e.PromptKey);
            switch (e.Phase)
            {
                case PlayerActionPhase.Started: _pose.Play(gesture, e.Duration, loop: e.Kind != InteractionKind.Hold); break;
                case PlayerActionPhase.Completed: if (e.Kind == InteractionKind.Press) { _pose.Play(gesture, 0.7f, loop: false); } else { _pose.Stop(); } break;
                case PlayerActionPhase.Cancelled: _pose.Stop(); break;
            }
        }

        private void Update()
        {
            if (_input != null && _input.ViewTogglePressed) { SetMode(Mode == PlayerViewMode.First ? PlayerViewMode.Third : PlayerViewMode.First); }
        }

        private void LateUpdate()
        {
            if (_body == null || _camera == null || _eye == null) { return; }
            float delta = _clock != null && _clock.IsPaused ? 0f : Time.deltaTime;
            if (_hasSpeed && _controller != null)
            {
                Vector3 v = _controller.velocity; v.y = 0f;
                _animator.SetFloat(SpeedParameter, v.magnitude, 0.1f, Time.deltaTime);
            }
            _pose.Apply(delta);

            // The camera's rotation is owned by FirstPersonController (pitch on the camera, yaw on the body).
            Quaternion look = _camera.transform.localRotation;
            Vector3 eyeLocal = _firstPersonEye;
            _eye.localPosition = eyeLocal;
            _eye.rotation = _camera.transform.rotation;
            if (Mode == PlayerViewMode.First)
            {
                _camera.transform.localPosition = eyeLocal;
                if (_head != null) { _head.localScale = Vector3.one * 0.001f; }
            }
            else
            {
                if (_head != null) { _head.localScale = Vector3.one; }
                Vector3 eyeWorld = transform.TransformPoint(eyeLocal);
                Vector3 wanted = eyeWorld + transform.rotation * look * _thirdPersonOffset;
                Vector3 dir = wanted - eyeWorld;
                float distance = dir.magnitude;
                if (Physics.SphereCast(eyeWorld, 0.2f, dir / distance, out RaycastHit hit, distance, 1 << TramChanhLayers.EnvironmentIndex, QueryTriggerInteraction.Ignore))
                {
                    distance = Mathf.Max(0.4f, hit.distance);
                }
                _camera.transform.position = eyeWorld + dir / Mathf.Max(0.0001f, dir.magnitude) * distance;
            }
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
            _held?.Dispose();
        }
    }
}
