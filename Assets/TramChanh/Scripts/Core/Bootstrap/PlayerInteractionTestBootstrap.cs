using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Interaction.Player;
using TramChanh.UI.Prompt;
using UnityEngine;

namespace TramChanh.App
{
    /// <summary>Explicit scene-owned composition for self-contained player test scenes (§5 test-scene exception).</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PlayerInteractionTestBootstrap : MonoBehaviour
    {
        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private FirstPersonController _player;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private InteractionPromptView _prompt;
        [SerializeField] private int _actorId;
        [SerializeField] private HeldItemView _heldItemView;
        private UnityGameClock _clock;
        public BalanceConfig Balance => _balance;
        public PlayerInputReader Input => _input;
        public FirstPersonController Player => _player;
        public PlayerInteractor Interactor => _interactor;
        public InteractionPromptView Prompt => _prompt;
        public ActorRef Actor => new ActorRef(_actorId);
        public ServiceRegistry Services { get; private set; }

        private void Awake()
        {
            Services = new ServiceRegistry();
            var events = new EventBus();
            _clock = new UnityGameClock();
            Services.Register<IEventBus>(events);
            Services.Register<IGameClock>(_clock);
            var context = new InteractionContext(new ActorRef(_actorId), ActorRole.Lobby | ActorRole.Stall, new HeldItemSlot(events), _clock, events);
            _heldItemView?.Initialize(context.Hands, events);
            _input.Initialize(_clock);
            _player.Initialize(_input, _clock, _balance.MoveSpeed, _balance.LookSensitivity, _balance.Gravity, _balance.PitchLimit);
            _interactor.Initialize(_player.ViewCamera, _input, context, _balance.ReachDistance);
            _prompt.Initialize(events, _interactor);
        }
        private void Update() => _clock.Tick();
        private void OnDestroy()
        {
            _heldItemView?.Disconnect();
            _interactor.Shutdown();
            _prompt.Disconnect();
            Services?.Dispose();
        }
    }
}
