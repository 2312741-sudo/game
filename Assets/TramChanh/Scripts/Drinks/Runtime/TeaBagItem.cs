using System;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using UnityEngine;

namespace TramChanh.Drinks.Runtime
{
    /// <summary>Pickup-only view of pre-portioned tea; no measuring or held-use action.</summary>
    public sealed class TeaBagItem : MonoBehaviour, IHoldable
    {
        [SerializeField] private Transform _handGrip;
        [SerializeField] private Transform _placementPoint;
        private TeaBagPickup _pickup;
        public Transform HandGrip => _handGrip;
        public Transform PlacementPoint => _placementPoint;
        public TeaBagState State => _pickup.State;

        public void Initialize(TeaBagPickup pickup)
        {
            _pickup = pickup ?? throw new ArgumentNullException(nameof(pickup));
        }

        public void OnPickedUp(IHeldItemSlot hands)
        {
            if (!ReferenceEquals(hands.Current, this) || !_pickup.TryPickUp().IsSuccess)
            {
                throw new InvalidOperationException("Only a stored bag can enter its receiving held slot.");
            }
        }

        public void OnReleased()
        {
            // Placing/returning is outside DRINK-001. Held is terminal for this slice.
        }
    }
}
