using System;
using TramChanh.Core;

namespace TramChanh.Interaction
{
    public readonly struct InteractionQuery : IEquatable<InteractionQuery>
    {
        public Availability Availability { get; }
        public string PromptKey { get; }
        public string BlockedReasonKey => Availability.ReasonKey;
        public InteractionKind Kind { get; }
        public float HoldDuration { get; }

        public InteractionQuery(Availability availability, string promptKey, InteractionKind kind = InteractionKind.Press, float holdDuration = 0f)
        {
            if (kind == InteractionKind.Hold && (holdDuration <= 0f || float.IsNaN(holdDuration) || float.IsInfinity(holdDuration)))
            {
                throw new ArgumentOutOfRangeException(nameof(holdDuration));
            }
            Availability = availability;
            PromptKey = promptKey;
            Kind = kind;
            HoldDuration = holdDuration;
        }

        public bool Equals(InteractionQuery other)
        {
            return Availability.Status == other.Availability.Status && BlockedReasonKey == other.BlockedReasonKey
                && PromptKey == other.PromptKey && Kind == other.Kind && HoldDuration == other.HoldDuration;
        }
        public override bool Equals(object obj) => obj is InteractionQuery other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Availability.Status, BlockedReasonKey, PromptKey, Kind, HoldDuration);
    }
}
