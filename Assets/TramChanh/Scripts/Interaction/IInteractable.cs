using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Interaction
{
    public interface IInteractable
    {
        InteractableId Id { get; }
        Transform InteractionPoint { get; }
        InteractionQuery Query(InteractionContext context);
        void Execute(InteractionContext context);
        void OnHoldStarted(InteractionContext context) { }
        void OnHoldCancelled(InteractionContext context) { }
        void OnContinuousReleased(InteractionContext context, float heldSeconds) { }
    }
}
