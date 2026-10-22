using UnityEngine;

namespace TramChanh.Effects
{
    /// <summary>
    /// Tail-light streak of a vehicle: a TrailRenderer child at the rear. This thin component only clears the trail when the
    /// vehicle teleports (pooling) and lets the controller switch emission on and off.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class TaillightTrail : MonoBehaviour
    {
        [SerializeField, Min(0.5f), Tooltip("A move bigger than this in one frame is a jump, not driving: the trail is cleared.")]
        private float _maxStepMeters = 3f;

        private TrailRenderer _trail;
        private Vector3 _last;
        private bool _hasLast;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
        }

        private void OnEnable()
        {
            _hasLast = false;
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            if (_hasLast && TrailResetRule.ShouldClear(_last, position, _maxStepMeters))
            {
                _trail.Clear();
            }
            _last = position;
            _hasLast = true;
        }

        public void SetEmitting(bool emitting)
        {
            if (_trail == null)
            {
                _trail = GetComponent<TrailRenderer>();
            }
            if (!emitting)
            {
                _trail.Clear();
            }
            _trail.emitting = emitting;
        }
    }
}
