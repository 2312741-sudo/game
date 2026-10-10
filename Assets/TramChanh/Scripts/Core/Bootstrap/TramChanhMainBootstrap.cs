using System;
using System.Collections.Generic;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Core.Provisional;
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

namespace TramChanh.App
{
    /// <summary>
    /// Root composition for SCN_TramChanh_Main (ACCEL-01 "AccelGameplayBootstrap" seam).
    /// Places the validated drink station, cake station, Ready counter and Lobby order points on the
    /// roadside environment's named anchors and wires them to one OrderService. Art stays replaceable:
    /// only anchor names are read, environment visuals are never modified.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class TramChanhMainBootstrap : MonoBehaviour
    {
        public const string StallRootAnchor = "StallRoot";
        public const string PlayerSpawnAnchor = "PlayerSpawn";
        public const string TableAnchor = "TablePoint";
        public const string CakeTableAnchor = "CakeTablePoint";
        public const string VehicleAnchor = "VehiclePoint";

        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private ItemDefinition _drinkDefinition;
        [SerializeField] private CakeRecipe[] _cakeRecipes = Array.Empty<CakeRecipe>();
        [SerializeField] private GameObject _environmentPrefab;
        [SerializeField] private GameObject _drinkStationPrefab;
        [SerializeField] private GameObject _cakeStationPrefab;
        [SerializeField] private GameObject _tablePointPrefab;
        [SerializeField] private GameObject _vehiclePointPrefab;
        [Tooltip("Cake station origin in stall-local space; aligns the station grill with the stall Grill anchor.")]
        [SerializeField] private Vector3 _cakeStationLocalPosition = new Vector3(0.39f, 1f, -0.08f);
        [SerializeField, Tbd("DEC-010", "Customer pacing is a development placeholder.")] private float _customerReturnSeconds = 4f;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private FirstPersonController _player;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private HeldItemView _heldView;
        [SerializeField] private InteractionPromptView _prompt;
        [SerializeField] private OrderEntryUI _entryUI;
        [SerializeField] private LobbyOrderController _lobby;

        private readonly List<CustomerSeat> _seats = new List<CustomerSeat>();
        private UnityGameClock _clock;
        private EventBus _events;
        private IIdGenerator _ids;
        private GameObject _runtime;
        private int _nextCustomer = 1;
        private Animator[] _animators = Array.Empty<Animator>();

        public bool IsInitialized { get; private set; }
        public IOrderService Orders { get; private set; }
        public IStallTicketQueue Tickets { get; private set; }
        public IReadyShelf Shelf { get; private set; }
        public IEventBus Events => _events;
        public PlayerInteractor Interactor => _interactor;
        public GameObject Environment { get; private set; }
        public GameObject DrinkStation { get; private set; }
        public CakeStation CakeStation { get; private set; }
        public TableOrderPoint DrinkTable { get; private set; }
        public TableOrderPoint CakeTable { get; private set; }
        public VehicleOrderPoint Vehicle { get; private set; }
        public IReadOnlyList<OrderPoint> CustomerPoints => _seats.ConvertAll(seat => seat.Point);

        private void Awake()
        {
            if (_cakeRecipes == null || _cakeRecipes.Length == 0 || Array.Exists(_cakeRecipes, recipe => recipe == null || recipe.ItemDefinition == null))
            { throw new InvalidOperationException("Main scene requires configured cake recipes."); }
            var definitions = new List<ItemDefinition> { _drinkDefinition };
            foreach (CakeRecipe recipe in _cakeRecipes) { definitions.Add(recipe.ItemDefinition); }
            _events = new EventBus();
            _clock = new UnityGameClock();
            _ids = new SequentialIdGenerator();
            var service = new OrderService(_clock, _events, _ids, new ContentDatabase(definitions), 1, 1, error => UnityEngine.Debug.LogException(error));
            Orders = service;
            var queue = new StallTicketQueue(service);
            Tickets = queue;
            Shelf = new ReadyShelf(service, queue, 1, 1);
            var context = new InteractionContext(new ActorRef(1), ActorRole.Lobby | ActorRole.Stall, new HeldItemSlot(_events), _clock, _events);
            _input.Initialize(_clock);
            _player.Initialize(_input, _clock, _balance.MoveSpeed, _balance.LookSensitivity, _balance.Gravity, _balance.PitchLimit);
            _interactor.Initialize(_player.ViewCamera, _input, context, _balance.ReachDistance);
            _heldView.Initialize(context.Hands, _events);
            _prompt.Initialize(_events, _interactor, Orders);
            _entryUI.Initialize(_events);
            _lobby.Initialize(Orders, _events);
            _entryUI.Shown += OnEntryShown;
            _entryUI.Hidden += OnEntryHidden;
            Compose(queue);
        }

        private void Compose(StallTicketQueue queue)
        {
            _runtime = new GameObject("TramChanhMainRuntime");
            _runtime.transform.SetParent(transform, false);
            _runtime.SetActive(false);

            Environment = Instantiate(_environmentPrefab, _runtime.transform);
            Environment.name = _environmentPrefab.name;
            // The environment is view-only; the player camera is the only camera and listener.
            foreach (Camera camera in Environment.GetComponentsInChildren<Camera>(true)) { camera.enabled = false; }
            foreach (AudioListener listener in Environment.GetComponentsInChildren<AudioListener>(true)) { listener.enabled = false; }
            Transform stallRoot = RequireAnchor(StallRootAnchor);
            StallAnchorSet anchors = stallRoot.GetComponentInChildren<StallAnchorSet>(true);
            if (anchors == null) { throw new InvalidOperationException("StallRoot must contain the canonical StallAnchorSet."); }

            DrinkStation = Instantiate(_drinkStationPrefab, _runtime.transform);
            DrinkStation.transform.SetPositionAndRotation(anchors.transform.position, anchors.transform.rotation);
            GameObject cake = Instantiate(_cakeStationPrefab, _runtime.transform);
            cake.transform.SetPositionAndRotation(anchors.transform.TransformPoint(_cakeStationLocalPosition), anchors.transform.rotation);
            CakeStation = cake.GetComponent<CakeStation>();
            if (CakeStation == null) { throw new InvalidOperationException("Cake station prefab requires CakeStation."); }
            foreach (StallAnchorId id in new[] { StallAnchorId.TeaRack, StallAnchorId.Topping, StallAnchorId.IceBin, StallAnchorId.WipeArea, StallAnchorId.ReadyCounter,
                StallAnchorId.Grill, StallAnchorId.BatterArea, StallAnchorId.RollArea, StallAnchorId.Sauce, StallAnchorId.Wrap })
            { HideStallEquipment(anchors, id); }

            foreach (TeaRackController rack in DrinkStation.GetComponentsInChildren<TeaRackController>(true)) { rack.Initialize(Tickets, _ids, _events, Shelf); }
            foreach (ReadyCounterPoint ready in DrinkStation.GetComponentsInChildren<ReadyCounterPoint>(true)) { ready.Initialize(Shelf, _events); }
            foreach (ReadyOrderPickupPoint pickup in DrinkStation.GetComponentsInChildren<ReadyOrderPickupPoint>(true)) { pickup.Initialize(Shelf, Orders, _events); }
            CakeStation.Initialize(queue, _ids, _events, _clock, new CakeRecipeCatalog(queue, _cakeRecipes), Shelf);

            var drink = new[] { new ItemRequest(_drinkDefinition.Id, 1) };
            var cakeOnly = new[] { new ItemRequest(_cakeRecipes[0].ItemDefinition.Id, 1) };
            var mixed = new[] { new ItemRequest(_drinkDefinition.Id, 1), new ItemRequest(_cakeRecipes[0].ItemDefinition.Id, 1) };
            DrinkTable = PlaceTable(TableAnchor, 21, new TableId(1));
            CakeTable = PlaceTable(CakeTableAnchor, 23, new TableId(2));
            Vehicle = PlaceVehicle(VehicleAnchor, 22, new VehicleId(1));
            _seats.Add(new CustomerSeat(DrinkTable, drink));
            _seats.Add(new CustomerSeat(CakeTable, cakeOnly));
            _seats.Add(new CustomerSeat(Vehicle, mixed));

            Transform spawn = RequireAnchor(PlayerSpawnAnchor);
            Teleport(spawn.position, spawn.rotation);
            _animators = _runtime.GetComponentsInChildren<Animator>(true);
            _runtime.SetActive(true);
            foreach (CustomerSeat seat in _seats) { RequestService(seat); }
            IsInitialized = true;
        }

        private TableOrderPoint PlaceTable(string anchorName, int interactableId, TableId tableId)
        {
            GameObject table = PlacePoint(_tablePointPrefab, anchorName);
            TableOrderPoint point = table.GetComponent<TableOrderPoint>();
            point.Initialize(interactableId, table.transform.Find("InteractionPoint"), Orders, _lobby, tableId);
            return point;
        }

        private VehicleOrderPoint PlaceVehicle(string anchorName, int interactableId, VehicleId vehicleId)
        {
            GameObject vehicle = PlacePoint(_vehiclePointPrefab, anchorName);
            VehicleOrderPoint point = vehicle.GetComponent<VehicleOrderPoint>();
            point.Initialize(interactableId, vehicle.transform.Find("Anchors/InteractionPoint"), Orders, _lobby, vehicleId);
            return point;
        }

        private GameObject PlacePoint(GameObject prefab, string anchorName)
        {
            Transform anchor = RequireAnchor(anchorName);
            GameObject point = Instantiate(prefab, _runtime.transform);
            point.name = prefab.name + "_" + anchorName;
            point.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            // The environment already shows the furniture/vehicle at this anchor; keep only the gameplay seam.
            foreach (Renderer renderer in point.GetComponentsInChildren<Renderer>(true)) { renderer.enabled = false; }
            return point;
        }

        private Transform RequireAnchor(string anchorName)
        {
            // Composition anchors are direct children of the environment root; fall back to a deep search.
            Transform direct = Environment.transform.Find(anchorName);
            if (direct != null) { return direct; }
            foreach (Transform child in Environment.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == anchorName) { return child; }
            }
            throw new InvalidOperationException("Environment is missing anchor " + anchorName + ".");
        }

        private void Teleport(Vector3 position, Quaternion rotation)
        {
            var body = _player.GetComponent<CharacterController>();
            bool wasEnabled = body != null && body.enabled;
            if (body != null) { body.enabled = false; }
            _player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            if (body != null) { body.enabled = wasEnabled; }
        }

        private static void HideStallEquipment(StallAnchorSet set, StallAnchorId id)
        {
            if (!set.TryGet(id, out StallAnchor anchor)) { throw new InvalidOperationException("Missing stall anchor " + id + "."); }
            foreach (Transform child in anchor.transform) { child.gameObject.SetActive(false); }
        }

        private void RequestService(CustomerSeat seat)
        {
            Result<OrderId> result = seat.Point.RequestCustomerService(new CustomerId(_nextCustomer++), seat.Requests);
            if (!result.IsSuccess) { UnityEngine.Debug.LogWarning("Customer request rejected: " + result.ReasonKey, seat.Point); }
            seat.WaitingSeconds = 0f;
        }

        private void Update()
        {
            _clock?.Tick();
            foreach (Animator animator in _animators) { if (animator != null) { animator.speed = _clock.IsPaused ? 0f : 1f; } }
            if (!IsInitialized || _clock.IsPaused) { return; }
            // Completed orders free their point; a new customer arrives after the provisional pause.
            foreach (CustomerSeat seat in _seats)
            {
                if (seat.Point.ActiveOrder.IsValid) { continue; }
                seat.WaitingSeconds += (float)_clock.DeltaTime;
                if (seat.WaitingSeconds >= _customerReturnSeconds) { RequestService(seat); }
            }
        }

        private void OnEntryShown()
        {
            // Pointer input belongs to the modal UI while the order entry is open.
            _input.enabled = false;
            _input.SetCaptured(false);
        }

        private void OnEntryHidden()
        {
            // Scene teardown may destroy the player before the modal document raises Hidden.
            if (_lobby != null) { _lobby.CloseEntry(); }
            if (_input != null)
            {
                _input.enabled = true;
                _input.SetCaptured(true);
            }
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

        private sealed class CustomerSeat
        {
            public CustomerSeat(OrderPoint point, IReadOnlyList<ItemRequest> requests)
            {
                Point = point != null ? point : throw new ArgumentNullException(nameof(point));
                Requests = requests;
            }
            public OrderPoint Point { get; }
            public IReadOnlyList<ItemRequest> Requests { get; }
            public float WaitingSeconds { get; set; }
        }
    }
}
