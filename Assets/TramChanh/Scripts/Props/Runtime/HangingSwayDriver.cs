using System;
using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Props
{
    /// <summary>
    /// Drives a hanging prop (Rigidbody + HingeJoint or SpringJoint, set up by PhysicsPropsBuilder) with small forces:
    /// a gentle wind gust every physics step and a nudge when something brushes past (via <see cref="HangingNudgeZone"/>).
    /// It never changes the joint: Break Force / Break Torque stay at Infinity, so the prop cannot fall off.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HangingSwayDriver : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _wind = 0.15f;
        [SerializeField, Min(0f)] private float _windForce = 0.6f;
        [SerializeField] private Vector3 _windDirection = Vector3.right;
        [SerializeField, Min(0f)] private float _nudgePerSpeed = 0.12f;
        [SerializeField, Min(0f)] private float _maxNudgeImpulse = 0.25f;

        private Rigidbody _body;
        private IEventBus _events;
        private float _phase;

        public event Action<PropCueRaised> CueRaised;

        /// <summary>0 = calm, 1 = strong gust. The lead can drive this from weather/rain; the force stays small either way.</summary>
        public float Wind
        {
            get => _wind;
            set => _wind = Mathf.Clamp01(value);
        }

        public void Initialize(IEventBus events)
        {
            _events = events;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _phase = (GetEntityId().GetHashCode() & 0x7fffffff) % 97 * 0.1f; // de-synchronise several hanging props
        }

        private void FixedUpdate()
        {
            if (_wind <= 0.001f)
            {
                return;
            }
            float gust = SwayModel.Gust(Time.timeAsDouble, _wind * _windForce, _phase);
            Vector3 dir = _windDirection.sqrMagnitude > 1e-4f ? _windDirection.normalized : Vector3.right;
            _body.AddForce(dir * gust, ForceMode.Force);
        }

        /// <summary>Called by <see cref="HangingNudgeZone"/> when another body enters the zone.</summary>
        public void Nudge(Vector3 awayDirection, float otherSpeed)
        {
            float impulse = SwayModel.NudgeImpulse(otherSpeed, _nudgePerSpeed, _maxNudgeImpulse);
            if (impulse <= 0f)
            {
                return;
            }
            awayDirection.y = 0f;
            if (awayDirection.sqrMagnitude < 1e-6f)
            {
                return;
            }
            _body.AddForce(awayDirection.normalized * impulse, ForceMode.Impulse);
            var cue = new PropCueRaised(PropCueKind.HangingNudge, transform.position, Mathf.Clamp01(impulse / Mathf.Max(0.0001f, _maxNudgeImpulse)));
            CueRaised?.Invoke(cue);
            if (_events != null)
            {
                _events.Publish(cue);
            }
        }
    }
}
