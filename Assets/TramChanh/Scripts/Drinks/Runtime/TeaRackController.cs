using System;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Drinks.Runtime
{
    public sealed class TeaRackController : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private DrinkRecipe _recipe;
        [SerializeField] private TeaBagItem _bagPrefab;
        [SerializeField] private Transform[] _bagSlots;
        private TeaRackInventory _inventory;
        private TeaBagItem[] _bags;
        private IStallTicketQueue _tickets;
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public int Stock => _inventory?.Stock ?? 0;
        public int Capacity => _inventory?.Capacity ?? 0;

        public void Initialize(IStallTicketQueue tickets, IIdGenerator ids, IEventBus events)
        {
            if (_inventory != null)
            {
                throw new InvalidOperationException("A rack's scene-owned stock can only be initialized once.");
            }
            if (_balance == null || _bagPrefab == null || _bagSlots == null
                || _balance.TeaRackCapacity > _bagSlots.Length
                || _bagPrefab.HandGrip == null || _bagPrefab.PlacementPoint == null)
            {
                throw new InvalidOperationException("Tea rack requires balance data, a bag prefab and capacity-matched slots.");
            }
            _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }
            _inventory = new TeaRackInventory(_balance.TeaRackCapacity, _balance.TeaRackInitialStock, ids);
            _bags = new TeaBagItem[_inventory.Capacity];
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                DrinkPreparation preparation = _inventory.BagAt(i);
                if (preparation == null)
                {
                    continue;
                }
                if (_bagSlots[i] == null)
                {
                    throw new InvalidOperationException("Each stocked bag requires a placement slot.");
                }
                TeaBagItem bag = Instantiate(_bagPrefab, _bagSlots[i], false);
                bag.transform.localPosition = -bag.PlacementPoint.localPosition;
                bag.Initialize(preparation, _recipe, events);
                _bags[i] = bag;
            }
        }

        public InteractionQuery Query(InteractionContext context)
        {
            Availability available = DrinkActionGuards.Actor(context);
            if (available.IsAvailable)
            {
                available = _inventory == null ? Availability.Blocked("drink.rack.not_configured")
                    : _inventory.CanTake(context.Hands.Current == null);
                if (available.IsAvailable && (_tickets == null || !_tickets.HasPending(ItemKind.Drink)))
                {
                    available = Availability.Blocked("stall.no_ticket.drink");
                }
            }
            return new InteractionQuery(available, "drink.rack.take_bag");
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey));
                return;
            }
            Result result = _inventory.TryTakeBag(context.Hands.Current == null, _tickets, index => context.Hands.TryPickUp(_bags[index]));
            if (!result.IsSuccess)
            {
                context.Events.Publish(new ActionBlocked(Id, result.ReasonKey));
            }
        }
    }
}
