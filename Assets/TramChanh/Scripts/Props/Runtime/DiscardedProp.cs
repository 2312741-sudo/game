using System;
using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Props
{
    /// <summary>
    /// A thrown-away item lying on the street: Rigidbody + Collider + PhysicsMaterial. OnCollisionEnter raises a
    /// <see cref="PropCueRaised"/> (IEventBus and/or the <see cref="CueRaised"/> action). When the spawner's remaining time
    /// for this prop drops below the fade time the prop shrinks to nothing (placeholder materials are opaque, so scale is the fade).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DiscardedProp : MonoBehaviour
    {
        private readonly ImpactCuePolicy _policy = new ImpactCuePolicy();
        private IEventBus _events;
        private Vector3 _baseScale = Vector3.one;

        /// <summary>Plain C# hook for anyone who does not use the bus.</summary>
        public event Action<PropCueRaised> CueRaised;

        public int PropId { get; internal set; }

        public void Initialize(IEventBus events)
        {
            _events = events;
            _baseScale = transform.localScale;
        }

        /// <summary>0..1: 1 = fully visible, 0 = gone. Called by the spawner.</summary>
        internal void SetVisibleFraction(float fraction)
        {
            float f = Mathf.Clamp01(fraction);
            transform.localScale = _baseScale * f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_policy.ShouldPlay(Time.timeAsDouble, collision.relativeVelocity.magnitude, out float intensity))
            {
                return;
            }
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            var cue = new PropCueRaised(PropCueKind.DebrisImpact, point, intensity);
            CueRaised?.Invoke(cue);
            if (_events != null)
            {
                _events.Publish(cue);
            }
        }
    }
}
