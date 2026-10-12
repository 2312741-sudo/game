using System;
using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Moves a point along a polyline at constant speed, driven by an explicit (game-clock) delta.
    /// Passes exactly through every waypoint, never overshoots the last one, and snaps onto it once
    /// within <c>arriveRadius</c>. Deterministic: the same deltas always give the same positions.
    /// </summary>
    public sealed class PathFollower
    {
        private readonly Vector3[] _points;
        private readonly double[] _remainingAfter; // _remainingAfter[i] = length of the path from point i to the end
        private readonly double _speed;
        private readonly double _arriveRadius;
        private int _target;            // index of the point being walked towards
        private double _alongSegment;   // distance travelled from _points[_target - 1] towards _points[_target]

        /// <param name="path">At least one point; copied.</param>
        /// <param name="speed">Metres per second; finite and &gt; 0.</param>
        /// <param name="arriveRadius">Snap distance to the final point; finite and &gt;= 0.</param>
        public PathFollower(IReadOnlyList<Vector3> path, float speed, float arriveRadius = 0.05f)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Count == 0)
            {
                throw new ArgumentException("Path needs at least one point.", nameof(path));
            }

            if (!VectorMath.IsFinite(speed) || speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Speed must be finite and > 0.");
            }

            if (!VectorMath.IsFinite(arriveRadius) || arriveRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arriveRadius), arriveRadius, "Arrive radius must be finite and >= 0.");
            }

            _points = new Vector3[path.Count];
            for (int i = 0; i < _points.Length; i++)
            {
                if (!VectorMath.IsFinite(path[i]))
                {
                    throw new ArgumentException("Path point " + i + " is not finite.", nameof(path));
                }

                _points[i] = path[i];
            }

            _remainingAfter = new double[_points.Length];
            for (int i = _points.Length - 2; i >= 0; i--)
            {
                _remainingAfter[i] = _remainingAfter[i + 1] + VectorMath.Distance(_points[i], _points[i + 1]);
            }

            _speed = speed;
            _arriveRadius = arriveRadius;
            Position = _points[0];
            Forward = new Vector3(0f, 0f, 1f);
            for (int i = 1; i < _points.Length; i++)
            {
                if (VectorMath.TryHorizontalDirection(_points[i - 1], _points[i], out var dir))
                {
                    Forward = dir;
                    break;
                }
            }

            _target = 1;
            _alongSegment = 0d;
            if (_points.Length == 1 || _remainingAfter[0] <= _arriveRadius)
            {
                Finish();
            }
        }

        public Vector3 Position { get; private set; }

        /// <summary>Horizontal unit travel direction; keeps its last non-zero value (initially the first segment's).</summary>
        public Vector3 Forward { get; private set; }

        /// <summary>The configured speed while walking, 0 once arrived.</summary>
        public float CurrentSpeed => Arrived ? 0f : (float)_speed;

        public bool Arrived { get; private set; }

        /// <summary>Path length still to walk (0 once arrived).</summary>
        public float RemainingDistance
        {
            get
            {
                if (Arrived)
                {
                    return 0f;
                }

                double segment = VectorMath.Distance(_points[_target - 1], _points[_target]);
                return (float)(Math.Max(0d, segment - _alongSegment) + _remainingAfter[_target]);
            }
        }

        /// <summary>Advances by <paramref name="seconds"/>. Values &lt;= 0, NaN or infinity are ignored (pause-safe).</summary>
        public void Step(double seconds)
        {
            if (Arrived || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d)
            {
                return;
            }

            double budget = _speed * seconds;
            while (budget > 0d && _target < _points.Length)
            {
                Vector3 from = _points[_target - 1];
                Vector3 to = _points[_target];
                double segment = VectorMath.Distance(from, to);
                if (VectorMath.TryHorizontalDirection(from, to, out var dir))
                {
                    Forward = dir;
                }

                double left = segment - _alongSegment;
                if (budget >= left)
                {
                    budget -= left;
                    _target++;
                    _alongSegment = 0d;
                    Position = to;
                    continue;
                }

                _alongSegment += budget;
                budget = 0d;
                Position = VectorMath.Lerp(from, to, segment > 0d ? _alongSegment / segment : 1d);
            }

            if (_target >= _points.Length || RemainingDistance <= _arriveRadius)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (_points.Length > 1)
            {
                // Final heading: last horizontal segment, if any, so a snap does not leave a stale direction.
                for (int i = Math.Max(_target, 1); i < _points.Length; i++)
                {
                    if (VectorMath.TryHorizontalDirection(_points[i - 1], _points[i], out var dir))
                    {
                        Forward = dir;
                    }
                }
            }

            _target = _points.Length;
            _alongSegment = 0d;
            Position = _points[_points.Length - 1];
            Arrived = true;
        }
    }
}
