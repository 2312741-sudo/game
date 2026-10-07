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
            bool buttonReleased = _driver.IsHeldUse ? _input.UseHeldReleased || !_input.UseHeldIsHeld
                : _input.InteractReleased || !_input.InteractIsHeld;
            if ((_driver.IsRunning && buttonReleased) || (!_input.IsCaptured && !_input.IsPausedByUser))
            {
                _driver.Release();
            }
            _driver.Tick(Focused);
            if (_input.InteractPressed)
            {
                Press();
            }
            else if (_input.UseHeldPressed)
            {
                UseHeld();
            }
            PublishPrompt();
        }
        public void RefreshFocus()
        {
            _focusedRef = null;
            if (_context != null && !_context.Clock.IsPaused && _input.IsCaptured)
            {
                Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
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
                if (target != null && !_driver.IsHeldUse)
                {
                    InteractionQuery query = target.Query(_context);
                    if (query.Availability.Status != AvailabilityStatus.Hidden)
                    {
                        state = new InteractionPromptChanged(target.Id, query, _driver.Progress);
                    }
                }
                if (!state.IsVisible && _context.Hands.Current is IHeldItemAction action)
                {
                    InteractableId id = (action as IInteractable)?.Id ?? default;
                    state = new InteractionPromptChanged(id, action.QueryUse(_context), _driver.IsHeldUse ? _driver.Progress : 0f, true);
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
