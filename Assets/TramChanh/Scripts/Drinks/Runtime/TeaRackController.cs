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
        private IReadyShelfPlacement _readyGate;
        private IEventBus _events;
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public int Stock => _inventory?.Stock ?? 0;
        public int Capacity => _inventory?.Capacity ?? 0;

        public void Initialize(IStallTicketQueue tickets, IIdGenerator ids, IEventBus events)
        {
            InitializeStock(tickets, ids, events);
        }

        /// <summary>
        /// Same as the 3-argument overload, plus a Ready-slot gate: while the drink Ready slot is occupied the
        /// rack refuses to hand out a bag (and never claims a ticket), because a finished drink could not be
        /// placed and a held bag cannot be put down.
        /// </summary>
        public void Initialize(IStallTicketQueue tickets, IIdGenerator ids, IEventBus events, IReadyShelfPlacement readyGate)
        {
            if (readyGate == null)
            {
                throw new ArgumentNullException(nameof(readyGate));
            }
            InitializeStock(tickets, ids, events);
            _readyGate = readyGate;
        }

        private void InitializeStock(IStallTicketQueue tickets, IIdGenerator ids, IEventBus events)
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
            _events = events;
            _bags = new TeaBagItem[_inventory.Capacity];
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                if (_inventory.BagAt(i) != null)
                {
                    if (_bagSlots[i] == null)
                    {
                        throw new InvalidOperationException("Each stocked bag requires a placement slot.");
                    }
                    SpawnBag(i);
                }
            }
        }

        private void SpawnBag(int index)
        {
            // A restocked slot without its own anchor falls back to the rack root, so the domain bag always has a view.
            Transform parent = _bagSlots[index] != null ? _bagSlots[index] : transform;
            TeaBagItem bag = Instantiate(_bagPrefab, parent, false);
            bag.transform.localPosition = -bag.PlacementPoint.localPosition;
            bag.Initialize(_inventory.BagAt(index), _recipe, _events);
            _bags[index] = bag;
        }

        /// <summary>
        /// MAIN-102: restock pre-portioned bags from storage once the rack has no stored bag (quantity policy is
        /// DEC-015 TBD). Only slots whose bag is Ready or a discarded orphan are refilled, each with a fresh unbound
        /// bag; a bag still held or in preparation keeps its slot. No ticket is claimed and no event is published.
        /// The previous bag object is not touched: it belongs to the shelf, a served bundle, or was destroyed.
        /// </summary>
        public int RestockEmptySlots() => _inventory == null ? 0 : _inventory.RestockWhenEmpty(SpawnBag);

        public InteractionQuery Query(InteractionContext context)
        {
            RestockEmptySlots();
            Availability available = DrinkActionGuards.Actor(context);
            if (available.IsAvailable)
            {
                available = _inventory == null ? Availability.Blocked("drink.rack.not_configured")
                    : _inventory.CanTake(context.Hands.Current == null);
                if (available.IsAvailable && (_tickets == null || !_tickets.HasPending(ItemKind.Drink)))
                {
                    available = Availability.Blocked("stall.no_ticket.drink");
                }
                if (available.IsAvailable && _readyGate != null && _readyGate.Occupied(ItemKind.Drink))
                {
                    available = Availability.Blocked("ready.slot_full");
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
