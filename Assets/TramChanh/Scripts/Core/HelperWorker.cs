using System;
using System.Collections.Generic;

namespace TramChanh.Core
{
    /// <summary>
    /// The helper takes orders at the tables for the player: after a short wait it picks up one waiting order, never more than
    /// one per <see cref="PauseSeconds"/>. Pure C#: the caller passes the game-clock time and does the real order commands.
    /// </summary>
    public sealed class HelperWorker
    {
        public const double DefaultDelaySeconds = 8d;
        public const double DefaultPauseSeconds = 4d;
        public const int WagePerNight = 30000;

        private readonly Dictionary<OrderId, double> _waiting = new Dictionary<OrderId, double>();
        private double _nextFreeAt;

        public HelperWorker(double delaySeconds = DefaultDelaySeconds, double pauseSeconds = DefaultPauseSeconds)
        {
            DelaySeconds = Math.Max(0d, delaySeconds);
            PauseSeconds = Math.Max(0d, pauseSeconds);
        }

        public double DelaySeconds { get; }
        public double PauseSeconds { get; }
        public int Waiting => _waiting.Count;

        /// <summary>An order is waiting to be taken. Seeing the same order again keeps the first time.</summary>
        public void Observe(OrderId id, double now)
        {
            if (!id.IsValid || _waiting.ContainsKey(id)) { return; }
            _waiting[id] = now;
        }

        /// <summary>The order is no longer waiting (taken by the player, failed, finished).</summary>
        public void Forget(OrderId id) { _waiting.Remove(id); }

        public void Clear() { _waiting.Clear(); }

        /// <summary>Returns the oldest waiting order that has waited long enough, and starts the pause. False when nothing is due.</summary>
        public bool TryNext(double now, out OrderId id)
        {
            id = default;
            if (now < _nextFreeAt) { return false; }
            double bestSince = double.MaxValue;
            foreach (KeyValuePair<OrderId, double> pair in _waiting)
            {
                if (now - pair.Value >= DelaySeconds && pair.Value < bestSince) { bestSince = pair.Value; id = pair.Key; }
            }
            if (!id.IsValid) { return false; }
            _waiting.Remove(id);
            _nextFreeAt = now + PauseSeconds;
            return true;
        }
    }
}
