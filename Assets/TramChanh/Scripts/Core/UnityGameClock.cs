using UnityEngine;

namespace TramChanh.Core
{
    /// <summary>
    /// Scene-local elapsed time. The composition root ticks once per Update, before
    /// timer consumers. Pause is explicit and does not modify Unity's global time scale.
    /// </summary>
    public sealed class UnityGameClock : IGameClock
    {
        private readonly ManualClock _clock = new ManualClock();
        public double Now => _clock.Now;
        public double DeltaTime => _clock.DeltaTime;
        public bool IsPaused => _clock.IsPaused;

        public void Tick()
        {
            _clock.Advance(Time.unscaledDeltaTime);
        }

        public void Pause()
        {
            _clock.Pause();
        }

        public void Resume()
        {
            _clock.Resume();
        }
    }
}
