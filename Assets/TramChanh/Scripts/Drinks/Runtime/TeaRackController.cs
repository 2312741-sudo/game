using System;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using UnityEngine;

namespace TramChanh.Drinks.Runtime
{
    public sealed class TeaRackController : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private TeaBagItem _bagPrefab;
        [SerializeField] private Transform[] _bagSlots;
        [SerializeField, Tooltip("DRINK-001 test-scene exception only; production must bind a Lobby ticket.")]
        private bool _allowUnboundPickupForTest;
        private TeaRackInventory _inventory;
        private TeaBagItem[] _bags;
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public int Stock => _inventory?.Stock ?? 0;
        public int Capacity => _inventory?.Capacity ?? 0;

        private void Awake()
        {
            if (_balance == null || _bagPrefab == null || _bagSlots == null ||
                _balance.TeaRackCapacity > _bagSlots.Length || _bagPrefab.HandGrip == null || _bagPrefab.PlacementPoint == null)
            {
                throw new InvalidOperationException("Tea rack requires balance data, a bag prefab and capacity-matched slots.");
            }
            _inventory = new TeaRackInventory(_balance.TeaRackCapacity, _balance.TeaRackInitialStock);
            _bags = new TeaBagItem[_inventory.Capacity];
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                TeaBagPickup pickup = _inventory.BagAt(i);
                if (pickup == null)
                {
                    continue;
                }
                TeaBagItem bag = Instantiate(_bagPrefab, _bagSlots[i], false);
                bag.transform.localPosition = -bag.PlacementPoint.localPosition;
                bag.Initialize(pickup);
                _bags[i] = bag;
            }
        }

        public InteractionQuery Query(InteractionContext context)
        {
            Availability available;
            if (_inventory == null)
            {
                available = Availability.Blocked("drink.rack.not_configured");
            }
            else if ((context.Role & ActorRole.Stall) == 0)
            {
                available = Availability.Blocked("interaction.stall_role_required");
            }
            else if (context.Clock.IsPaused)
            {
                available = Availability.Blocked("interaction.paused");
            }
            else
            {
                available = _inventory.CanTake(context.Hands.Current == null);
                if (available.IsAvailable && !AllowsUnboundTestPickup())
                {
                    available = Availability.Blocked("stall.no_ticket.drink");
                }
            }
            return new InteractionQuery(available, "drink.rack.take_bag");
        }

        private bool AllowsUnboundTestPickup()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return _allowUnboundPickupForTest;
#else
            return false;
#endif
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey));
                return;
            }
            int index = _inventory.NextStoredIndex();
            if (!context.Hands.TryPickUp(_bags[index]))
            {
                context.Events.Publish(new ActionBlocked(Id, "hands.full"));
            }
            // TeaBagItem transitions the domain bag; only Stored bags count as stock.
            // The HeldItemChanged subscriber moves the same bag from its slot to HandSocket.
        }
    }
}
