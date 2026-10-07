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
        public InteractionQuery HeldQuery { get; }
        public float HeldProgress { get; }
        public bool HasHeldPrompt => !IsHeldUse && HeldQuery.Availability.Status != AvailabilityStatus.Hidden;
        public bool IsVisible => (Id.IsValid || IsHeldUse) && Query.Availability.Status != AvailabilityStatus.Hidden;
        public InteractionPromptChanged(InteractableId id, InteractionQuery query, float progress, bool isHeldUse = false, InteractionQuery heldQuery = default, float heldProgress = 0f)
        {
            Id = id;
            Query = query;
            Progress = progress;
            IsHeldUse = isHeldUse;
            HeldQuery = heldQuery;
            HeldProgress = heldProgress;
        }
        public bool Equals(InteractionPromptChanged other) => Id == other.Id && Query.Equals(other.Query) && Progress == other.Progress && IsHeldUse == other.IsHeldUse && HeldQuery.Equals(other.HeldQuery) && HeldProgress == other.HeldProgress;
        public override bool Equals(object obj) => obj is InteractionPromptChanged other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Query, Progress, IsHeldUse, HeldQuery, HeldProgress);
    }
}
