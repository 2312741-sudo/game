using UnityEngine;

namespace TramChanh.Interaction
{
    public interface IHoldable
    {
        Transform HandGrip { get; }
        void OnPickedUp(IHeldItemSlot hands);
        void OnReleased();
    }
}
