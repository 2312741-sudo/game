namespace TramChanh.Props
{
    /// <summary>Decides whether a collision is loud enough to raise a cue, with a cooldown so a bouncing prop is not spammy.</summary>
    public sealed class ImpactCuePolicy
    {
        private double _nextAllowedAt = double.NegativeInfinity;

        public ImpactCuePolicy(float minSpeed = 0.6f, float fullSpeed = 4f, float cooldownSeconds = 0.08f)
        {
            MinSpeed = minSpeed;
            FullSpeed = fullSpeed > minSpeed ? fullSpeed : minSpeed + 0.01f;
            CooldownSeconds = cooldownSeconds;
        }

        public float MinSpeed { get; }
        public float FullSpeed { get; }
        public float CooldownSeconds { get; }

        /// <summary>True when the impact speed (m/s, relative velocity magnitude) is above the threshold and not on cooldown.</summary>
        public bool ShouldPlay(double now, float impactSpeed, out float intensity)
        {
            intensity = 0f;
            if (impactSpeed < MinSpeed || now < _nextAllowedAt) { return false; }
            float t = (impactSpeed - MinSpeed) / (FullSpeed - MinSpeed);
            intensity = t < 0f ? 0f : (t > 1f ? 1f : t);
            _nextAllowedAt = now + CooldownSeconds;
            return true;
        }
    }
}
