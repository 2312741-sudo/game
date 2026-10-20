using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction.Player;
using UnityEngine;

namespace TramChanh.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private Camera _camera;
        private PlayerInputReader _input;
        private InteractionContext _context;
        private InteractionActionDriver _driver;
        private InteractableRef _focusedRef;
        private float _reachDistance;
        private IInteractable _keyedTarget;
        private Transform _aimOrigin;
        private const float KeyedRange = 3f;
        private static readonly Collider[] KeyedBuffer = new Collider[32];
        /// <summary>Optional ray origin (the avatar's eye); defaults to the camera so third person still aims from the eye.</summary>
        public void SetAimOrigin(Transform aim) { _aimOrigin = aim; }
        private Transform Aim => _aimOrigin != null ? _aimOrigin : _camera.transform;
        public InteractionPromptChanged Prompt { get; private set; }
        public IInteractable Focused => _focusedRef != null ? _focusedRef.Target : null;
        public InteractionContext Context => _context;

        public void Initialize(Camera camera, PlayerInputReader input, InteractionContext context, float reachDistance)
        {
            _camera = camera;
            _input = input;
            _context = context;
            _reachDistance = reachDistance;
            _driver = new InteractionActionDriver(context);
        }
        private void Update()
        {
            if (_context == null)
            {
                return;
            }
            RefreshFocus();
            if (!_driver.IsRunning) { _keyedTarget = null; }
            bool keyed = _keyedTarget != null;
            bool buttonReleased = keyed ? _input.CakeStepReleased || !_input.CakeStepIsHeld
                : _driver.IsHeldUse ? _input.UseHeldReleased || !_input.UseHeldIsHeld
                : _input.InteractReleased || !_input.InteractIsHeld;
            if ((_driver.IsRunning && buttonReleased) || (!_input.IsCaptured && !_input.IsPausedByUser))
            {
                _driver.Release();
            }
            _driver.Tick(keyed ? _keyedTarget : Focused);
            if (_input.InteractPressed)
            {
                Press();
            }
            else if (_input.UseHeldPressed)
            {
                UseHeld();
            }
            else if (_input.CakeStepPressed)
            {
                PressKeyed();
            }
            PublishPrompt();
        }
        public void RefreshFocus()
        {
            _focusedRef = null;
            if (_context != null && !_context.Clock.IsPaused && _input.IsCaptured)
            {
                Ray ray = new Ray(Aim.position, Aim.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, _reachDistance, 1 << TramChanhLayers.InteractableIndex, QueryTriggerInteraction.Collide))
                {
                    _focusedRef = hit.collider.GetComponentInParent<InteractableRef>();
                }
            }
            PublishPrompt();
        }
        public void Press()
        {
            if (_context != null && _input.IsCaptured)
            {
                _driver.Begin(Focused);
            }
            PublishPrompt();
        }
        /// <summary>Runs the next workflow step of whatever keyed station is within reach, regardless of where the crosshair points.</summary>
        public void PressKeyed()
        {
            if (_context == null || !_input.IsCaptured || _driver.IsRunning) { return; }
            IInteractable target = ResolveKeyedTarget();
            if (target == null) { return; }
            _driver.Begin(target);
            _keyedTarget = _driver.IsRunning ? target : null;
        }

        private IInteractable ResolveKeyedTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(Aim.position, KeyedRange, KeyedBuffer, 1 << TramChanhLayers.InteractableIndex, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                InteractableRef reference = KeyedBuffer[i].GetComponentInParent<InteractableRef>();
                if (reference != null && reference.Target is IKeyedStepSource source)
                {
                    IInteractable step = source.ResolveKeyedStep(_context);
                    if (step != null) { return step; }
                }
            }
            return null;
        }

        public void UseHeld()
        {
            if (_context != null && _input.IsCaptured)
            {
                _driver.BeginHeld();
            }
            PublishPrompt();
        }

        private void PublishPrompt()
        {
            if (_context == null)
            {
                return;
            }
            InteractionPromptChanged state = default;
            if (isActiveAndEnabled && _input.IsCaptured && !_context.Clock.IsPaused)
            {
                IInteractable target = Focused;
                InteractionQuery heldQuery = _context.Hands.Current is IHeldItemAction heldAction ? heldAction.QueryUse(_context) : default;
                float heldProgress = _driver.IsHeldUse ? _driver.Progress : 0f;
                if (target != null)
                {
                    InteractionQuery query = target.Query(_context);
                    if (query.Availability.Status != AvailabilityStatus.Hidden)
                    {
                        state = new InteractionPromptChanged(target.Id, query, _driver.IsHeldUse ? 0f : _driver.Progress, false, heldQuery, heldProgress);
                    }
                }
                if (!state.IsVisible && _context.Hands.Current is IHeldItemAction action)
                {
                    InteractableId id = (action as IInteractable)?.Id ?? default;
                    state = new InteractionPromptChanged(id, heldQuery, heldProgress, true);
                }
            }
            if (!state.Equals(Prompt))
            {
                Prompt = state;
                _context.Events.Publish(state);
            }
        }
        public void Shutdown()
        {
            _driver?.Release();
            _focusedRef = null;
            if (_context != null && !Prompt.Equals(default(InteractionPromptChanged)))
            {
                Prompt = default;
                _context.Events.Publish(Prompt);
            }
            _context = null;
        }
        private void OnDisable()
        {
            _driver?.Release();
            _focusedRef = null;
            PublishPrompt();
        }
    }
}
