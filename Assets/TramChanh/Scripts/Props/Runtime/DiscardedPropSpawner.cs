using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using UnityEngine;

namespace TramChanh.Props
{
    /// <summary>
    /// Spawns the physical "rubbish" copy of a discarded item and removes it again after a few seconds.
    /// The copy is built from the source's MeshFilter/MeshRenderer pairs only (no scripts, colliders or interactables are copied),
    /// with one BoxCollider fitted to the combined bounds. At most <see cref="MaxAlive"/> copies exist at once (oldest removed first).
    /// Call <see cref="SpawnCopyOf"/> from whoever discards an item (the item is destroyed by the caller as before).
    /// </summary>
    public sealed class DiscardedPropSpawner : MonoBehaviour
    {
        private const float MinSide = 0.04f;
        private const float MaxSide = 0.6f;

        [SerializeField] private PhysicsMaterial _material;
        [SerializeField, Min(1)] private int _maxAlive = 8;
        [SerializeField, Min(1f)] private float _lifetimeSeconds = 6f;
        [SerializeField, Min(0.1f)] private float _fadeSeconds = 1.5f;

        private readonly List<DiscardedProp> _alive = new List<DiscardedProp>();
        private DebrisBudget _budget;
        private IEventBus _events;
        private int _nextId;

        public int MaxAlive => _maxAlive;
        public int AliveCount => _alive.Count;

        /// <summary>Optional. With a bus, every impact is published as <see cref="PropCueRaised"/>.</summary>
        public void Initialize(IEventBus events)
        {
            _events = events;
        }

        /// <summary>Returns the new prop, or null when the source has no mesh to copy.</summary>
        public DiscardedProp SpawnCopyOf(GameObject source, Vector3 position, Quaternion rotation, Vector3 initialVelocity)
        {
            if (source == null)
            {
                return null;
            }
            EnsureBudget();

            MeshRenderer[] renderers = source.GetComponentsInChildren<MeshRenderer>(false);
            if (renderers.Length == 0)
            {
                return null;
            }

            var root = new GameObject("DiscardedProp_" + _nextId);
            root.layer = TramChanhLayers.HeldItemIndex;
            root.transform.SetPositionAndRotation(position, rotation);

            bool boundsStarted = false;
            Bounds local = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer r = renderers[i];
                MeshFilter f = r.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null)
                {
                    continue;
                }
                var part = new GameObject(r.gameObject.name);
                part.layer = root.layer;
                Transform pt = part.transform;
                pt.SetParent(root.transform, false);
                // Same pose relative to the source root, so the copy looks like the thing that was in the hand.
                pt.localPosition = source.transform.InverseTransformPoint(r.transform.position);
                pt.localRotation = Quaternion.Inverse(source.transform.rotation) * r.transform.rotation;
                pt.localScale = r.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = f.sharedMesh;
                MeshRenderer copy = part.AddComponent<MeshRenderer>();
                copy.sharedMaterials = r.sharedMaterials;
                copy.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                Bounds mb = f.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 p = pt.localPosition + pt.localRotation * Vector3.Scale(mb.center + Vector3.Scale(mb.extents, sign), pt.localScale);
                    if (!boundsStarted) { local = new Bounds(p, Vector3.zero); boundsStarted = true; }
                    else { local.Encapsulate(p); }
                }
            }
            if (!boundsStarted)
            {
                Destroy(root);
                return null;
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = new Vector3(
                Mathf.Clamp(local.size.x, MinSide, MaxSide),
                Mathf.Clamp(local.size.y, MinSide, MaxSide),
                Mathf.Clamp(local.size.z, MinSide, MaxSide));
            box.sharedMaterial = _material != null ? _material : (_material = DebrisPhysicsDefaults.CreateMaterial());
            box.excludeLayers = DebrisPhysicsDefaults.ExcludedLayerMask;

            var body = root.AddComponent<Rigidbody>();
            body.mass = DebrisPhysicsDefaults.Mass;
            body.linearDamping = DebrisPhysicsDefaults.LinearDamping;
            body.angularDamping = DebrisPhysicsDefaults.AngularDamping;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = initialVelocity;

            DiscardedProp prop = root.AddComponent<DiscardedProp>();
            prop.PropId = _nextId++;
            prop.Initialize(_events);

            int evicted = _budget.Register(prop.PropId, Time.timeAsDouble, _lifetimeSeconds);
            _alive.Add(prop);
            if (evicted >= 0)
            {
                Remove(evicted);
            }
            return prop;
        }

        private void EnsureBudget()
        {
            if (_budget == null)
            {
                _budget = new DebrisBudget(_maxAlive);
            }
        }

        private void Update()
        {
            if (_budget == null || _alive.Count == 0)
            {
                return;
            }
            double now = Time.timeAsDouble;
            while (_budget.TryTakeExpired(now, out int id))
            {
                Remove(id);
            }
            for (int i = 0; i < _alive.Count; i++)
            {
                DiscardedProp p = _alive[i];
                double left = _budget.Remaining(p.PropId, now);
                p.SetVisibleFraction((float)(left / _fadeSeconds));
            }
        }

        private void Remove(int id)
        {
            for (int i = 0; i < _alive.Count; i++)
            {
                if (_alive[i] != null && _alive[i].PropId == id)
                {
                    Destroy(_alive[i].gameObject);
                    _alive.RemoveAt(i);
                    return;
                }
                if (_alive[i] == null)
                {
                    _alive.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
