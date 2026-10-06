using System;

namespace TramChanh.Core
{
    /// <summary>Deterministic elapsed seconds; paused advances are discarded.</summary>
    public sealed class ManualClock : IGameClock
    {
        public double Now { get; private set; }
        public double DeltaTime { get; private set; }
        public bool IsPaused { get; private set; }

        public void Advance(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "Elapsed seconds must be finite and non-negative.");
            }
            double next = Now + (IsPaused ? 0 : seconds);
            if (double.IsInfinity(next))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "Elapsed time exceeds the clock range.");
            }
            DeltaTime = IsPaused ? 0 : seconds;
            Now = next;
        }

        public void Pause()
        {
            IsPaused = true;
            DeltaTime = 0;
        }

        public void Resume()
        {
            IsPaused = false;
            DeltaTime = 0;
        }
    }
}
