using System;
using System.Collections.Generic;
using TramChanh.Cakes;
using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Effects
{
    /// <summary>
    /// Thin adapter between the game and the particle effects (rain, steam, tail-light trails, hanging wire).
    /// Every decision lives in <see cref="NightEffectsModel"/>; this class only moves objects and sets module values.
    /// Put the prefab PF_NightEffects (built by "Tram Chanh/Effects/Build Effect Prefabs") in the scene, then call
    /// <see cref="SetNightEvent"/>, <see cref="SetCameraTarget"/>, <see cref="SetGrillAnchor"/>, <see cref="SetCupAnchor"/>,
    /// <see cref="SetStallRoot"/>, <see cref="SetVehicleRoot"/> and <see cref="BindEvents"/> from the bootstrap.
    /// Every call is optional: an effect without its target simply does not show.
    /// STATUS: not compiled and not run when written (no Unity available).
    /// </summary>
    public sealed class NightEffectsController : MonoBehaviour
    {
        [Header("Built by the Effects builder")]
        [SerializeField] private ParticleSystem _rain;
        [SerializeField, Tooltip("Collision plane of the rain; only its height is driven (the ground under the target).")]
        private Transform _rainGroundPlane;
        [SerializeField] private GameObject _grillSteamPrefab;
        [SerializeField] private GameObject _cupSteamPrefab;
        [SerializeField] private GameObject _taillightPrefab;
        [SerializeField] private SagWire _wire;

        [Header("Tuning")]
        [SerializeField, Min(2f), Tooltip("Height of the rain emitter box above the ground, metres.")]
        private float _rainHeight = 8f;
        [SerializeField, Tooltip("Where the tail light sits in the local space of a vehicle (x right, y up, z forward). Not measured on the art: tune it.")]
        private Vector3 _taillightLocalOffset = new Vector3(0f, 0.65f, -0.85f);
        [SerializeField, Min(0.2f), Tooltip("Seconds between two searches for new vehicles under the vehicle root.")]
        private float _vehicleScanSeconds = 1f;

        [Header("Demo switches (change them while playing)")]
        [SerializeField, Tooltip("Rain whatever the night event is.")] private bool _forceRain;
        [SerializeField] private bool _rainEnabled = true;
        [SerializeField] private bool _steamEnabled = true;
        [SerializeField] private bool _taillightsEnabled = true;
        [SerializeField] private bool _wireEnabled = true;

        private sealed class SteamHandle
        {
            public ParticleSystem System;
            public float BaseRate;
        }

        private readonly NightEffectsModel _model = new NightEffectsModel();
        private readonly List<TaillightTrail> _trails = new List<TaillightTrail>();
        private readonly HashSet<int> _scanned = new HashSet<int>();
        private SteamHandle _grillSteam;
        private SteamHandle _cupSteam;
        private IDisposable _cakeSubscription;
        private Transform _target;
        private float _targetHeightAboveGround;
        private Transform _vehicleRoot;
        private float _scanTimer;
        private float _rainBaseRate = 1f;
        private bool _taillightsApplied = true;
        private bool _wireApplied = true;

        /// <summary>Read-only view of the decisions, for tests and the debug HUD.</summary>
        public NightEffectsModel Model => _model;

        // ------------------------------------------------------------------ public API (bootstrap)

        /// <summary>Tells the effects which event the current night has. Rain fades in and out by itself.</summary>
        public void SetNightEvent(NightEvent night)
        {
            _model.SetNight(night);
        }

        /// <summary>The object the rain follows (the player). <paramref name="targetHeightAboveGround"/> is 0 for a transform at the feet, about 1.7 for a camera.</summary>
        public void SetCameraTarget(Transform target, float targetHeightAboveGround = 0f)
        {
            _target = target;
            _targetHeightAboveGround = targetHeightAboveGround;
            FollowTarget();
        }

        /// <summary>Puts a steam source on the grill (the Grill anchor of the stall). Pass null to remove it.</summary>
        public void SetGrillAnchor(Transform anchor)
        {
            DestroySteam(ref _grillSteam);
            _grillSteam = SpawnSteam(_grillSteamPrefab, anchor);
        }

        /// <summary>Puts a steam source on the hot tea (the TeaRack anchor of the stall). Pass null to remove it.</summary>
        public void SetCupAnchor(Transform anchor)
        {
            DestroySteam(ref _cupSteam);
            _cupSteam = SpawnSteam(_cupSteamPrefab, anchor);
        }

        /// <summary>Hangs the wire on the stall root (the end points of the wire prefab are in the local space of this root).</summary>
        public void SetStallRoot(Transform stallRoot)
        {
            if (_wire == null)
            {
                return;
            }
            if (stallRoot == null)
            {
                _wire.transform.SetParent(transform, false);
                _wire.gameObject.SetActive(false);
                return;
            }
            _wire.transform.SetParent(stallRoot, false);
            _wire.Rebuild();
            _wireApplied = !_model.IsEnabled(EffectKinds.Wire);
            ApplyWire();
        }

        /// <summary>The object whose children are the vehicles (the People object). Tail lights are added to new vehicles found there.</summary>
        public void SetVehicleRoot(Transform root)
        {
            _vehicleRoot = root;
            _scanTimer = 0f;
        }

        /// <summary>Subscribes to the cake steps (steam puff when the cake is flipped). Calling it again replaces the old subscription.</summary>
        public void BindEvents(IEventBus bus)
        {
            UnbindEvents();
            if (bus != null)
            {
                _cakeSubscription = bus.Subscribe<CakeStepCompleted>(OnCakeStep);
            }
        }

        public void UnbindEvents()
        {
            if (_cakeSubscription != null)
            {
                _cakeSubscription.Dispose();
                _cakeSubscription = null;
            }
        }

        /// <summary>A new night starts: no cake is on the grill any more.</summary>
        public void ResetNight()
        {
            _model.ResetNight();
        }

        public void SetEffectEnabled(EffectKinds kind, bool on)
        {
            _model.SetEnabled(kind, on);
            if ((kind & EffectKinds.Rain) != 0) { _rainEnabled = on; }
            if ((kind & EffectKinds.Steam) != 0) { _steamEnabled = on; }
            if ((kind & EffectKinds.Taillights) != 0) { _taillightsEnabled = on; }
            if ((kind & EffectKinds.Wire) != 0) { _wireEnabled = on; }
        }

        public void SetForceRain(bool force)
        {
            _forceRain = force;
            _model.SetForceRain(force);
        }

        /// <summary>Sideways acceleration of the rain in m/s^2 (Force over Lifetime X). Positive leans the rain toward +X.</summary>
        public void SetWind(float acceleration)
        {
            if (_rain == null)
            {
                return;
            }
            ParticleSystem.ForceOverLifetimeModule force = _rain.forceOverLifetime;
            force.x = new ParticleSystem.MinMaxCurve(acceleration * 0.6f, acceleration * 1.4f);
            force.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            force.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        // ------------------------------------------------------------------ Unity messages

        private void Awake()
        {
            if (_rain != null)
            {
                _rainBaseRate = Mathf.Max(1f, _rain.emission.rateOverTimeMultiplier);
                ParticleSystem.EmissionModule emission = _rain.emission;
                emission.enabled = false;
            }
            if (_wire != null)
            {
                _wire.gameObject.SetActive(false); // shown once SetStallRoot gives it a place
            }
            SyncSwitches();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void Update()
        {
            SyncSwitches();
            _model.Step(Time.deltaTime);
            ApplyRain();
            ApplySteam(_grillSteam, _model.GrillSteam.Current);
            ApplySteam(_cupSteam, _model.CupSteam.Current);
            ApplyTaillightSwitch();
            ApplyWire();
            ScanVehicles(Time.deltaTime);
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

        // ------------------------------------------------------------------ rain

        private void ApplyRain()
        {
            if (_rain == null)
            {
                return;
            }
            ParticleSystem.EmissionModule emission = _rain.emission;
            if (_model.RainShouldEmit)
            {
                emission.enabled = true;
                emission.rateOverTimeMultiplier = _rainBaseRate * _model.Rain.Current;
                if (!_rain.isPlaying)
                {
                    _rain.Play(false); // false: do not start child systems (the splash is a sub emitter, started by collisions)
                }
            }
            else
            {
                if (emission.enabled)
                {
                    emission.enabled = false; // stop emitting, let the last drops fall
                }
                if (_rain.isPlaying && _rain.particleCount == 0)
                {
                    _rain.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void FollowTarget()
        {
            if (_target == null || _rain == null)
            {
                return;
            }
            Vector3 position = _target.position;
            float ground = position.y - _targetHeightAboveGround;
            _rain.transform.position = new Vector3(position.x, ground + _rainHeight, position.z);
            if (_rainGroundPlane != null)
            {
                _rainGroundPlane.position = new Vector3(position.x, ground, position.z);
            }
        }

        // ------------------------------------------------------------------ steam

        private void OnCakeStep(CakeStepCompleted step)
        {
            int burst = _model.OnCakeStep(step.PreparationId, step.State);
            if (burst > 0 && _grillSteam != null && _grillSteam.System != null)
            {
                _grillSteam.System.Emit(burst);
            }
        }

        private SteamHandle SpawnSteam(GameObject prefab, Transform anchor)
        {
            if (prefab == null || anchor == null)
            {
                return null;
            }
            GameObject instance = Instantiate(prefab, anchor, false);
            instance.name = prefab.name;
            ParticleSystem system = instance.GetComponent<ParticleSystem>();
            if (system == null)
            {
                Destroy(instance);
                return null;
            }
            var handle = new SteamHandle { System = system, BaseRate = Mathf.Max(0.1f, system.emission.rateOverTimeMultiplier) };
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            system.Play(true);
            return handle;
        }

        private static void DestroySteam(ref SteamHandle handle)
        {
            if (handle != null && handle.System != null)
            {
                Destroy(handle.System.gameObject);
            }
            handle = null;
        }

        private static void ApplySteam(SteamHandle handle, float level)
        {
            if (handle == null || handle.System == null)
            {
                return;
            }
            ParticleSystem.EmissionModule emission = handle.System.emission;
            bool on = level > 0f;
            if (emission.enabled != on)
            {
                emission.enabled = on;
            }
            if (on)
            {
                emission.rateOverTimeMultiplier = handle.BaseRate * level;
            }
        }

        // ------------------------------------------------------------------ tail lights

        private void ScanVehicles(float deltaSeconds)
        {
            if (_vehicleRoot == null || _taillightPrefab == null)
            {
                return;
            }
            _scanTimer -= deltaSeconds;
            if (_scanTimer > 0f)
            {
                return;
            }
            _scanTimer = _vehicleScanSeconds;
            if (_scanned.Count > 1024)
            {
                _scanned.Clear(); // ids of destroyed vehicles pile up; the TaillightTrail check below keeps this safe
            }
            for (int i = 0; i < _vehicleRoot.childCount; i++)
            {
                Transform child = _vehicleRoot.GetChild(i);
                if (!_scanned.Add(child.GetEntityId().GetHashCode()))
                {
                    continue;
                }
                if (VehicleNameRule.IsVehicle(child.name) && child.GetComponentInChildren<TaillightTrail>(true) == null)
                {
                    AttachTaillight(child);
                }
            }
        }

        private void AttachTaillight(Transform vehicle)
        {
            GameObject instance = Instantiate(_taillightPrefab, vehicle, false);
            instance.name = _taillightPrefab.name;
            instance.transform.localPosition = _taillightLocalOffset;
            TaillightTrail trail = instance.GetComponent<TaillightTrail>();
            if (trail == null)
            {
                Destroy(instance);
                return;
            }
            trail.SetEmitting(_model.IsEnabled(EffectKinds.Taillights));
            _trails.Add(trail);
        }

        private void ApplyTaillightSwitch()
        {
            bool on = _model.IsEnabled(EffectKinds.Taillights);
            if (on == _taillightsApplied)
            {
                return;
            }
            _taillightsApplied = on;
            for (int i = _trails.Count - 1; i >= 0; i--)
            {
                if (_trails[i] == null)
                {
                    _trails.RemoveAt(i);
                    continue;
                }
                _trails[i].SetEmitting(on);
            }
        }

        // ------------------------------------------------------------------ wire and demo switches

        private void ApplyWire()
        {
            if (_wire == null)
            {
                return;
            }
            bool on = _model.IsEnabled(EffectKinds.Wire) && _wire.transform.parent != transform;
            if (on == _wireApplied)
            {
                return;
            }
            _wireApplied = on;
            _wire.gameObject.SetActive(on);
        }

        private void SyncSwitches()
        {
            _model.SetForceRain(_forceRain);
            _model.SetEnabled(EffectKinds.Rain, _rainEnabled);
            _model.SetEnabled(EffectKinds.Steam, _steamEnabled);
            _model.SetEnabled(EffectKinds.Taillights, _taillightsEnabled);
            _model.SetEnabled(EffectKinds.Wire, _wireEnabled);
        }

        [ContextMenu("Demo: rain on")]
        private void DemoRainOn() { _forceRain = true; }

        [ContextMenu("Demo: rain off")]
        private void DemoRainOff() { _forceRain = false; }
    }
}
