using System;
using System.Collections;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Interaction.Player;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Stall.Anchors;
using TramChanh.Stall.Runtime;
using TramChanh.UI.Orders;
using TramChanh.UI.Prompt;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.App
{
    [DefaultExecutionOrder(-1000)]
    public sealed class DrinkWaveBootstrap : MonoBehaviour
    {
        [SerializeField] private DrinkWaveSceneLoader _loader;
        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private ItemDefinition _drinkDefinition;
        [SerializeField] private GameObject _stationPrefab;
        [SerializeField] private GameObject _tablePointPrefab;
        [SerializeField] private GameObject _vehiclePointPrefab;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private FirstPersonController _player;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private HeldItemView _heldView;
        [SerializeField] private InteractionPromptView _prompt;
        [SerializeField] private OrderEntryUI _entryUI;
        [SerializeField] private LobbyOrderController _lobby;
        private UnityGameClock _clock;
        private EventBus _events;
        private IIdGenerator _ids;
        private GameObject _runtime;
        private Animator[] _animators = Array.Empty<Animator>();
        public bool IsInitialized { get; private set; }
        public IOrderService Orders { get; private set; }
        public IStallTicketQueue Tickets { get; private set; }
        public IReadyShelf Shelf { get; private set; }
        public TableOrderPoint Table { get; private set; }
        public VehicleOrderPoint Vehicle { get; private set; }
        public PlayerInteractor Interactor => _interactor;
        public IEventBus Events => _events;
        public GameObject Station { get; private set; }

        private void Awake()
        {
            _events = new EventBus();
            _clock = new UnityGameClock();
            _ids = new SequentialIdGenerator();
            var service = new OrderService(_clock, _events, _ids, new ContentDatabase(new[] { _drinkDefinition }), 1, 1, error => UnityEngine.Debug.LogException(error));
            Orders = service;
            Tickets = new StallTicketQueue(service);
            Shelf = new ReadyShelf(service, Tickets, 1, 1);
            var context = new InteractionContext(new ActorRef(1), ActorRole.Lobby | ActorRole.Stall, new HeldItemSlot(_events), _clock, _events);
            _input.Initialize(_clock);
            _player.Initialize(_input, _clock, _balance.MoveSpeed, _balance.LookSensitivity, _balance.Gravity, _balance.PitchLimit);
            _interactor.Initialize(_player.ViewCamera, _input, context, _balance.ReachDistance);
            _heldView.Initialize(context.Hands, _events);
            _prompt.Initialize(_events, _interactor);
            _entryUI.Initialize(_events);
            _lobby.Initialize(Orders, _events);
            _entryUI.Shown += OnEntryShown;
            _entryUI.Hidden += OnEntryHidden;
        }

        private IEnumerator Start()
        {
            yield return _loader.LoadBase();
            StallAnchorSet anchors = null;
            Transform originalTable = null;
            foreach (GameObject root in _loader.BaseScene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true)) { camera.enabled = false; }
                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true)) { listener.enabled = false; }
                if (root.TryGetComponent(out StallAnchorSet found)) { anchors = found; }
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "PF_YellowCrateTable") { originalTable = child; }
                }
            }
            if (anchors == null || originalTable == null) { throw new InvalidOperationException("Saved blockout requires stall anchors and table."); }
            _runtime = new GameObject("DrinkWaveRuntime");
            SceneManager.MoveGameObjectToScene(_runtime, gameObject.scene);
            _runtime.SetActive(false);
            Station = Instantiate(_stationPrefab, _runtime.transform);
            Station.transform.SetPositionAndRotation(anchors.transform.position, anchors.transform.rotation);
            DisableEquipment(anchors, StallAnchorId.TeaRack);
            DisableEquipment(anchors, StallAnchorId.Topping);
            DisableEquipment(anchors, StallAnchorId.IceBin);
            DisableEquipment(anchors, StallAnchorId.ReadyCounter);
            DisableEquipment(anchors, StallAnchorId.WipeArea);
            foreach (TeaRackController rack in Station.GetComponentsInChildren<TeaRackController>(true)) { rack.Initialize(Tickets, _ids, _events); }
            foreach (ReadyCounterPoint ready in Station.GetComponentsInChildren<ReadyCounterPoint>(true)) { ready.Initialize(Shelf, _events); }
            foreach (ReadyOrderPickupPoint pickup in Station.GetComponentsInChildren<ReadyOrderPickupPoint>(true)) { pickup.Initialize(Shelf); }
            GameObject table = Instantiate(_tablePointPrefab, _runtime.transform);
            table.transform.SetPositionAndRotation(originalTable.position, originalTable.rotation);
            originalTable.gameObject.SetActive(false);
            Table = table.GetComponent<TableOrderPoint>();
            Table.Initialize(21, table.transform.Find("InteractionPoint"), Orders, _lobby, new TableId(1));
            GameObject vehicle = Instantiate(_vehiclePointPrefab, _runtime.transform);
            Vehicle = vehicle.GetComponent<VehicleOrderPoint>();
            Vehicle.Initialize(22, vehicle.transform.Find("Anchors/InteractionPoint"), Orders, _lobby, new VehicleId(1));
            _animators = Station.GetComponentsInChildren<Animator>(true);
            _runtime.SetActive(true);
            var requests = new[] { new ItemRequest(_drinkDefinition.Id, 1) };
            Result<OrderId> tableRequest = Table.RequestCustomerService(new CustomerId(1), requests);
            Result<OrderId> vehicleRequest = Vehicle.RequestCustomerService(new CustomerId(2), requests);
            if (!tableRequest.IsSuccess || !vehicleRequest.IsSuccess) { throw new InvalidOperationException("Scripted customer requests could not be created."); }
            IsInitialized = true;
        }
        private static void DisableEquipment(StallAnchorSet set, StallAnchorId id)
        {
            if (!set.TryGet(id, out StallAnchor anchor)) { throw new InvalidOperationException("Missing station anchor."); }
            foreach (Transform child in anchor.transform) { child.gameObject.SetActive(false); }
        }
        private void Update()
        {
            _clock?.Tick();
            foreach (Animator animator in _animators) { if (animator != null) { animator.speed = _clock.IsPaused ? 0f : 1f; } }
        }
        private void OnEntryShown()
        {
            // Disable gameplay input while pointer input belongs to the modal UI.
            // Its left-click binding must not recapture the cursor over a UI button.
            _input.enabled = false;
            _input.SetCaptured(false);
        }
        private void OnEntryHidden()
        {
            _lobby.CloseEntry();
            _input.enabled = true;
            _input.SetCaptured(true);
        }
        private void OnDestroy()
        {
            if (_entryUI != null)
            {
                _entryUI.Shown -= OnEntryShown;
                _entryUI.Hidden -= OnEntryHidden;
                _entryUI.Disconnect();
            }
            if (_interactor != null) { _interactor.Shutdown(); }
            if (_heldView != null) { _heldView.Disconnect(); }
            if (_prompt != null) { _prompt.Disconnect(); }
            (Shelf as IDisposable)?.Dispose();
            _events?.Dispose();
        }
    }
}
