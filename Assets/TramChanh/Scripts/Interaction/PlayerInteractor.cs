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
            _driver.Tick(Focused);
            if (_input.InteractPressed)
            {
                Press();
            }
            if (_input.InteractReleased)
            {
                _driver.Release();
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
            _driver?.Begin(Focused);
            PublishPrompt();
        }
        private void PublishPrompt()
        {
            if (_context == null)
            {
                return;
            }
            IInteractable target = Focused;
            InteractionPromptChanged state = target == null ? default
                : new InteractionPromptChanged(target.Id, target.Query(_context), _driver.Progress);
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
            PublishPrompt();
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
