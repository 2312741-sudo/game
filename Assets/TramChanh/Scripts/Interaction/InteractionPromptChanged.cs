using System;
using TramChanh.Core;

namespace TramChanh.Interaction
{
    public readonly struct InteractionPromptChanged : IEquatable<InteractionPromptChanged>
    {
        public InteractableId Id { get; }
        public InteractionQuery Query { get; }
        public float Progress { get; }
        public bool IsVisible => Id.IsValid && Query.Availability.Status != AvailabilityStatus.Hidden;
        public InteractionPromptChanged(InteractableId id, InteractionQuery query, float progress)
        {
            Id = id;
            Query = query;
            Progress = progress;
        }
        public bool Equals(InteractionPromptChanged other) => Id == other.Id && Query.Equals(other.Query) && Progress == other.Progress;
        public override bool Equals(object obj) => obj is InteractionPromptChanged other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Query, Progress);
    }
}
