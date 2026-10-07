using System;
using TramChanh.Core;

namespace TramChanh.Interaction
{
    public readonly struct InteractionPromptChanged : IEquatable<InteractionPromptChanged>
    {
        public InteractableId Id { get; }
        public InteractionQuery Query { get; }
        public float Progress { get; }
        public bool IsHeldUse { get; }
        public bool IsVisible => (Id.IsValid || IsHeldUse) && Query.Availability.Status != AvailabilityStatus.Hidden;
        public InteractionPromptChanged(InteractableId id, InteractionQuery query, float progress, bool isHeldUse = false)
        {
            Id = id;
            Query = query;
            Progress = progress;
            IsHeldUse = isHeldUse;
        }
        public bool Equals(InteractionPromptChanged other) => Id == other.Id && Query.Equals(other.Query) && Progress == other.Progress && IsHeldUse == other.IsHeldUse;
        public override bool Equals(object obj) => obj is InteractionPromptChanged other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Query, Progress, IsHeldUse);
    }
}
