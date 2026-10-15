using TramChanh.Core;
using UnityEngine;

namespace TramChanh.App
{
    /// <summary>
    /// Makes a street animal (dog, cat, chicken) wander slowly around where it was placed: rest, pick a spot, walk to it, rest again.
    /// The bootstrap calls <see cref="Tick"/> with the game clock so animals freeze while the game is paused.
    /// The models have no animation clips, so walking is a small hop and a turn toward the target.
    /// </summary>
    public sealed class StreetAnimalWander : MonoBehaviour
    {
        private const float ObstacleProbeDistance = 0.45f;
        private const float TurnDegreesPerSecond = 220f;
        private const float HopHeight = 0.025f;
        private const float HopRate = 9f;
        private const float MinWalkMetres = 0.25f;
        private const float ObstacleMargin = 0.4f;

        private Vector3 _home;
        private float _homeY;
        private float _radius = 1.5f;
        private float _speed = 0.4f;
        private float _restSeconds;
        private float _walkSecondsLeft;
        private Vector3 _target;
        private bool _walking;
        private float _hopPhase;
        private System.Random _random;

        /// <summary>Call once after the animal is in place. <paramref name="seed"/> makes each animal move differently.</summary>
        public void Configure(float radius, float speed, int seed)
        {
            _home = transform.position;
            _homeY = _home.y;
            _radius = Mathf.Max(0f, radius);
            _speed = Mathf.Max(0.05f, speed);
            _random = new System.Random(seed);
            _restSeconds = 0.5f + (float)_random.NextDouble() * 3f;
            _walking = false;
        }

        public void Tick(float seconds)
        {
            if (_random == null || seconds <= 0f) { return; }
            if (_walking) { Walk(seconds); } else { Rest(seconds); }
        }

        private void Rest(float seconds)
        {
            _restSeconds -= seconds;
            if (_restSeconds > 0f) { return; }
            // Try a few spots; each one is cut short in front of the first obstacle on the way (a wall or a neighbour).
            float distance = 0f;
            for (int attempt = 0; attempt < 6 && distance < MinWalkMetres; attempt++)
            {
                WanderArea.Pick(_home.x, _home.z, _radius, _random.NextDouble(), _random.NextDouble(), out float x, out float z);
                _target = new Vector3(x, _homeY, z);
                distance = ClearDistanceTo(_target);
            }
            _target = transform.position + (_target - transform.position).normalized * distance;
            _walkSecondsLeft = WanderArea.WalkSeconds(distance, _speed);
            _walking = _walkSecondsLeft > 0.05f;
            if (!_walking) { _restSeconds = 1f; }
        }

        /// <summary>How far toward <paramref name="target"/> the animal can walk before reaching an obstacle (margin kept), at most the full distance.</summary>
        private float ClearDistanceTo(Vector3 target)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            float full = flat.magnitude;
            if (full < 0.01f) { return 0f; }
            Vector3 probe = transform.position + Vector3.up * 0.15f;
            if (Physics.Raycast(probe, flat / full, out RaycastHit hit, full + ObstacleMargin, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                return Mathf.Max(0f, Mathf.Min(full, hit.distance - ObstacleMargin));
            }
            return full;
        }

        private void Walk(float seconds)
        {
            Vector3 toTarget = _target - transform.position;
            toTarget.y = 0f;
            if (_walkSecondsLeft <= 0f || toTarget.sqrMagnitude < 0.0004f) { StopWalking(); return; }

            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, TurnDegreesPerSecond * seconds);

            Vector3 probe = transform.position + Vector3.up * 0.15f;
            if (Physics.Raycast(probe, transform.forward, out RaycastHit hit, ObstacleProbeDistance, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                StopWalking();
                return;
            }

            float step = Mathf.Min(_speed * seconds, toTarget.magnitude);
            Vector3 next = transform.position + toTarget.normalized * step;
            _hopPhase += HopRate * seconds;
            next.y = _homeY + Mathf.Abs(Mathf.Sin(_hopPhase)) * HopHeight;
            transform.position = next;
            _walkSecondsLeft -= seconds;
        }

        private void StopWalking()
        {
            _walking = false;
            Vector3 p = transform.position;
            p.y = _homeY;
            transform.position = p;
            _restSeconds = 1.5f + (float)_random.NextDouble() * 4f;
        }
    }
}
