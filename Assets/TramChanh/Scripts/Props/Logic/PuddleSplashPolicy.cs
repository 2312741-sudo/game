using System.Collections.Generic;

namespace TramChanh.Props
{
    /// <summary>
    /// Decides whether an actor (player or vehicle, identified by an int key) moving in a puddle should splash now.
    /// Rules: must be moving at least <see cref="MinSpeed"/>; each actor has its own cooldown; leaving forgets the actor.
    /// </summary>
    public sealed class PuddleSplashPolicy
    {
        private readonly Dictionary<int, double> _nextAllowedAt = new Dictionary<int, double>();

        public PuddleSplashPolicy(float minSpeed = 0.5f, float cooldownSeconds = 0.35f, float fullSpeed = 6f)
        {
            MinSpeed = minSpeed;
            FullSpeed = fullSpeed > minSpeed ? fullSpeed : minSpeed + 0.01f;
            CooldownSeconds = cooldownSeconds;
        }

        public float MinSpeed { get; }
        public float FullSpeed { get; }
        public float CooldownSeconds { get; }
        public int TrackedActors => _nextAllowedAt.Count;

        public bool ShouldSplash(int actorKey, double now, float horizontalSpeed, out float intensity)
        {
            intensity = 0f;
            if (horizontalSpeed < MinSpeed) { return false; }
            if (_nextAllowedAt.TryGetValue(actorKey, out double next) && now < next) { return false; }
            _nextAllowedAt[actorKey] = now + CooldownSeconds;
            float t = (horizontalSpeed - MinSpeed) / (FullSpeed - MinSpeed);
            intensity = t < 0f ? 0f : (t > 1f ? 1f : t);
            return true;
        }

        public void Exit(int actorKey)
        {
            _nextAllowedAt.Remove(actorKey);
        }
    }
}
