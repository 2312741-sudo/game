using System;

namespace TramChanh.Effects
{
    /// <summary>
    /// A 0..1 level that moves toward a target at a fixed speed (linear ramp). Used for the rain intensity and the steam
    /// strength so an effect fades in and out instead of switching on or off in one frame. Pure C#, no Unity types.
    /// </summary>
    public sealed class SmoothedLevel
    {
        private readonly float _riseSeconds;
        private readonly float _fallSeconds;

        public float Current { get; private set; }
        public float Target { get; private set; }

        /// <summary>True when the level has reached its target.</summary>
        public bool IsSettled => Current == Target;

        /// <param name="riseSeconds">Seconds to go from 0 to 1. Zero or less means jump at once.</param>
        /// <param name="fallSeconds">Seconds to go from 1 to 0. Zero or less means jump at once.</param>
        public SmoothedLevel(float riseSeconds, float fallSeconds, float initial = 0f)
        {
            _riseSeconds = riseSeconds;
            _fallSeconds = fallSeconds;
            Current = Clamp01(initial);
            Target = Current;
        }

        public void SetTarget(float target)
        {
            Target = Clamp01(target);
        }

        /// <summary>Moves the level toward the target. A non-positive step changes nothing.</summary>
        public void Step(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Current == Target)
            {
                return;
            }
            if (Current < Target)
            {
                Current = _riseSeconds <= 0f ? Target : Math.Min(Target, Current + deltaSeconds / _riseSeconds);
            }
            else
            {
                Current = _fallSeconds <= 0f ? Target : Math.Max(Target, Current - deltaSeconds / _fallSeconds);
            }
        }

        /// <summary>Jumps to the target at once (used when an effect is created already in its final state).</summary>
        public void Snap()
        {
            Current = Target;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }
    }
}
