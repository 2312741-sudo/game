using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using UnityEngine;

namespace TramChanh.Props
{
    /// <summary>
    /// Trigger volume laid over a puddle mesh. Raises <see cref="PropCueKind.PuddleSplash"/> when the player (CharacterController)
    /// or a vehicle moves inside it.
    ///
    /// Required configuration (see Docs/Design/COURSE_BAI4_BAI6_PHYSICS.md):
    ///  - Collider.isTrigger = true (Box, sized from the puddle mesh).
    ///  - A kinematic Rigidbody ON THIS OBJECT. Unity manual "Interaction between collider types": trigger messages are sent when
    ///    "a dynamic or kinematic trigger collider collides with any collider type" and for a static trigger only against dynamic or
    ///    kinematic colliders. A CharacterController is not documented as a Rigidbody, so the kinematic Rigidbody on the trigger is
    ///    what makes the message reliable without touching the player prefab.
    ///  - Layer must NOT be Interactable: PlayerInteractor raycasts that layer with QueryTriggerInteraction.Collide.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PuddleSplashTrigger : MonoBehaviour
    {
        [SerializeField] private LayerMask _actorLayers = (1 << TramChanhLayers.PlayerIndex) | (1 << TramChanhLayers.NpcIndex);
        [SerializeField, Min(0f)] private float _minSpeed = 0.5f;
        [SerializeField, Min(0.05f)] private float _cooldownSeconds = 0.35f;

        private readonly Dictionary<int, Vector3> _lastPosition = new Dictionary<int, Vector3>();
        private PuddleSplashPolicy _policy;
        private IEventBus _events;
        private Collider _volume;

        /// <summary>Plain C# hook; use it if you do not go through the bus.</summary>
        public event Action<PropCueRaised> CueRaised;

        public void Initialize(IEventBus events)
        {
            _events = events;
        }

        private void Awake()
        {
            _volume = GetComponent<Collider>();
            _policy = new PuddleSplashPolicy(_minSpeed, _cooldownSeconds);
        }

        private bool Accepts(Collider other)
        {
            return (_actorLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Accepts(other)) { return; }
            _lastPosition[other.GetEntityId().GetHashCode()] = other.transform.position;
            // Entering: use the real velocity when there is one. Scripted vehicles have none, so assume they are moving.
            float speed = KnownSpeed(other, out bool known);
            TrySplash(other, known ? speed : _minSpeed);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!Accepts(other)) { return; }
            int key = other.GetEntityId().GetHashCode();
            float speed = KnownSpeed(other, out bool known);
            Vector3 now = other.transform.position;
            if (!known)
            {
                if (_lastPosition.TryGetValue(key, out Vector3 before) && Time.fixedDeltaTime > 0f)
                {
                    Vector3 d = now - before;
                    d.y = 0f;
                    speed = d.magnitude / Time.fixedDeltaTime;
                }
            }
            _lastPosition[key] = now;
            TrySplash(other, speed);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!Accepts(other)) { return; }
            int key = other.GetEntityId().GetHashCode();
            _lastPosition.Remove(key);
            _policy.Exit(key);
        }

        private static float KnownSpeed(Collider other, out bool known)
        {
            known = true;
            if (other is CharacterController controller)
            {
                Vector3 v = controller.velocity;
                v.y = 0f;
                return v.magnitude;
            }
            Rigidbody body = other.attachedRigidbody;
            if (body != null && !body.isKinematic)
            {
                Vector3 v = body.linearVelocity;
                v.y = 0f;
                return v.magnitude;
            }
            known = false;
            return 0f;
        }

        private void TrySplash(Collider other, float speed)
        {
            if (!_policy.ShouldSplash(other.GetEntityId().GetHashCode(), Time.timeAsDouble, speed, out float intensity))
            {
                return;
            }
            Vector3 p = other.transform.position;
            p.y = _volume.bounds.max.y;
            var cue = new PropCueRaised(PropCueKind.PuddleSplash, p, intensity);
            CueRaised?.Invoke(cue);
            if (_events != null)
            {
                _events.Publish(cue);
            }
        }
    }
}
