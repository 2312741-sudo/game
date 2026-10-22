using UnityEngine;

namespace TramChanh.Props
{
    public enum PropCueKind
    {
        DebrisImpact = 0,
        PuddleSplash = 1,
        HangingNudge = 2,
    }

    /// <summary>
    /// Cue raised by physics props. Audio / Effects subscribe to it (IEventBus.Subscribe&lt;PropCueRaised&gt;) and decide
    /// what to play; Props never references those assemblies. Intensity is 0..1.
    /// </summary>
    public readonly struct PropCueRaised
    {
        public PropCueRaised(PropCueKind kind, Vector3 position, float intensity)
        {
            Kind = kind;
            Position = position;
            Intensity = intensity;
        }

        public PropCueKind Kind { get; }
        public Vector3 Position { get; }
        public float Intensity { get; }
    }
}
