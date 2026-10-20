using System;
using System.Collections.Generic;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.App.People;
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
using TramChanh.UI.Menu;
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
        [Tooltip("Plastic stool the red tea rack stands on behind the counter. Optional.")]
        [SerializeField] private GameObject _teaRackStoolPrefab;
        [SerializeField] private GameObject _vehiclePointPrefab;
        [Tooltip("Cake station origin in stall-local space; aligns the station grill with the stall Grill anchor.")]
        [SerializeField] private Vector3 _cakeStationLocalPosition = new Vector3(-0.51f, 1f, -0.08f);
        [SerializeField, Tbd("DEC-010", "Customer pacing is a development placeholder.")] private float _customerReturnSeconds = 4f;
        [Tooltip("Dine-in customer arrivals for TABLE_01..TABLE_10 (max active customers, pacing, menu weights).")]
        [SerializeField] private CustomerDirectorSettings _customerSettings = new CustomerDirectorSettings();
        [SerializeField] private int _customerSeed = 1;
        [Header("Day cycle")]
        [Tooltip("Real seconds in one game day (1440 = one game hour per minute).")]
        [SerializeField] private float _secondsPerGameDay = (float)DayCycle.DefaultSecondsPerGameDay;
        [Tooltip("Game hour at which the scene starts. 18 = the stall is open at once.")]
        [SerializeField, Range(0f, 24f)] private float _startHour = 18f;
        [Tooltip("How many times faster the game clock runs while the stall is closed (skips the night after 24:00).")]
        [SerializeField, Min(1f)] private float _closedTimeScale = 18f;
        [Tooltip("Tilt the sun and scale light and ambient with the game hour. Off keeps the lighting authored in the scene.")]
        [SerializeField] private bool _driveDaylight = true;
        [Tooltip("Optional seated customer visual (Antigravity). Empty: a primitive placeholder is used.")]
        [SerializeField] private GameObject _customerVisualPrefab;
        [Tooltip("Simulated people (NPC_AND_MAP_EXPANSION.md): customers walk to their table and away; pedestrians and motorbikes fill the street.")]
        [SerializeField] private bool _simulatePeople = true;
        [SerializeField] private PeopleSettings _peopleSettings = new PeopleSettings();
        [Tooltip("Antigravity PF_Person_xx prefabs (visual only). Empty: development capsules.")]
        [SerializeField] private GameObject[] _personPrefabs = Array.Empty<GameObject>();
        [Tooltip("Antigravity PF_Rider_xx prefabs (visual only). Empty: development boxes.")]
        [SerializeField] private GameObject[] _bikePrefabs = Array.Empty<GameObject>();
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
        private bool _peoplePaused;
        private DayClock _dayClock;
        private int _shownClockMinute = -1;
        private bool _shownClockOpen;
        private Light _sun;
        private float _sunBaseIntensity;
        private float _ambientBaseIntensity;
        private Vector3 _sunBaseEuler;
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
        /// <summary>Current game hour in [0, 24).</summary>
        public double HourOfDay => _dayClock == null ? _startHour : _dayClock.HourOfDay;
        /// <summary>True during the stall opening hours (18:00 to 24:00); the clock skips ahead while it is closed.</summary>
        public bool IsStallOpen => _dayClock == null || _dayClock.IsOpen;
        /// <summary>Simulated people layer, or null when _simulatePeople is off.</summary>
        public PeopleController People { get; private set; }
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
            // Drink flavours (MenuBook) are order labels of kind Drink; they have no ItemDefinition asset of their own.
            var kinds = new List<KeyValuePair<string, ItemKind>>();
            foreach (ItemDefinition definition in definitions) { kinds.Add(new KeyValuePair<string, ItemKind>(definition.Id, definition.Kind)); }
            foreach (string flavourId in MenuBook.ExtraIds()) { kinds.Add(new KeyValuePair<string, ItemKind>(flavourId, ItemKind.Drink)); }
            var service = new OrderService(_clock, _events, _ids, new ContentDatabase(kinds), 1, 1, error => UnityEngine.Debug.LogException(error));
            Orders = service;
            var queue = new StallTicketQueue(service);
            Tickets = queue;
            Shelf = new ReadyShelf(service, queue, 1, 1);
            var context = new InteractionContext(new ActorRef(1), ActorRole.Lobby | ActorRole.Stall, new HeldItemSlot(_events), _clock, _events);
            _input.Initialize(_clock);
            _player.Initialize(_input, _clock, _balance.MoveSpeed, _balance.LookSensitivity, _balance.Gravity, _balance.PitchLimit);
            _interactor.Initialize(_player.ViewCamera, _input, context, _balance.ReachDistance);
            _heldView.Initialize(context.Hands, _events);
            PlayerAvatar avatar = _player.GetComponent<PlayerAvatar>();
            if (avatar != null)
            {
                avatar.Initialize(_input, _clock, _events, context.Hands);
                _interactor.SetAimOrigin(avatar.Eye);
            }
            _prompt.Initialize(_events, _interactor, Orders);
            _prompt.EmergencyRestockRequested += OnEmergencyRestock;
            _prompt.SetPatienceProvider(id => _patience.Has(id) ? _patience.LeftFraction(id, _patienceNow) : -1d);
            _prompt.SetCustomerIconProvider(id => _patience.TryGetKind(id, out CustomerKind kind) ? UiIcons.NameFor(kind) : null);
            _settings = new GameSettings();
            _settings.Load(new PlayerPrefsSettingsStore());
            ApplySettings();
            _prompt.AttachPauseMenu(_input, _settings, () => { ApplySettings(); _settings.Save(new PlayerPrefsSettingsStore()); }, QuitGame);
            _progress = new GameProgress();
            _progress.Load(new PlayerPrefsSettingsStore());
            // Tests start from the day-one stock so an empty saved stock on a developer machine cannot fail their orders.
            if (SuppressTitleScreen) { _stock.FillStart(); } else { _stock.Load(new PlayerPrefsSettingsStore()); }
            if (!SuppressTitleScreen)
            {
                _reputation.Load(new PlayerPrefsSettingsStore());
                _upgrades.Load(new PlayerPrefsSettingsStore());
                _difficulty = Difficulties.Load(new PlayerPrefsSettingsStore());
            }
            BeginNight();
            AnnounceStars();
            SetUpSound();
            _orderSubscription = _events.Subscribe<OrderStatusChanged>(OnOrderStatus);
            if (_showTitleScreen && !SuppressTitleScreen)
            {
                _input.ModalOpen = true;
                _input.SetCaptured(false);
                _prompt.ShowTitle(() => { _input.ModalOpen = false; _input.SetCaptured(true); },
                    "Ngày " + _progress.Day + "  |  Tổng cộng: " + _progress.TotalServed + " đơn");
            }
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

            SetUpStreetAnimals();
            foreach (TeaRackController rack in DrinkStation.GetComponentsInChildren<TeaRackController>(true))
            {
                rack.Initialize(Tickets, _ids, _events, Shelf);
                if (_teaRackStoolPrefab != null)
                {
                    // The rack's origin is its base; the stool stands on the floor right under it.
                    Vector3 under = rack.transform.position;
                    GameObject stool = Instantiate(_teaRackStoolPrefab, new Vector3(under.x, Environment.transform.position.y, under.z), rack.transform.rotation, _runtime.transform);
                    stool.name = "TeaRackStool";
                    ArrangeCrateRow(rack.transform);
                }
            }
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
            CustomerDirectorSettings settings = _customerSettings ?? new CustomerDirectorSettings();
            if (_simulatePeople)
            {
                var people = new GameObject("People");
                people.transform.SetParent(_runtime.transform, false);
                People = people.AddComponent<PeopleController>();
                People.Prepare(Environment.transform, _tableAnchors, _seatAnchors, _peopleSettings, _personPrefabs, _bikePrefabs, _customerSeed);
                // Customers walk: the order is requested when they reach their seat (People layer reports arrival).
                // Safety timeouts must outlast the longest real walk, or customers would sit or vanish mid-street.
                float walkBudget = People.LongestCustomerWalkSeconds * 1.5f + 5f;
                settings = new CustomerDirectorSettings(settings.MaxActiveCustomers, settings.ArrivalIntervalSeconds, settings.TableClearSeconds,
                    settings.DrinkWeight, settings.CakeWeight, settings.MixedWeight, true, settings.EatSeconds,
                    Mathf.Max(settings.ArrivalTimeoutSeconds, walkBudget), Mathf.Max(settings.DepartureTimeoutSeconds, walkBudget));
            }
            Customers = new CustomerDirector(customerSeats, settings, _drinkDefinition.Id, _cakeRecipes[0].ItemDefinition.Id, _customerSeed);
            Customers.CustomerRequestFailed += OnCustomerRequestFailed;
            if (People != null) { People.Player = _player.transform; People.Attach(Customers); }
            else
            {
                Customers.CustomerSeated += OnCustomerSeated;
                Customers.CustomerLeft += OnCustomerLeft;
            }

            _dayClock = new DayClock(_secondsPerGameDay, _startHour, OpeningHours.Stall, _closedTimeScale);
            CaptureSun();
            Transform spawn = RequireAnchor(PlayerSpawnAnchor);
            Teleport(spawn.position, spawn.rotation);
            _animators = _runtime.GetComponentsInChildren<Animator>(true);
            _runtime.SetActive(true);
            // The takeaway customer arrives by motorbike (see UpdateVehicleSeat); only walking-in seats are served immediately.
            foreach (CustomerSeat seat in _seats) { if (!UsesVehicleArrival(seat)) { RequestService(seat); } }
            // Open with one dine-in customer; further arrivals follow the director's pacing and max-active limit.
            Customers.SpawnNow();
            IsInitialized = true;
        }

        /// <summary>Distance from a TableApproach_xx anchor to its table, along the anchor's forward axis (art contract §5).</summary>
        private const float TableFromApproachMeters = 0.8f;

        private TableOrderPoint PlaceTable(int number)
        {
            string name = TableAnchorName(number);
            // Prefer the art's TABLE_xx anchor; otherwise the two original environment tables, then the fallback layout.
            Transform source = FindAnchor(name);
            if (source == null && number == 1) { source = FindAnchor(TableAnchor); }
            if (source == null && number == 2) { source = FindAnchor(CakeTableAnchor); }
            // Extended street: no TABLE_xx anchor, but each table has a TableApproach_xx on the walkway facing it.
            // The table stands TableFromApproachMeters ahead of that approach, along its forward axis.
            Transform approach = source == null ? FindAnchor(PeopleLayout.TableApproachPrefix + number.ToString("00")) : null;
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
            else if (approach != null)
            {
                root.SetPositionAndRotation(approach.TransformPoint(new Vector3(0f, 0f, TableFromApproachMeters)), approach.rotation);
            }
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

        private bool UsesVehicleArrival(CustomerSeat seat) => People != null && ReferenceEquals(seat.Point, Vehicle);

        // Rider pulls in off the road and parks at the kerb; the order opens once it has stopped, and it rides off when the order ends.
        private void UpdateVehicleSeat(CustomerSeat seat, bool open)
        {
            if (seat.Point.ActiveOrder.IsValid) { return; }
            if (People.VehicleParked) { People.SendVehicleAway(); return; }
            if (People.VehicleBusy || !open) { seat.WaitingSeconds = 0f; return; }
            seat.WaitingSeconds += (float)_clock.DeltaTime;
            if (seat.WaitingSeconds < _customerReturnSeconds) { return; }
            seat.WaitingSeconds = 0f;
            // The rider stops where the order point's collider is, so aiming at the rider is what opens the order.
            Collider zone = Vehicle.GetComponentInChildren<Collider>(true);
            Vector3 stop = zone != null ? new Vector3(zone.bounds.center.x, Vehicle.transform.position.y, zone.bounds.center.z) : Vehicle.transform.position;
            if (!People.BringVehicle(stop, () => RequestService(seat))) { RequestService(seat); }
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
            if (People != null && _peoplePaused != _clock.IsPaused) { _peoplePaused = _clock.IsPaused; People.SetPaused(_peoplePaused); }
            if (!IsInitialized || _clock.IsPaused) { return; }
            _dayClock.Advance(_clock.DeltaTime);
            double hour = _dayClock.HourOfDay;
            bool open = _dayClock.IsOpen;
            int clockMinute = GameClockText.MinuteOfDay(hour);
            if (_prompt != null && (clockMinute != _shownClockMinute || open != _shownClockOpen || _progress.Money != _shownMoney || StockHash() != _shownStockHash || StatusKey() != _shownStatusKey))
            {
                _shownClockMinute = clockMinute;
                _shownClockOpen = open;
                _shownMoney = _progress.Money;
                _shownStockHash = StockHash();
                _shownStatusKey = StatusKey();
                _prompt.SetStatus("Ngày " + _progress.Day + "  " + GameClockText.Format(hour, open)
                    + "  |  " + PriceList.Format(_progress.Money) + "\n" + StockLine() + "\n" + NightLine());
            }
            foreach (StreetAnimalWander animal in _animals) { if (animal != null) { animal.Tick((float)_clock.DeltaTime); } }
            Customers.DrinkChoices = _unlockedDrinks;
            Customers.ArrivalsEnabled = open && !NightPlan.ArrivalsBlocked(_night, hour);
            Customers.ArrivalIntervalScale = Math.Min(4d, Math.Max(0.2d,
                ArrivalPlan.IntervalScale(hour, _progress.Day, _failureStreak) * _reputation.ArrivalFactor
                * NightPlan.ArrivalScaleAt(_night, hour) * Difficulties.Profile(_difficulty).ArrivalIntervalScale));
            if (open) { TickPatience(); }
            if (_wasOpen && !open) { _wasOpen = false; EndOfDay(); return; }
            _wasOpen = open;
            if (_driveDaylight) { ApplyNightLook(hour); ApplyDaylight(DayCycle.LightingAt(hour)); }
            // A load hitch must not seat several customers at once; arrivals follow normal frame pacing.
            double customerDelta = Math.Min(_clock.DeltaTime, MaxCustomerTickSeconds);
            Customers.Tick(customerDelta);
            if (People != null) { People.Tick(customerDelta); }
            // Completed orders free their point; a new customer arrives after the provisional pause.
            foreach (CustomerSeat seat in _seats)
            {
                if (UsesVehicleArrival(seat)) { UpdateVehicleSeat(seat, open); continue; }
                if (!open) { seat.WaitingSeconds = 0f; continue; }
                if (seat.Point.ActiveOrder.IsValid) { continue; }
                seat.WaitingSeconds += (float)_clock.DeltaTime;
                if (seat.WaitingSeconds >= _customerReturnSeconds) { RequestService(seat); }
            }
        }

        private void CaptureSun()
        {
            _ambientBaseIntensity = RenderSettings.ambientIntensity;
            _lamps.Clear();
            foreach (Light candidate in FindObjectsByType<Light>())
            {
                if (candidate.type != LightType.Directional)
                {
                    _lamps.Add(new KeyValuePair<Light, float>(candidate, candidate.intensity));
                    continue;
                }
                if (_sun != null) { continue; }
                _sun = candidate;
                _sunBaseIntensity = candidate.intensity;
                _sunBaseEuler = candidate.transform.eulerAngles;
            }
        }

        private readonly List<KeyValuePair<Light, float>> _lamps = new List<KeyValuePair<Light, float>>();
        private double _lookSun = 1d, _lookAmbient = 1d, _lookLamps = 1d;

        /// <summary>Tonight's event dims or brightens the light (rain, power cut, festival), moving smoothly towards the target.</summary>
        private void ApplyNightLook(double hour)
        {
            NightLookFactors target = NightLook.For(_night, hour);
            double step = Math.Max(0.0001d, _clock.DeltaTime) * 0.6d;
            _lookSun = NightLook.Approach(_lookSun, target.Sun, step);
            _lookAmbient = NightLook.Approach(_lookAmbient, target.Ambient, step);
            double lamps = NightLook.Approach(_lookLamps, target.Lamps, step * 2d);
            if (Math.Abs(lamps - _lookLamps) > 1e-6)
            {
                _lookLamps = lamps;
                foreach (KeyValuePair<Light, float> lamp in _lamps)
                {
                    if (lamp.Key != null) { lamp.Key.intensity = lamp.Value * (float)_lookLamps; }
                }
            }
        }

        private void ApplyDaylight(DayLighting lighting)
        {
            RenderSettings.ambientIntensity = _ambientBaseIntensity * Mathf.Lerp(0.35f, 1f, (float)lighting.AmbientFactor) * (float)_lookAmbient;
            if (_sun == null) { return; }
            // Moonlight keeps a floor so the street never goes fully black; the sun never points up from below.
            _sun.intensity = _sunBaseIntensity * Mathf.Lerp(0.2f, 1f, (float)lighting.LightIntensity) * (float)_lookSun;
            float pitch = Mathf.Clamp((float)lighting.SunAngleDegrees, 15f, 90f);
            _sun.transform.rotation = Quaternion.Euler(pitch, _sunBaseEuler.y, 0f);
        }

        private GameSettings _settings;
        private readonly List<StreetAnimalWander> _animals = new List<StreetAnimalWander>();
        private GameProgress _progress;
        private readonly PriceList _prices = new PriceList();
        private readonly CustomerKinds _customerKinds = CustomerKinds.Default;
        private readonly PatienceBook _patience = new PatienceBook();
        private readonly List<OrderId> _expired = new List<OrderId>();
        private readonly System.Random _kindRandom = new System.Random(20260101);
        private readonly StockBook _stock = new StockBook();
        private readonly Supplier _supplier = new Supplier();
        private readonly Reputation _reputation = new Reputation();
        private readonly UpgradeBook _upgrades = new UpgradeBook();
        private Difficulty _difficulty = Difficulty.Normal;
        private NightEvent _night = NightEvent.Clear;
        private DailyGoal _goal;
        private IReadOnlyList<string> _unlockedDrinks;
        private readonly HelperWorker _helper = new HelperWorker();
        private readonly Dictionary<OrderId, string> _noStockWhy = new Dictionary<OrderId, string>();
        private bool _goalAnnounced;
        private double _nextEmergencyAt;
        private int _announcedStars = -1;
        private int _shownStatusKey = -1;
        private readonly List<OrderId> _noStock = new List<OrderId>();
        private readonly Dictionary<Ingredient, int> _need = new Dictionary<Ingredient, int>();
        private int _shownStockHash = -1;
        private double _patienceNow;
        private int _failureStreak;
        private int _shownMoney = -1;
        private IDisposable _orderSubscription;
        private bool _wasOpen = true;
        private AudioSource _sfx;
        private AudioClip _readyClip;
        private AudioClip _servedClip;
        private AudioClip _failedClip;

        /// <summary>Tests set this before loading the scene so the game starts running instead of waiting on the title screen.</summary>
        public static bool SuppressTitleScreen { get; set; }
        [SerializeField] private bool _showTitleScreen;

        /// <summary>Spacing between neighbouring crates in the row behind the counter, metres.</summary>
        private const float CrateRowSpacing = 0.6f;

        /// <summary>
        /// Lines the street's decorative tea crates up beside the working tea rack, each on its own plastic stool,
        /// so the stall shows one tidy row of crates behind the counter.
        /// </summary>
        private void ArrangeCrateRow(Transform rack)
        {
            var crates = new List<Transform>();
            foreach (Transform t in Environment.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("PF_PlasticCrate_Street_")) { crates.Add(t); }
            }
            crates.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            Vector3 origin = rack.position;
            for (int i = 0; i < crates.Count; i++)
            {
                Vector3 spot = origin + Environment.transform.right * (CrateRowSpacing * (i + 1));
                crates[i].SetPositionAndRotation(spot, rack.rotation);
                var stool = Instantiate(_teaRackStoolPrefab, new Vector3(spot.x, Environment.transform.position.y, spot.z), rack.rotation, _runtime.transform);
                stool.name = "CrateStool_" + (i + 1);
            }
        }

        /// <summary>Street dogs, cats and chickens (instances named PF_Street_Dog/Cat/Chicken...) wander near where they were placed.</summary>
        private void SetUpStreetAnimals()
        {
            int seed = 11;
            foreach (Transform t in Environment.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                float speed;
                if (n.StartsWith("PF_Street_Dog")) { speed = 0.5f; }
                else if (n.StartsWith("PF_Street_Cat")) { speed = 0.4f; }
                else if (n.StartsWith("PF_Street_Chicken")) { speed = 0.35f; }
                else { continue; }
                if (t.GetComponent<StreetAnimalWander>() != null) { continue; }
                var wander = t.gameObject.AddComponent<StreetAnimalWander>();
                wander.Configure(1.0f, speed, seed++);
                _animals.Add(wander);
            }
        }

        private void SetUpSound()
        {
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;
            _readyClip = Resources.Load<AudioClip>("Audio/sfx_ready");
            _servedClip = Resources.Load<AudioClip>("Audio/sfx_served");
            _failedClip = Resources.Load<AudioClip>("Audio/sfx_failed");
        }

        private void OnOrderStatus(OrderStatusChanged change)
        {
            AudioClip clip = null;
            if (change.Status != OrderStatus.WaitingForLobby) { _helper.Forget(change.OrderId); }
            TrackPatience(change);
            TrackStock(change);
            switch (change.Status)
            {
                case OrderStatus.Ready: clip = _readyClip; break;
                case OrderStatus.Completed: CompleteOrder(change.OrderId); clip = _servedClip; break;
                case OrderStatus.Failed: FailOrder(change.OrderId); clip = _failedClip; break;
            }
            if (clip != null && _sfx != null) { _sfx.PlayOneShot(clip); }
        }

        private void TrackPatience(OrderStatusChanged change)
        {
            if (change.Status == OrderStatus.WaitingForLobby && !_patience.Has(change.OrderId))
            {
                // The food critic only visits a stall with at least three stars.
                CustomerProfile profile = _customerKinds.Pick(_kindRandom.NextDouble(), _reputation.Stars >= 3);
                _patience.Add(change.OrderId, profile.Kind, _patienceNow, profile.PatienceSeconds * _upgrades.PatienceMultiplier * Difficulties.Profile(_difficulty).PatienceScale);
                if (_upgrades.HasHelper) { _helper.Observe(change.OrderId, _patienceNow); }
                if (profile.Kind == CustomerKind.Critic) { _prompt?.ShowNotice("Nhà phê bình vừa đến! Làm thật chuẩn và thật nhanh.", true, 5f); }
            }
            else if (change.Status == OrderStatus.PickedUpByLobby || change.Status == OrderStatus.Delivered)
            {
                _patience.Freeze(change.OrderId, _patienceNow);
            }
        }

        /// <summary>
        /// Takes the ingredients when an order goes to the stall. Short of anything: nothing is taken and the customer
        /// leaves (queued, because an order must not be failed from inside its own status event).
        /// </summary>
        private void TrackStock(OrderStatusChanged change)
        {
            if (change.Status != OrderStatus.SentToStall) { return; }
            IReadOnlyOrder order = Orders?.Get(change.OrderId);
            if (order == null) { return; }
            _need.Clear();
            foreach (IReadOnlyOrderItem item in order.Items) { IngredientUse.Add(item.Kind, _need, NightPlan.IceUsePerDrink(_night)); }
            if (!_stock.TryTake(_need))
            {
                Ingredient? shortOf = _stock.FirstShort(_need);
                _noStockWhy[change.OrderId] = shortOf.HasValue ? IngredientUse.DisplayName(shortOf.Value) : "nguyên liệu";
                _noStock.Add(change.OrderId);
            }
        }

        private void TickPatience()
        {
            if (_noStock.Count > 0)
            {
                var pending = new List<OrderId>(_noStock);
                _noStock.Clear();
                foreach (OrderId id in pending)
                {
                    _noStockWhy.TryGetValue(id, out string why);
                    _noStockWhy.Remove(id);
                    _prompt?.ShowNotice("Hết " + (why ?? "nguyên liệu") + "! Khách bỏ đi. Mua thêm khi đóng cửa.", false, 5f);
                    Orders.Fail(id, FailureReason.CustomerLeft);
                }
            }
            _patienceNow += _clock.DeltaTime;
            if (_upgrades.HasHelper && _helper.TryNext(_patienceNow, out OrderId helperOrder)) { HelperTakeOrder(helperOrder); }
            _expired.Clear();
            _patience.CollectExpired(_patienceNow, _expired);
            foreach (OrderId id in _expired) { Orders.Fail(id, FailureReason.CustomerLeft); }
        }

        /// <summary>R: the corner shop. Only while the stall is open, not paused, and at most once every 30 seconds of game time.</summary>
        private void OnEmergencyRestock()
        {
            if (_dayClock == null || !_dayClock.IsOpen || _input.ModalOpen || _clock.IsPaused) { return; }
            if (_patienceNow < _nextEmergencyAt)
            {
                _prompt?.ShowNotice("Cửa hàng góc phố: đợi thêm " + Mathf.CeilToInt((float)(_nextEmergencyAt - _patienceNow)) + " giây.", false, 2.5f);
                return;
            }
            if (EmergencyShop.TryBuy(_stock, _progress, _supplier, out Ingredient bought, out int cost))
            {
                _nextEmergencyAt = _patienceNow + EmergencyShop.CooldownSeconds;
                SaveAll();
                _prompt?.ShowNotice("Mua gấp ở cửa hàng góc phố: +" + EmergencyShop.PackUnits + " " + IngredientUse.DisplayName(bought) + " (" + PriceList.Format(cost) + ").", true, 4f);
            }
            else
            {
                _prompt?.ShowNotice("Không mua được: thiếu tiền hoặc kho " + IngredientUse.DisplayName(bought) + " đã đầy.", false, 3.5f);
            }
        }

        /// <summary>A delivered order: priced, and judged when the customer is the food critic.</summary>
        private void CompleteOrder(OrderId id)
        {
            Sale sale = PriceOrder(id);
            if (_patience.TryGetKind(id, out CustomerKind kind) && kind == CustomerKind.Critic)
            {
                CriticVerdict verdict = CriticReview.Judge(sale.Tier, _patience.LeftFraction(id, _patienceNow));
                int amount = (int)(Math.Round(sale.Amount * verdict.MoneyMultiplier / PriceList.RoundTo, MidpointRounding.AwayFromZero) * PriceList.RoundTo);
                sale = new Sale(amount, sale.Tier, sale.Quality);
                _reputation.AddPoints(verdict.ReputationPoints);
                _prompt?.ShowNotice(verdict.Text, verdict.IsGood, 6f);
            }
            _progress.RecordSale(sale);
            _failureStreak = 0;
            _patience.Remove(id);
            if (!_goalAnnounced && _goal.ServedTarget > 0 && _progress.ServedToday >= _goal.ServedTarget)
            {
                _goalAnnounced = true;
                _prompt?.ShowNotice("Đạt mục tiêu ngày! Giữ tỉ lệ phục vụ từ " + DailyGoal.MinServedPercent + "% để nhận thưởng.", true, 5f);
            }
        }

        /// <summary>A failed order: counted, and a critic who left without being served leaves a bad review.</summary>
        private void FailOrder(OrderId id)
        {
            if (_patience.TryGetKind(id, out CustomerKind kind) && kind == CustomerKind.Critic)
            {
                CriticVerdict verdict = CriticReview.Walkout();
                _reputation.AddPoints(verdict.ReputationPoints);
                _prompt?.ShowNotice(verdict.Text, false, 5f);
            }
            _progress.RecordFailed();
            _failureStreak++;
            _patience.Remove(id);
        }

        /// <summary>The helper worker takes a waiting order for the player: take it, enter what the customer asked for, send it to the stall.</summary>
        private void HelperTakeOrder(OrderId id)
        {
            IReadOnlyOrder order = Orders?.Get(id);
            if (order == null || order.Status != OrderStatus.WaitingForLobby) { return; }
            var helperActor = new ActorRef(2);
            if (!Orders.BeginTaking(id, helperActor, order.Origin).IsSuccess) { return; }
            if (!Orders.Enter(id, order.RequestedItems).IsSuccess) { return; }
            if (!Orders.SendToStall(id).IsSuccess) { return; }
            _prompt?.ShowNotice("Nhân viên phụ đã nhận một đơn.", true, 2.5f);
        }

        private Sale PriceOrder(OrderId id)
        {
            IReadOnlyOrder order = Orders?.Get(id);
            if (order == null) { return new Sale(0, QualityTier.Poor, 0); }
            var items = new List<KeyValuePair<ItemKind, int>>(order.Items.Count);
            long qualitySum = 0;
            foreach (IReadOnlyOrderItem item in order.Items)
            {
                items.Add(new KeyValuePair<ItemKind, int>(item.Kind, MenuBook.UnitPrice(item.ItemDefinitionId, item.Kind, _prices)));
                qualitySum += item.Quality;
            }
            int quality = items.Count == 0 ? 0 : (int)Math.Round((double)qualitySum / items.Count, MidpointRounding.AwayFromZero);
            float payMultiplier = 1f;
            if (_patience.TryGetKind(id, out CustomerKind kind)) { payMultiplier = _customerKinds.Profile(kind).PayMultiplier; }
            payMultiplier *= 1f + _upgrades.PriceBonusPercent / 100f;
            return _prices.PriceItems(items, quality, _patience.LeftFraction(id, _patienceNow), payMultiplier);
        }

        /// <summary>Third status line: tonight's event, stars and how far the daily goal is.</summary>
        private string NightLine()
        {
            string line = NightPlan.Name(_night) + "  |  " + Reputation.StarText(_reputation.Stars);
            if (_goal.ServedTarget > 0) { line += "  |  Mục tiêu " + _progress.ServedToday + "/" + _goal.ServedTarget; }
            return line;
        }

        private int StatusKey() => (_progress.ServedToday * 31 + _reputation.Stars) * 31 + (int)_night;

        /// <summary>Picks tonight's event and goal for the current day.</summary>
        private void BeginNight()
        {
            _night = SuppressTitleScreen ? NightEvent.Clear : NightPlan.ForDay(_progress.Day, _customerSeed);
            _goal = DailyGoal.ForDay(_progress.Day, _difficulty);
            _goalAnnounced = false;
            _helper.Clear();
            _unlockedDrinks = MenuBook.UnlockedDrinkIds(_drinkDefinition != null ? _drinkDefinition.Id : null, _reputation.Stars);
            ApplyUpgrades();
        }

        private void ApplyUpgrades()
        {
            _stock.SetCapacityBonus(_upgrades.IceCapacityBonus, _upgrades.OtherCapacityBonus);
            _supplier.DiscountPercent = _upgrades.SupplierDiscountPercent;
            _supplier.PriceScale = Difficulties.Profile(_difficulty).SupplierPriceScale;
        }

        private void SaveAll()
        {
            var store = new PlayerPrefsSettingsStore();
            _progress.Save(store);
            _stock.Save(store);
            _reputation.Save(store);
            _upgrades.Save(store);
            Difficulties.Save(store, _difficulty);
        }

        /// <summary>Which extra drinks the stall sells at this star count, and when the next one comes.</summary>
        private static string MenuNote(int stars)
        {
            var have = new List<string>();
            string next = null;
            foreach (MenuEntry entry in MenuBook.Extra)
            {
                if (stars >= entry.RequiredStars) { have.Add(entry.DisplayName); }
                else if (next == null) { next = entry.DisplayName + " (" + entry.RequiredStars + " sao)"; }
            }
            string note = have.Count > 0 ? "\nMón thêm đang bán: " + string.Join(", ", have) : string.Empty;
            if (next != null) { note += "\nMón kế tiếp: " + next; }
            return note;
        }

        private string StockLine()
        {
            var line = new System.Text.StringBuilder("Kho: ");
            foreach (Ingredient ingredient in StockBook.Ingredients)
            {
                if (ingredient != StockBook.Ingredients[0]) { line.Append("  "); }
                line.Append(IngredientUse.DisplayName(ingredient)).Append(' ').Append(_stock.Level(ingredient));
            }
            return line.ToString();
        }

        private int StockHash()
        {
            int hash = 17;
            foreach (Ingredient ingredient in StockBook.Ingredients) { hash = hash * 31 + _stock.Level(ingredient); }
            return hash;
        }

        private void EndOfDay()
        {
            DaySummary summary = _progress.EndDay();
            _stock.Overnight();
            var notes = new System.Text.StringBuilder();
            int points = _reputation.ApplyDay(summary);
            bool goalMet = _goal.IsMet(summary);
            if (goalMet)
            {
                _progress.AddBonus(_goal.BonusMoney);
                points += _reputation.AddPoints(DailyGoal.BonusPoints);
                notes.Append("Đạt mục tiêu! Thưởng ").Append(PriceList.Format(_goal.BonusMoney)).Append('\n');
            }
            else if (_goal.ServedTarget > 0)
            {
                notes.Append("Chưa đạt mục tiêu (").Append(_goal.ServedTarget).Append(" khách).\n");
            }
            if (_night == NightEvent.Inspector)
            {
                NightPlan.Inspect(summary, out int money, out int inspectorPoints);
                int realMoney = _progress.AddBonus(money);
                points += _reputation.AddPoints(inspectorPoints);
                notes.Append(money > 0 ? "Thanh tra hài lòng: +" + PriceList.Format(realMoney) : "Thanh tra phạt: " + PriceList.Format(-realMoney)).Append('\n');
            }
            if (_upgrades.HasHelper)
            {
                int paid = -_progress.AddBonus(-HelperWorker.WagePerNight);
                notes.Append("Lương nhân viên phụ: -").Append(PriceList.Format(paid)).Append('\n');
            }
            notes.Append("Uy tín: ").Append(Reputation.StarText(_reputation.Stars)).Append(" (").Append(points >= 0 ? "+" : "").Append(points).Append(" điểm)");
            SaveAll();
            _input.ModalOpen = true;
            _input.SetCaptured(false);
            _prompt.ShowDaySummary(summary, _progress, () => ShowShop(), notes.ToString(), _reputation.Stars);
        }

        private void ShowShop()
        {
            ApplyUpgrades();
            NightEvent tomorrow = SuppressTitleScreen ? NightEvent.Clear : NightPlan.ForDay(_progress.Day, _customerSeed);
            DailyGoal goal = DailyGoal.ForDay(_progress.Day, _difficulty);
            var context = new SupplierContext
            {
                Stock = _stock,
                Wallet = _progress,
                Supplier = _supplier,
                Upgrades = _upgrades,
                Reputation = _reputation,
                GetDifficulty = () => _difficulty,
                CycleDifficulty = () => { _difficulty = Difficulties.Next(_difficulty); },
                Changed = () => { ApplyUpgrades(); SaveAll(); },
            };
            context.ForecastNight = tomorrow;
            string menuNote = MenuNote(_reputation.Stars);
            context.Forecast = "Ngày " + _progress.Day + ": " + NightPlan.Name(tomorrow) + " - " + NightPlan.Announcement(tomorrow) + "\n" + goal.Describe() + menuNote;
            _prompt.ShowSupplier(context, () =>
            {
                BeginNight();
                SaveAll();
                _shownStatusKey = -1;
                _input.ModalOpen = false;
                _input.SetCaptured(true);
                AnnounceStars();
            });
        }

        /// <summary>At the start of a night: tell the player about a new star, the drinks it opens and the critic at three stars.</summary>
        private void AnnounceStars()
        {
            int stars = _reputation.Stars;
            if (_announcedStars >= 0 && stars > _announcedStars)
            {
                string text = "Lên " + stars + " sao!";
                IReadOnlyList<MenuEntry> opened = MenuBook.UnlockedAt(stars);
                if (opened.Count > 0)
                {
                    var names = new List<string>();
                    foreach (MenuEntry entry in opened) { names.Add(entry.DisplayName); }
                    text += " Mở món: " + string.Join(", ", names) + ".";
                }
                if (stars == 3) { text += " Nhà phê bình có thể ghé quán."; }
                _prompt?.ShowNotice(text, true, 7f);
            }
            _announcedStars = stars;
        }

        private void ApplySettings()
        {
            AudioListener.volume = _settings.MasterVolume;
            _player.LookScale = _settings.LookScale;
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
            _orderSubscription?.Dispose();
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
