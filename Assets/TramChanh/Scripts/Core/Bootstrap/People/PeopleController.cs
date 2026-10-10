using System;
using System.Collections.Generic;
using TramChanh.Core.Provisional;
using TramChanh.Customers;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.App.People
{
    [Serializable]
    public sealed class PeopleSettings
    {
        [SerializeField, Tbd("DEC-010", "Walking pace of customers and pedestrians.")] private float _walkSpeed = 1.3f;
        [SerializeField, Tbd("DEC-010")] private int _maxPedestrians = 12;
        [SerializeField, Tbd("DEC-010")] private float _pedestrianIntervalSeconds = 3f;
        [SerializeField, Tbd("DEC-010")] private int _maxBikes = 6;
        [SerializeField, Tbd("DEC-010")] private float _bikeIntervalSeconds = 2.5f;
        [SerializeField, Tbd("DEC-010")] private float _bikeSpeed = 6f;
        public float WalkSpeed => _walkSpeed;
        public int MaxPedestrians => _maxPedestrians;
        public float PedestrianIntervalSeconds => _pedestrianIntervalSeconds;
        public int MaxBikes => _maxBikes;
        public float BikeIntervalSeconds => _bikeIntervalSeconds;
        public float BikeSpeed => _bikeSpeed;
    }

    /// <summary>
    /// Presentation/flow layer for simulated people (NPC_AND_MAP_EXPANSION.md §3). Customers walk from a street entry to their table,
    /// sit, eat and walk away; the CustomerDirector (Lobby path) stays the only owner of seats and orders. Ambient pedestrians and
    /// motorbikes are pure decoration. Everything advances with the game-clock delta passed to Tick, so pausing the game pauses people.
    /// </summary>
    public sealed class PeopleController : MonoBehaviour
    {
        public const string PedestrianLaneNorth = "SidewalkNorth";
        public const string PedestrianLaneSouth = "SidewalkSouth";
        public const string RoadEast = "RoadEast";
        public const string RoadWest = "RoadWest";

        private readonly Dictionary<int, PersonView> _customers = new Dictionary<int, PersonView>();
        private readonly Dictionary<int, PersonView> _pedestrians = new Dictionary<int, PersonView>();
        private readonly Dictionary<int, PersonView> _bikes = new Dictionary<int, PersonView>();
        private readonly List<PersonView> _stepBuffer = new List<PersonView>();
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly Dictionary<Color, Material> _placeholderMaterials = new Dictionary<Color, Material>();
        private IReadOnlyList<Transform> _seats;
        private CustomerDirector _director;
        private PeopleSettings _settings;
        private GameObject[] _personPrefabs;
        private GameObject[] _bikePrefabs;
        private CrowdScheduler _pedestrianScheduler;
        private CrowdScheduler _bikeScheduler;
        private System.Random _random;
        private int _entryWest = -1, _entryEast = -1;

        public WaypointGraph Graph { get; private set; }
        public bool UsedFallbackLayout { get; private set; }
        public IReadOnlyList<string> LayoutErrors { get; private set; } = Array.Empty<string>();
        public int ActivePedestrians => _pedestrians.Count;
        public int ActiveBikes => _bikes.Count;
        /// <summary>Customer view for a seat index (walking, seated or leaving), or null.</summary>
        public PersonView CustomerAt(int seatIndex) => _customers.TryGetValue(seatIndex, out PersonView view) ? view : null;

        public void Initialize(Transform environmentRoot, IReadOnlyList<Transform> tableAnchors, IReadOnlyList<Transform> seats,
            CustomerDirector director, PeopleSettings settings, GameObject[] personPrefabs, GameObject[] bikePrefabs, int seed)
        {
            _seats = seats ?? throw new ArgumentNullException(nameof(seats));
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _settings = settings ?? new PeopleSettings();
            _personPrefabs = personPrefabs ?? Array.Empty<GameObject>();
            _bikePrefabs = bikePrefabs ?? Array.Empty<GameObject>();
            _random = new System.Random(seed);

            Dictionary<string, Vector3> anchors = PeopleLayout.Collect(environmentRoot, tableAnchors, out bool fallback);
            UsedFallbackLayout = fallback;
            WaypointGraphBuildResult build = WaypointGraphBuilder.Build(anchors);
            Graph = build.Graph;
            LayoutErrors = build.Errors;
            foreach (string error in build.Errors) { Debug.LogWarning("People layout: " + error, this); }
            Graph.TryFind(PeopleLayout.StreetEntryWest, out _entryWest);
            Graph.TryFind(PeopleLayout.StreetEntryEast, out _entryEast);

            _director.CustomerArriving += OnArriving;
            _director.CustomerEating += OnEating;
            _director.CustomerLeaving += OnLeaving;
            _director.CustomerLeft += OnLeft;

            var walkLanes = new List<string>();
            foreach (string lane in new[] { PedestrianLaneSouth, PedestrianLaneNorth }) { if (Graph.TryGetLane(lane, out _, out _)) { walkLanes.Add(lane); } }
            if (walkLanes.Count > 0 && _settings.MaxPedestrians > 0)
            {
                _pedestrianScheduler = new CrowdScheduler(walkLanes, _settings.MaxPedestrians, _settings.PedestrianIntervalSeconds, seed + 17);
                _pedestrianScheduler.Spawned += OnPedestrianSpawned;
                _pedestrianScheduler.Despawned += id => Release(_pedestrians, id);
            }
            var roadLanes = new List<string>();
            foreach (string lane in new[] { RoadEast, RoadWest }) { if (Graph.TryGetLane(lane, out _, out _)) { roadLanes.Add(lane); } }
            if (roadLanes.Count > 0 && _settings.MaxBikes > 0)
            {
                _bikeScheduler = new CrowdScheduler(roadLanes, _settings.MaxBikes, _settings.BikeIntervalSeconds, seed + 31);
                _bikeScheduler.Spawned += OnBikeSpawned;
                _bikeScheduler.Despawned += id => Release(_bikes, id);
            }
        }

        /// <summary>Advance people by the game-clock delta (0 while paused).</summary>
        public void Tick(double seconds)
        {
            if (!(seconds > 0d) || double.IsInfinity(seconds)) { return; }
            _pedestrianScheduler?.Tick(seconds);
            _bikeScheduler?.Tick(seconds);
            StepAll(_customers, seconds);
            StepAll(_pedestrians, seconds);
            StepAll(_bikes, seconds);
        }

        /// <summary>Animators freeze while the game clock is paused.</summary>
        public void SetPaused(bool paused)
        {
            float speed = paused ? 0f : 1f;
            foreach (PersonView view in _customers.Values) { view.SetAnimatorSpeed(speed); }
            foreach (PersonView view in _pedestrians.Values) { view.SetAnimatorSpeed(speed); }
            foreach (PersonView view in _bikes.Values) { view.SetAnimatorSpeed(speed); }
        }

        private void StepAll(Dictionary<int, PersonView> views, double seconds)
        {
            // Copy first: arrival callbacks can add or remove views.
            _stepBuffer.Clear();
            _stepBuffer.AddRange(views.Values);
            foreach (PersonView view in _stepBuffer) { if (view != null && view.IsWalking) { view.Step(seconds); } }
        }

        private void OnArriving(CustomerArrivingEvent arriving)
        {
            Guard(() =>
            {
                int seat = arriving.SeatIndex;
                Release(_customers, seat);
                PersonView view = Acquire(_personPrefabs, "Customer_" + TramChanhMainBootstrap.TableAnchorName(seat + 1), PrimitiveType.Capsule, new Color(0.85f, 0.55f, 0.35f));
                _customers[seat] = view;
                int entry = PickEntry();
                if (!TryRoute(entry, ApproachOf(seat), _path) || seat >= _seats.Count)
                {
                    // No walkable route: sit immediately so the Lobby flow is never blocked by presentation.
                    SitDown(view, seat);
                    _director.NotifyArrived(seat);
                    return;
                }
                _path.Add(_seats[seat].position);
                view.transform.position = _path[0];
                view.Walk(_path, _settings.WalkSpeed, _ =>
                {
                    SitDown(view, seat);
                    _director.NotifyArrived(seat);
                });
            });
        }

        private void OnEating(int seat)
        {
            Guard(() => { if (_customers.TryGetValue(seat, out PersonView view)) { view.SetEating(true); } });
        }

        private void OnLeaving(int seat)
        {
            Guard(() =>
            {
                if (!_customers.TryGetValue(seat, out PersonView view)) { _director.NotifyDeparted(seat); return; }
                view.SetEating(false);
                view.SetSitting(false);
                int exit = PickEntry();
                if (!TryRoute(ApproachOf(seat), exit, _path))
                {
                    _director.NotifyDeparted(seat);
                    return;
                }
                _path.Insert(0, view.transform.position);
                view.Walk(_path, _settings.WalkSpeed, _ => _director.NotifyDeparted(seat));
            });
        }

        private void OnLeft(int seat) { Guard(() => Release(_customers, seat)); }

        private void OnPedestrianSpawned(CrowdSpawn spawn)
        {
            Guard(() =>
            {
                if (!LanePath(spawn.Lane, spawn.Reverse, _path)) { _pedestrianScheduler.NotifyFinished(spawn.AgentId); return; }
                PersonView view = Acquire(_personPrefabs, "Pedestrian_" + spawn.AgentId, PrimitiveType.Capsule, new Color(0.45f, 0.55f, 0.7f));
                _pedestrians[spawn.AgentId] = view;
                view.transform.position = _path[0];
                int id = spawn.AgentId;
                view.Walk(_path, _settings.WalkSpeed * (0.85f + 0.3f * (float)_random.NextDouble()), _ => _pedestrianScheduler.NotifyFinished(id));
            });
        }

        private void OnBikeSpawned(CrowdSpawn spawn)
        {
            Guard(() =>
            {
                // Road lanes are one-way: never reverse them.
                if (!LanePath(spawn.Lane, false, _path)) { _bikeScheduler.NotifyFinished(spawn.AgentId); return; }
                PersonView view = Acquire(_bikePrefabs, "Motorbike_" + spawn.AgentId, PrimitiveType.Cube, new Color(0.2f, 0.2f, 0.25f), new Vector3(0.45f, 0.9f, 1.6f));
                _bikes[spawn.AgentId] = view;
                view.transform.position = _path[0];
                int id = spawn.AgentId;
                view.Walk(_path, _settings.BikeSpeed, _ => _bikeScheduler.NotifyFinished(id));
            });
        }

        private void SitDown(PersonView view, int seat)
        {
            if (seat < _seats.Count) { view.Place(_seats[seat].position, _seats[seat].rotation); }
            view.SetSitting(true);
        }

        private int ApproachOf(int seat)
        {
            return Graph.TryFind(PeopleLayout.TableApproachPrefix + (seat + 1).ToString("00"), out int node) ? node : -1;
        }

        private int PickEntry()
        {
            if (_entryWest < 0) { return _entryEast; }
            if (_entryEast < 0) { return _entryWest; }
            return _random.Next(2) == 0 ? _entryWest : _entryEast;
        }

        private bool TryRoute(int from, int to, List<Vector3> path)
        {
            path.Clear();
            return from >= 0 && to >= 0 && Graph.TryFindPath(from, to, path) && path.Count > 0;
        }

        private bool LanePath(string lane, bool reverse, List<Vector3> path)
        {
            path.Clear();
            if (!Graph.TryGetLane(lane, out IReadOnlyList<int> nodes, out _) || nodes.Count < 2) { return false; }
            for (int i = 0; i < nodes.Count; i++) { path.Add(Graph.PositionOf(nodes[reverse ? nodes.Count - 1 - i : i])); }
            return true;
        }

        private PersonView Acquire(GameObject[] prefabs, string name, PrimitiveType placeholder, Color color, Vector3? scale = null)
        {
            GameObject body;
            if (prefabs.Length > 0) { body = Instantiate(prefabs[_random.Next(prefabs.Length)], transform, false); }
            else
            {
                // Development placeholder only (DEC-010); Antigravity's PF_Person_xx / PF_Rider_xx replace it.
                body = new GameObject();
                body.transform.SetParent(transform, false);
                GameObject shape = GameObject.CreatePrimitive(placeholder);
                Destroy(shape.GetComponent<Collider>());
                shape.transform.SetParent(body.transform, false);
                Vector3 size = scale ?? new Vector3(0.45f, 0.85f, 0.45f);
                shape.transform.localScale = size;
                shape.transform.localPosition = new Vector3(0f, placeholder == PrimitiveType.Cube ? size.y * 0.5f : size.y, 0f);
                shape.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterial(shape.GetComponent<Renderer>(), color);
            }
            body.name = name;
            PersonView view = body.GetComponent<PersonView>();
            if (view == null) { view = body.AddComponent<PersonView>(); }
            view.Prepare();
            return view;
        }

        private Material PlaceholderMaterial(Renderer template, Color color)
        {
            if (!_placeholderMaterials.TryGetValue(color, out Material material))
            {
                material = new Material(template.sharedMaterial) { color = color };
                _placeholderMaterials.Add(color, material);
            }
            return material;
        }

        private void Release(Dictionary<int, PersonView> views, int key)
        {
            if (!views.TryGetValue(key, out PersonView view)) { return; }
            views.Remove(key);
            if (view != null) { Destroy(view.gameObject); }
        }

        private void Guard(Action action)
        {
            try { action(); }
            catch (Exception error) { Debug.LogException(error, this); }
        }

        private void OnDestroy()
        {
            foreach (Material material in _placeholderMaterials.Values) { if (material != null) { Destroy(material); } }
            _placeholderMaterials.Clear();
            if (_director != null)
            {
                _director.CustomerArriving -= OnArriving;
                _director.CustomerEating -= OnEating;
                _director.CustomerLeaving -= OnLeaving;
                _director.CustomerLeft -= OnLeft;
            }
        }
    }
}
