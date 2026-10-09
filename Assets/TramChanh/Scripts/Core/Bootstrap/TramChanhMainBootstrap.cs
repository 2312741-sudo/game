using System;
using System.Collections.Generic;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.Customers;
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
        public const int TableCount = 10;
        public const int FirstTableInteractableId = 31;
        public const string SeatAnchor = "Seat";
        private const double MaxCustomerTickSeconds = 0.25d;
        // Vehicle customers use their own id range so they never share an id with director-seated customers (100+).
        private const int FirstVehicleCustomerId = 1000000;
        /// <summary>Environment anchor name for table n (1-based): TABLE_01..TABLE_10.</summary>
        public static string TableAnchorName(int tableNumber) => "TABLE_" + tableNumber.ToString("00");

        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private ItemDefinition _drinkDefinition;
        [SerializeField] private CakeRecipe[] _cakeRecipes = Array.Empty<CakeRecipe>();
        [SerializeField] private GameObject _environmentPrefab;
        [Tooltip("Optional modular art prefabs (customer area, buildings, trees, street lights...). They are instantiated under the environment root, so anchors inside them (e.g. TABLE_01..TABLE_10) are found like environment anchors. Visual-only: they must not carry gameplay components.")]
        [SerializeField] private GameObject[] _additionalEnvironmentPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject _drinkStationPrefab;
        [SerializeField] private GameObject _cakeStationPrefab;
        [SerializeField] private GameObject _tablePointPrefab;
        [SerializeField] private GameObject _vehiclePointPrefab;
        [Tooltip("Cake station origin in stall-local space; aligns the station grill with the stall Grill anchor.")]
        [SerializeField] private Vector3 _cakeStationLocalPosition = new Vector3(0.39f, 1f, -0.08f);
        [SerializeField, Tbd("DEC-010", "Customer pacing is a development placeholder.")] private float _customerReturnSeconds = 4f;
        [Tooltip("Dine-in customer arrivals for TABLE_01..TABLE_10 (max active customers, pacing, menu weights).")]
        [SerializeField] private CustomerDirectorSettings _customerSettings = new CustomerDirectorSettings();
        [SerializeField] private int _customerSeed = 1;
        [Tooltip("Optional seated customer visual (Antigravity). Empty: a primitive placeholder is used.")]
        [SerializeField] private GameObject _customerVisualPrefab;
        [Tooltip("Environment-root-local positions for TABLE_03..TABLE_10 when the environment has no TABLE_xx anchors.")]
        [SerializeField, Tbd("DEC-010", "Development customer-area layout until Antigravity's customer area prefab provides TABLE_xx anchors.")]
        private Vector3[] _fallbackTablePositions =
        {
            new Vector3(-4.6f, 0f, 1.7f), new Vector3(-5.85f, 0f, 0.2f), new Vector3(-7.1f, 0f, 1.7f), new Vector3(-3.35f, 0f, -1.8f),
            new Vector3(-5.85f, 0f, -1.8f), new Vector3(4.2f, 0f, 0.6f), new Vector3(5.6f, 0f, -0.9f), new Vector3(4.2f, 0f, -2.2f),
        };
        [Tooltip("Seat offset in table-local space when a table anchor has no Seat child.")]
        [SerializeField] private Vector3 _defaultSeatOffset = new Vector3(-0.7f, 0f, 0f);
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private FirstPersonController _player;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private HeldItemView _heldView;
        [SerializeField] private InteractionPromptView _prompt;
        [SerializeField] private OrderEntryUI _entryUI;
        [SerializeField] private LobbyOrderController _lobby;

        private readonly List<CustomerSeat> _seats = new List<CustomerSeat>();
        private readonly List<TableOrderPoint> _tables = new List<TableOrderPoint>();
        private readonly List<Transform> _tableAnchors = new List<Transform>();
        private readonly List<Transform> _seatAnchors = new List<Transform>();
        private readonly Dictionary<int, GameObject> _customerVisuals = new Dictionary<int, GameObject>();
        private UnityGameClock _clock;
        private EventBus _events;
        private IIdGenerator _ids;
        private GameObject _runtime;
        private int _nextCustomer = FirstVehicleCustomerId;
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
        /// <summary>TABLE_01 (kept for the earlier two-table tests).</summary>
        public TableOrderPoint DrinkTable => _tables.Count > 0 ? _tables[0] : null;
        /// <summary>TABLE_02 (kept for the earlier two-table tests).</summary>
        public TableOrderPoint CakeTable => _tables.Count > 1 ? _tables[1] : null;
        /// <summary>Index i is TABLE_{i+1}: TableId i+1, interactable id 31+i.</summary>
        public IReadOnlyList<TableOrderPoint> Tables => _tables;
        /// <summary>Runtime table anchor roots named TABLE_01..TABLE_10; each has a Seat child.</summary>
        public IReadOnlyList<Transform> TableAnchors => _tableAnchors;
        public VehicleOrderPoint Vehicle { get; private set; }
        public CustomerDirector Customers { get; private set; }
        /// <summary>All Lobby customer points: the 10 tables followed by the takeaway vehicle.</summary>
        public IReadOnlyList<OrderPoint> CustomerPoints
        {
            get
            {
                var points = new List<OrderPoint>(_tables);
                if (Vehicle != null) { points.Add(Vehicle); }
                return points;
            }
        }

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
            if (_additionalEnvironmentPrefabs != null)
            {
                foreach (GameObject module in _additionalEnvironmentPrefabs)
                {
                    if (module == null) { continue; }
                    GameObject instance = Instantiate(module, Environment.transform, false);
                    instance.name = module.name;
                }
            }
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

            var mixed = new[] { new ItemRequest(_drinkDefinition.Id, 1), new ItemRequest(_cakeRecipes[0].ItemDefinition.Id, 1) };
            for (int number = 1; number <= TableCount; number++) { _tables.Add(PlaceTable(number)); }
            Vehicle = PlaceVehicle(VehicleAnchor, 22, new VehicleId(1));
            // The takeaway vehicle keeps its own always-present customer; tables are driven by the customer director.
            _seats.Add(new CustomerSeat(Vehicle, mixed));
            // Dynamic dine-in customers replace the environment's static seated placeholders.
            foreach (Transform child in Environment.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "DineInCustomer" || child.name == "CakeDineInCustomer") { child.gameObject.SetActive(false); }
            }
            var customerSeats = new List<ICustomerSeat>(TableCount);
            for (int i = 0; i < _tables.Count; i++) { customerSeats.Add(new OrderPointSeat(_tables[i], i + 1)); }
            Customers = new CustomerDirector(customerSeats, _customerSettings, _drinkDefinition.Id, _cakeRecipes[0].ItemDefinition.Id, _customerSeed);
            Customers.CustomerSeated += OnCustomerSeated;
            Customers.CustomerLeft += OnCustomerLeft;
            Customers.CustomerRequestFailed += OnCustomerRequestFailed;

            Transform spawn = RequireAnchor(PlayerSpawnAnchor);
            Teleport(spawn.position, spawn.rotation);
            _animators = _runtime.GetComponentsInChildren<Animator>(true);
            _runtime.SetActive(true);
            foreach (CustomerSeat seat in _seats) { RequestService(seat); }
            // Open with one dine-in customer; further arrivals follow the director's pacing and max-active limit.
            Customers.SpawnNow();
            IsInitialized = true;
        }

        private TableOrderPoint PlaceTable(int number)
        {
            string name = TableAnchorName(number);
            // Prefer the art's TABLE_xx anchor; otherwise the two original environment tables, then the fallback layout.
            Transform source = FindAnchor(name);
            if (source == null && number == 1) { source = FindAnchor(TableAnchor); }
            if (source == null && number == 2) { source = FindAnchor(CakeTableAnchor); }
            bool environmentShowsFurniture = false;
            if (source != null)
            {
                foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(false))
                {
                    if (renderer.enabled) { environmentShowsFurniture = true; break; }
                }
            }
            var root = new GameObject(name).transform;
            root.SetParent(_runtime.transform, false);
            if (source != null) { root.SetPositionAndRotation(source.position, source.rotation); }
            else
            {
                int fallback = number - 3;
                if (_fallbackTablePositions == null || fallback < 0 || fallback >= _fallbackTablePositions.Length)
                { throw new InvalidOperationException("No anchor or fallback position for " + name + "."); }
                root.SetPositionAndRotation(Environment.transform.TransformPoint(_fallbackTablePositions[fallback]), Environment.transform.rotation);
            }
            Transform artSeat = source != null ? source.Find(SeatAnchor) : null;
            var seat = new GameObject(SeatAnchor).transform;
            seat.SetParent(root, false);
            if (artSeat != null) { seat.SetPositionAndRotation(artSeat.position, artSeat.rotation); }
            else
            {
                seat.localPosition = _defaultSeatOffset;
                Vector3 facing = new Vector3(-_defaultSeatOffset.x, 0f, -_defaultSeatOffset.z);
                seat.localRotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing.normalized) : Quaternion.identity;
            }

            GameObject table = Instantiate(_tablePointPrefab, root, false);
            table.name = _tablePointPrefab.name + "_" + name;
            // Where the environment already draws furniture, keep only the gameplay seam; fallback tables stay visible.
            if (environmentShowsFurniture) { foreach (Renderer renderer in table.GetComponentsInChildren<Renderer>(true)) { renderer.enabled = false; } }
            TableOrderPoint point = table.GetComponent<TableOrderPoint>();
            point.Initialize(FirstTableInteractableId + number - 1, table.transform.Find("InteractionPoint"), Orders, _lobby, new TableId(number));
            _tableAnchors.Add(root);
            _seatAnchors.Add(seat);
            return point;
        }

        private void OnCustomerSeated(CustomerSeatedEvent seated)
        {
            // Presentation only: a faulty visual must never abort composition or the director's tick (§6.4 spirit).
            try { ShowCustomer(seated); }
            catch (Exception error) { UnityEngine.Debug.LogException(error, this); }
        }

        private void ShowCustomer(CustomerSeatedEvent seated)
        {
            if (seated.SeatIndex < 0 || seated.SeatIndex >= _seatAnchors.Count) { return; }
            OnCustomerLeft(seated.SeatIndex);
            Transform seat = _seatAnchors[seated.SeatIndex];
            GameObject visual;
            if (_customerVisualPrefab != null)
            {
                visual = Instantiate(_customerVisualPrefab, seat, false);
                // Contract: customer visuals never block movement or the interaction ray.
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) { collider.enabled = false; }
            }
            else
            {
                // Development placeholder only (DEC-010): no collider so it never blocks movement or the interaction ray.
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(visual.GetComponent<Collider>());
                visual.transform.SetParent(seat, false);
                visual.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                visual.transform.localScale = new Vector3(0.38f, 0.6f, 0.38f);
            }
            visual.name = "Customer_" + TableAnchorName(seated.SeatIndex + 1);
            _customerVisuals[seated.SeatIndex] = visual;
        }

        private void OnCustomerLeft(int seatIndex)
        {
            try
            {
                if (_customerVisuals.TryGetValue(seatIndex, out GameObject visual))
                {
                    _customerVisuals.Remove(seatIndex);
                    if (visual != null) { Destroy(visual); }
                }
            }
            catch (Exception error) { UnityEngine.Debug.LogException(error, this); }
        }

        private void OnCustomerRequestFailed(CustomerRequestFailedEvent failed)
        {
            string table = failed.SeatIndex >= 0 && failed.SeatIndex < _tables.Count ? _tables[failed.SeatIndex].name : "seat " + failed.SeatIndex;
            UnityEngine.Debug.LogWarning("Customer could not be seated at " + table + ": " + failed.ReasonKey, this);
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

        private Transform FindAnchor(string anchorName)
        {
            Transform found = null;
            int count = 0;
            foreach (Transform child in Environment.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != anchorName) { continue; }
                count++;
                // A direct child of the environment root wins, as in RequireAnchor.
                if (found == null || (child.parent == Environment.transform && found.parent != Environment.transform)) { found = child; }
            }
            if (count > 1)
            {
                // Environment + modular prefabs must name each anchor once (TABLE_ANCHOR_CONTRACT).
                UnityEngine.Debug.LogError("Duplicate environment anchor '" + anchorName + "' (" + count + "); using " + found.parent.name + "/" + found.name + ".", this);
            }
            return found;
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
            // A load hitch must not seat several customers at once; arrivals follow normal frame pacing.
            Customers.Tick(Math.Min(_clock.DeltaTime, MaxCustomerTickSeconds));
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
            if (Customers != null)
            {
                Customers.CustomerSeated -= OnCustomerSeated;
                Customers.CustomerLeft -= OnCustomerLeft;
                Customers.CustomerRequestFailed -= OnCustomerRequestFailed;
            }
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
