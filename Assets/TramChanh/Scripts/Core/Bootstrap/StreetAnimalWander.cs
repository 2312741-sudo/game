using System.Collections.Generic;
using TramChanh.App.People;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.App
{
    /// <summary>
    /// Thin Unity view of a street animal's AI (dog, cat, chicken, pigeon). The name is kept so the bootstrap still adds it the same way.
    /// The thinking is in <see cref="AnimalBrain"/> (finite state machine) and <see cref="AnimalPerception"/> (view cone, hearing, smell);
    /// this class only senses on a timer, walks the path with <see cref="PathFollower"/> over the <see cref="WaypointGraph"/>,
    /// steps aside from people and neighbours, and drives an Animator when the prefab has one.
    /// The bootstrap calls <see cref="Tick"/> with the game clock so animals freeze while the game is paused.
    /// Animals far from the player are updated less often (<see cref="AnimalLod"/>).
    /// No NavMesh: the street is built at run time (Docs/Design/COURSE_AI_ANIMALS.md).
    /// </summary>
    public sealed class StreetAnimalWander : MonoBehaviour
    {
        private const float TurnDegreesPerSecond = 220f;
        private const float FleeTurnDegreesPerSecond = 540f;
        private const float HopHeight = 0.025f;
        private const float HopRate = 9f;
        private const float SteerPushSpeed = 1.2f;
        private const float SteerRelaxPerSecond = 2.5f;
        private const float MaxSteerOffset = 0.6f;
        private const float NeighbourScanMetres = 3f;
        private const float ArriveRadius = 0.05f;
        private const float LabelHeight = 0.9f;

        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int SittingParameter = Animator.StringToHash("Sitting");

        [SerializeField, Tooltip("Show this animal's state as floating text (demo / debugging).")] private bool _showDebugLabel;

        private readonly List<Vector3> _route = new List<Vector3>(8);
        private readonly List<Vector3> _others = new List<Vector3>(8);
        private readonly List<AnimalThreat> _threatScratch = new List<AnimalThreat>(4);
        private readonly SeparationSteering _steer = new SeparationSteering();

        private StreetAnimalContext _context;
        private AnimalProfile _profile;
        private AnimalBrain _brain;
        private IAnimalPerception _perception;
        private PathFollower _follower;
        private int _planId;
        private AnimalSenses _senses;
        private AnimalLodTier _tier = AnimalLodTier.Near;
        private Vector3 _home;
        private float _homeY;
        private float _thinkAccum;
        private float _moveAccum;
        private float _hopPhase;
        private bool _routeUsedGraph;
        private bool _customPerception;
        private Animator _animator;
        private bool _hasSpeedParameter;
        private bool _hasSittingParameter;
        private TextMesh _label;
        private string _labelText;

        public AnimalState State => _brain == null ? AnimalState.Idle : _brain.State;

        public AnimalBrain Brain => _brain;

        public AnimalProfile Profile => _profile;

        public AnimalLodTier LodTier => _tier;

        public bool ShowDebugLabel { get => _showDebugLabel; set => _showDebugLabel = value; }

        /// <summary>Call once after the animal is in place. <paramref name="seed"/> makes each animal move differently.</summary>
        public void Configure(float radius, float speed, int seed)
        {
            _home = transform.position;
            _homeY = _home.y;
            _profile = AnimalProfile.For(SpeciesFromName(gameObject.name), radius, speed);
            _brain = new AnimalBrain(_profile, _home, seed);
            if (!_customPerception) { _perception = null; }
            _follower = null;
            _planId = 0;
            FindAnimator();
        }

        /// <summary>Shares the street data (graph, player, food, vehicles) with this animal. Optional.</summary>
        public void Bind(StreetAnimalContext context)
        {
            if (_context != null) { _context.Unregister(this); }
            _context = context;
            if (!_customPerception) { _perception = null; }
            if (_context != null) { _context.Register(this); }
        }

        /// <summary>Replaces perception with fake data (tests, demo). Pass null to go back to the real senses.</summary>
        public void SetPerception(IAnimalPerception perception)
        {
            _perception = perception;
            _customPerception = perception != null;
        }

        public void Tick(float seconds)
        {
            if (_brain == null || !(seconds > 0f)) { return; }
            StreetAnimalContext context = Context();
            _tier = context.TryGetViewer(out Vector3 viewer) ? AnimalLod.TierFor(Flat(transform.position, viewer)) : AnimalLodTier.Near;

            _thinkAccum += seconds;
            _moveAccum += seconds;
            if (_thinkAccum >= AnimalLod.ThinkInterval(_tier))
            {
                Think(_thinkAccum);
                _thinkAccum = 0f;
            }

            if (_tier == AnimalLodTier.Near || _moveAccum >= AnimalLod.MoveInterval(_tier))
            {
                Move(_moveAccum);
                _moveAccum = 0f;
            }

            UpdateLabel(context);
        }

        private void Think(float seconds)
        {
            if (_perception == null) { EnsurePerception(); }
            Vector3 position = transform.position;
            _senses = _perception.Sense(position, transform.forward);
            CollectOthers(position);
            _brain.Update(seconds, position, in _senses);
        }

        private void EnsurePerception()
        {
            if (_perception != null) { return; }
            StreetAnimalContext context = Context();
            _perception = new AnimalPerception(_profile, context, context, context.LineOfSight);
        }

        private StreetAnimalContext Context()
        {
            if (_context == null) { _context = new StreetAnimalContext(); }
            return _context;
        }

        // People, motorbikes and neighbouring animals the steering should keep away from.
        private void CollectOthers(Vector3 position)
        {
            _others.Clear();
            _threatScratch.Clear();
            StreetAnimalContext context = Context();
            context.Collect(_threatScratch);
            for (int i = 0; i < _threatScratch.Count; i++)
            {
                if (Flat(position, _threatScratch[i].Position) <= NeighbourScanMetres) { _others.Add(_threatScratch[i].Position); }
            }

            IReadOnlyList<StreetAnimalWander> animals = context.Animals;
            for (int i = 0; i < animals.Count; i++)
            {
                StreetAnimalWander other = animals[i];
                if (other == null || other == this) { continue; }
                Vector3 p = other.transform.position;
                if (Flat(position, p) <= NeighbourScanMetres) { _others.Add(p); }
            }
        }

        private void Move(float seconds)
        {
            if (_follower != null && !_brain.HasDestination)
            {
                _follower = null;
                _steer.Reset();
            }

            if (_brain.HasDestination && _brain.DestinationId != _planId) { Plan(); }

            Vector3 position = transform.position;
            float speed = 0f;
            if (_follower != null)
            {
                _follower.Step(seconds);
                Vector3 ideal = _follower.Position;
                ideal.y = _homeY;
                ILineOfSight walls = Context().LineOfSight;
                Vector3 feeler = Vector3.up * 0.15f;
                if (walls.IsBlocked(position + feeler, ideal + feeler))
                {
                    _follower = null;
                    _steer.Reset();
                    _brain.ReportBlocked();
                }
                else
                {
                    Vector3 push = SeparationSteering.Push(ideal, _others, _profile.SeparationRadius);
                    _steer.Step(seconds, push, SteerPushSpeed, SteerRelaxPerSecond, MaxSteerOffset);
                    Vector3 next = ideal + _steer.Offset;
                    if (_steer.Offset.sqrMagnitude > 1e-6f && walls.IsBlocked(position + feeler, next + feeler))
                    {
                        _steer.Reset();
                        next = ideal;
                    }

                    FaceTowards(next - position, seconds, _brain.State == AnimalState.Flee ? FleeTurnDegreesPerSecond : TurnDegreesPerSecond);
                    speed = _profile.WalkSpeed * _brain.SpeedMultiplier;
                    _hopPhase += HopRate * (_brain.SpeedMultiplier > 1f ? 1.6f : 1f) * seconds;
                    next.y = _homeY + (_animator == null ? Mathf.Abs(Mathf.Sin(_hopPhase)) * HopHeight : 0f);
                    transform.position = next;
                    if (_follower.Arrived)
                    {
                        _follower = null;
                        _steer.Reset();
                        _brain.ReportArrived();
                        speed = 0f;
                    }
                }
            }

            if (_follower == null)
            {
                Vector3 p = transform.position;
                p.y = _homeY;
                transform.position = p;
                if (_brain.HasFocus) { FaceTowards(_brain.Focus - p, seconds, TurnDegreesPerSecond); }
            }

            ApplyAnimator(speed);
        }

        private void Plan()
        {
            _planId = _brain.DestinationId;
            Vector3 from = transform.position;
            from.y = _homeY;
            Vector3 to = _brain.Destination;
            to.y = _homeY;
            StreetAnimalContext context = Context();
            _routeUsedGraph = AnimalRoute.Build(context.Graph, context.Lanes, from, to, _route);
            _follower = new PathFollower(_route, _profile.WalkSpeed * _brain.SpeedMultiplier, ArriveRadius);
            _steer.Reset();
            if (_follower.Arrived)
            {
                _follower = null;
                _brain.ReportArrived();
            }
        }

        private void FaceTowards(Vector3 direction, float seconds, float degreesPerSecond)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f) { return; }
            Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, degreesPerSecond * seconds);
        }

        private void FindAnimator()
        {
            _animator = GetComponentInChildren<Animator>(true);
            _hasSpeedParameter = false;
            _hasSittingParameter = false;
            if (_animator == null) { return; }
            _animator.applyRootMotion = false;
            if (_animator.runtimeAnimatorController == null) { _animator = null; return; }
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == SpeedParameter) { _hasSpeedParameter = true; }
                else if (parameter.nameHash == SittingParameter) { _hasSittingParameter = true; }
            }
        }

        // The current animal prefabs have no Animator, so this does nothing there (the hop in Move is the only motion cue).
        // The parameter names "Speed" and "Sitting" are the ones PersonView reads; they are set only if the controller declares them.
        private void ApplyAnimator(float speed)
        {
            if (_animator == null) { return; }
            if (_hasSpeedParameter) { _animator.SetFloat(SpeedParameter, speed); }
            if (_hasSittingParameter) { _animator.SetBool(SittingParameter, _brain.IsSitting); }
        }

        private void UpdateLabel(StreetAnimalContext context)
        {
            bool want = (_showDebugLabel || context.ShowStateLabels) && _tier <= AnimalLodTier.Mid;
            if (!want)
            {
                if (_label != null) { _label.gameObject.SetActive(false); }
                return;
            }

            if (_label == null) { _label = CreateLabel(); }
            if (_label == null) { return; }
            _label.gameObject.SetActive(true);
            string text = _labelText;
            string wanted = StateText();
            if (text != wanted)
            {
                _labelText = wanted;
                _label.text = wanted;
            }

            Transform t = _label.transform;
            t.position = transform.position + Vector3.up * LabelHeight;
            Camera camera = Camera.main;
            if (camera != null) { t.rotation = camera.transform.rotation; }
        }

        private string _stateTextCache;
        private AnimalState _stateTextState = (AnimalState)(-1);
        private bool _stateTextSitting;

        private string StateText()
        {
            if (_stateTextCache == null || _stateTextState != _brain.State || _stateTextSitting != _brain.IsSitting)
            {
                _stateTextState = _brain.State;
                _stateTextSitting = _brain.IsSitting;
                _stateTextCache = _profile.Species + "\n" + _brain.State + (_brain.IsSitting ? " (sit)" : string.Empty);
            }

            return _stateTextCache;
        }

        private TextMesh CreateLabel()
        {
            var go = new GameObject("AI_StateLabel_" + gameObject.name);
            go.hideFlags = HideFlags.DontSave;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.characterSize = 0.04f;
            mesh.fontSize = 64;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                mesh.font = font;
                MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
                if (meshRenderer != null) { meshRenderer.sharedMaterial = font.material; }
            }

            return mesh;
        }

        private void OnDestroy()
        {
            if (_context != null) { _context.Unregister(this); }
            if (_label != null) { Destroy(_label.gameObject); }
        }

        private static AnimalSpecies SpeciesFromName(string objectName)
        {
            if (objectName.Contains("Cat")) { return AnimalSpecies.Cat; }
            if (objectName.Contains("Chicken")) { return AnimalSpecies.Chicken; }
            if (objectName.Contains("Pigeon")) { return AnimalSpecies.Pigeon; }
            return AnimalSpecies.Dog;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Scene view (select the animal): view cone, hearing and flee radii, planned route, destination and the state name.
        private void OnDrawGizmosSelected()
        {
            if (_profile == null || _brain == null) { return; }
            Vector3 p = transform.position + Vector3.up * 0.1f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f) { forward = Vector3.forward; }
            forward.Normalize();

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            float half = Mathf.Min(_profile.ViewHalfAngleDegrees, 179f);
            Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * forward * _profile.ViewDistance;
            Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * forward * _profile.ViewDistance;
            Gizmos.DrawLine(p, p + left);
            Gizmos.DrawLine(p, p + right);
            const int segments = 16;
            Vector3 previous = p + left;
            for (int i = 1; i <= segments; i++)
            {
                float angle = -half + 2f * half * i / segments;
                Vector3 point = p + Quaternion.AngleAxis(angle, Vector3.up) * forward * _profile.ViewDistance;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(p, _profile.HearDistance);
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.8f);
            Gizmos.DrawWireSphere(p, _profile.FleeDistance);

            Gizmos.color = Color.green;
            for (int i = 1; i < _route.Count; i++) { Gizmos.DrawLine(_route[i - 1] + Vector3.up * 0.1f, _route[i] + Vector3.up * 0.1f); }
            if (_brain.HasDestination)
            {
                Gizmos.color = _routeUsedGraph ? Color.cyan : Color.green;
                Gizmos.DrawWireSphere(_brain.Destination + Vector3.up * 0.1f, 0.12f);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(p + Vector3.up * 0.8f, _profile.Species + ": " + _brain.State
                + "  (" + _tier + ")  beg cd " + _brain.BegCooldownLeft.ToString("0") + "s  inv cd " + _brain.InvestigateCooldownLeft.ToString("0") + "s");
#endif
        }
    }
}
