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
        private const float MinSpeed = 0.1f;
        private const float MinInterval = 0.1f;

        [SerializeField, Tbd("DEC-010", "Walking pace of customers and pedestrians.")] private float _walkSpeed = 1.3f;
        [SerializeField, Tbd("DEC-010")] private int _maxPedestrians = 12;
        [SerializeField, Tbd("DEC-010")] private float _pedestrianIntervalSeconds = 3f;
        [SerializeField, Tbd("DEC-010")] private int _maxBikes = 6;
        [SerializeField, Tbd("DEC-010")] private float _bikeIntervalSeconds = 2.5f;
        [SerializeField, Tbd("DEC-010")] private float _bikeSpeed = 6f;

        // Inspector values are clamped so a bad value can never stop the scene from composing.
        public float WalkSpeed => Positive(_walkSpeed, MinSpeed);
        public int MaxPedestrians => Math.Max(0, _maxPedestrians);
        public float PedestrianIntervalSeconds => Positive(_pedestrianIntervalSeconds, MinInterval);
        public int MaxBikes => Math.Max(0, _maxBikes);
        public float BikeIntervalSeconds => Positive(_bikeIntervalSeconds, MinInterval);
        public float BikeSpeed => Positive(_bikeSpeed, MinSpeed);

        private static float Positive(float value, float minimum) => float.IsNaN(value) || float.IsInfinity(value) || value < minimum ? minimum : value;
    }

    /// <summary>
    /// Presentation/flow layer for simulated people (NPC_AND_MAP_EXPANSION.md §3). Customers walk from the nearest street entry to
    /// their table, sit, eat and walk to the nearest exit; the CustomerDirector (Lobby path) stays the only owner of seats and orders.
    /// Ambient pedestrians and motorbikes are pure decoration. Everything advances with the game-clock delta passed to Tick.
    /// Lifecycle: <see cref="Prepare"/> (layout, route timing) before the director exists, then <see cref="Attach"/>.
    /// </summary>
    public sealed class PeopleController : MonoBehaviour
    {
        public const string PedestrianLaneNorth = "SidewalkNorth";
        public const string PedestrianLaneSouth = "SidewalkSouth";
        public const string RoadEast = "RoadEast";
        public const string RoadWest = "RoadWest";

        private readonly Dictionary<int, PersonView> _customers = new Dictionary<int, PersonView>();
        private readonly Dictionary<int, PersonView> _leavers = new Dictionary<int, PersonView>();
        private readonly Dictionary<int, PersonView> _pedestrians = new Dictionary<int, PersonView>();
        private readonly Dictionary<int, PersonView> _bikes = new Dictionary<int, PersonView>();
        private readonly Dictionary<Color, Material> _placeholderMaterials = new Dictionary<Color, Material>();
        private readonly List<PersonView> _stepBuffer = new List<PersonView>();
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<Vector3> _probe = new List<Vector3>();
        private IReadOnlyList<Transform> _seats;
        private CustomerDirector _director;
        private PeopleSettings _settings = new PeopleSettings();
        private GameObject[] _personPrefabs = Array.Empty<GameObject>();
        private GameObject[] _bikePrefabs = Array.Empty<GameObject>();
        private CrowdScheduler _pedestrianScheduler;
        private CrowdScheduler _bikeScheduler;
        private System.Random _random;
        private int _entryWest = -1, _entryEast = -1;
        private int _nextLeaverId;
        private bool _paused;

        public WaypointGraph Graph { get; private set; }
        public PeopleSettings Settings => _settings;
        public bool UsedFallbackLayout { get; private set; }
        public IReadOnlyList<string> LayoutErrors { get; private set; } = Array.Empty<string>();
        /// <summary>Longest one-way customer walk (entry→seat or seat→exit) in seconds at the walk speed; 0 if unknown.</summary>
        public float LongestCustomerWalkSeconds { get; private set; }
        public int ActivePedestrians => _pedestrians.Count;
        public int ActiveBikes => _bikes.Count;
        /// <summary>Customer view for a seat index (walking in, seated or leaving), or null.</summary>
        public PersonView CustomerAt(int seatIndex) => _customers.TryGetValue(seatIndex, out PersonView view) ? view : null;

        /// <summary>Builds the walkway graph and measures customer routes. Safe to call before the director exists.</summary>
        public void Prepare(Transform environmentRoot, IReadOnlyList<Transform> tableAnchors, IReadOnlyList<Transform> seats,
            PeopleSettings settings, GameObject[] personPrefabs, GameObject[] bikePrefabs, int seed)
        {
            _seats = seats ?? throw new ArgumentNullException(nameof(seats));
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

            float longest = 0f;
            for (int seat = 0; seat < _seats.Count; seat++)
            {
                if (_seats[seat] == null) { continue; }
                float length = NearestEndpointRoute(ApproachOf(seat), _probe);
                if (length >= 0f) { longest = Mathf.Max(longest, length + Vector3.Distance(_probe[0], _seats[seat].position)); }
            }
            LongestCustomerWalkSeconds = longest / _settings.WalkSpeed;

            TryCreateAmbient(seed);
        }

        /// <summary>Subscribes to a walking-mode director. Call after <see cref="Prepare"/> and before the first SpawnNow.</summary>
        public void Attach(CustomerDirector director)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _director.CustomerArriving += OnArriving;
            _director.CustomerEating += OnEating;
            _director.CustomerLeaving += OnLeaving;
            _director.CustomerLeft += OnLeft;
        }

        private void TryCreateAmbient(int seed)
        {
            try
            {
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
            catch (Exception error)
            {
                // Ambient life is decoration: never let it stop the scene from composing.
                Debug.LogException(error, this);
                _pedestrianScheduler = null;
                _bikeScheduler = null;
            }
        }

        /// <summary>Advance people by the game-clock delta (0 while paused).</summary>
        public void Tick(double seconds)
        {
            if (!(seconds > 0d) || double.IsInfinity(seconds)) { return; }
            Guard(() => _pedestrianScheduler?.Tick(seconds));
            Guard(() => _bikeScheduler?.Tick(seconds));
            StepAll(_customers, seconds);
            StepAll(_leavers, seconds);
            StepAll(_pedestrians, seconds);
            StepAll(_bikes, seconds);
        }

        /// <summary>Animators freeze while the game clock is paused; views created while paused start frozen.</summary>
        public void SetPaused(bool paused)
        {
            _paused = paused;
            float speed = paused ? 0f : 1f;
            foreach (Dictionary<int, PersonView> views in new[] { _customers, _leavers, _pedestrians, _bikes })
            {
                foreach (PersonView view in views.Values) { if (view != null) { view.SetAnimatorSpeed(speed); } }
            }
        }

        private void StepAll(Dictionary<int, PersonView> views, double seconds)
        {
            // Copy first: arrival callbacks can add or remove views. Each step is isolated so one fault cannot stall the rest.
            _stepBuffer.Clear();
            _stepBuffer.AddRange(views.Values);
            foreach (PersonView view in _stepBuffer)
            {
                if (view == null || !view.IsWalking) { continue; }
                try { view.Step(seconds); }
                catch (Exception error) { Debug.LogException(error, this); }
            }
        }

        private void OnArriving(CustomerArrivingEvent arriving)
        {
            Guard(() =>
            {
                int seat = arriving.SeatIndex;
                Release(_customers, seat);
                PersonView view = Acquire(_personPrefabs, "Customer_" + TramChanhMainBootstrap.TableAnchorName(seat + 1), PrimitiveType.Capsule, new Color(0.85f, 0.55f, 0.35f));
                _customers[seat] = view;
                if (seat >= _seats.Count || _seats[seat] == null || NearestEndpointRoute(ApproachOf(seat), _path) < 0f)
                {
                    // No walkable route: sit immediately so the Lobby flow is never blocked by presentation.
                    SitDown(view, seat);
                    _director.NotifyArrived(seat);
                    return;
                }
                _path.Reverse();                       // entry → approach
                _path.Add(_seats[seat].position);      // approach → seat
                view.transform.position = _path[0];
                view.Walk(_path, _settings.WalkSpeed, _ => Guard(() =>
                {
                    SitDown(view, seat);
                    _director.NotifyArrived(seat);
                }));
            });
        }

        private void OnEating(int seat)
        {
            Guard(() => { if (_customers.TryGetValue(seat, out PersonView view) && view != null) { view.SetEating(true); } });
        }

        private void OnLeaving(int seat)
        {
            Guard(() =>
            {
                if (!_customers.TryGetValue(seat, out PersonView view) || view == null) { _director.NotifyDeparted(seat); return; }
                view.SetEating(false);
                view.SetSitting(false);
                if (NearestEndpointRoute(ApproachOf(seat), _path) < 0f)
                {
                    _director.NotifyDeparted(seat);
                    return;
                }
                _path.Insert(0, view.transform.position);   // seat → approach → nearest exit
                view.Walk(_path, _settings.WalkSpeed, _ => Guard(() => _director.NotifyDeparted(seat)));
            });
        }

        private void OnLeft(int seat)
        {
            Guard(() =>
            {
                if (!_customers.TryGetValue(seat, out PersonView view)) { return; }
                _customers.Remove(seat);
                if (view == null) { return; }
                if (view.IsWalking)
                {
                    // The table is free (e.g. departure timeout) but the person keeps walking out instead of vanishing mid-street.
                    int id = ++_nextLeaverId;
                    _leavers[id] = view;
                    view.ContinueThen(_ => Release(_leavers, id));
                }
                else { Destroy(view.gameObject); }
            });
        }

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
            if (seat < _seats.Count && _seats[seat] != null) { view.Place(_seats[seat].position, _seats[seat].rotation); }
            view.SetSitting(true);
        }

        private int ApproachOf(int seat)
        {
            return Graph != null && Graph.TryFind(PeopleLayout.TableApproachPrefix + (seat + 1).ToString("00"), out int node) ? node : -1;
        }

        /// <summary>
        /// Shortest walkable route from <paramref name="from"/> to the nearer street entry/exit; fills <paramref name="path"/>
        /// (from → exit) and returns its length, or -1 when unreachable.
        /// </summary>
        private float NearestEndpointRoute(int from, List<Vector3> path)
        {
            path.Clear();
            if (from < 0 || Graph == null) { return -1f; }
            float best = -1f;
            foreach (int end in new[] { _entryWest, _entryEast })
            {
                if (end < 0 || !Graph.TryFindPath(from, end, _routeScratch) || _routeScratch.Count == 0) { continue; }
                float length = 0f;
                for (int i = 1; i < _routeScratch.Count; i++) { length += Vector3.Distance(_routeScratch[i - 1], _routeScratch[i]); }
                if (best < 0f || length < best)
                {
                    best = length;
                    path.Clear();
                    path.AddRange(_routeScratch);
                }
            }
            return best;
        }

        private readonly List<Vector3> _routeScratch = new List<Vector3>();

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
            if (prefabs.Length > 0 && prefabs[0] != null) { body = Instantiate(prefabs[_random.Next(prefabs.Length)], transform, false); }
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
                Renderer renderer = shape.GetComponent<Renderer>();
                renderer.sharedMaterial = PlaceholderMaterial(renderer, color);
            }
            body.name = name;
            PersonView view = body.GetComponent<PersonView>();
            if (view == null) { view = body.AddComponent<PersonView>(); }
            view.Prepare();
            view.SetAnimatorSpeed(_paused ? 0f : 1f);
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
