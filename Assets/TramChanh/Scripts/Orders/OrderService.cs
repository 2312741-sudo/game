using System;
using System.Collections.Generic;
using TramChanh.Content;
using TramChanh.Core;

namespace TramChanh.Orders
{
    /// <summary>Canonical Lobby intake; preparation mutation is internal to ticket/shelf use cases.</summary>
    public sealed class OrderService : IOrderService
    {
        private readonly IGameClock _clock;
        private readonly IIdGenerator _ids;
        private readonly IContentDatabase _content;
        private readonly Dictionary<OrderId, Order> _orders = new Dictionary<OrderId, Order>();
        private readonly List<Order> _active = new List<Order>();
        private readonly List<OrderId> _tickets = new List<OrderId>();
        private readonly int _drinkCapacity;
        private readonly int _cakeCapacity;
        private readonly List<ReadyShelf> _shelves = new List<ReadyShelf>();
        private readonly Queue<Action> _outbox = new Queue<Action>();
        private readonly List<Exception> _observerFaults = new List<Exception>();
        private readonly Action<Exception> _observerFaultSink;
        private bool _flushing;
        public IReadOnlyList<IReadOnlyOrder> Active { get; }
        public IReadOnlyList<Exception> ObserverFaults { get; }
        internal IReadOnlyList<OrderId> Tickets { get; }
        internal IEventBus Events { get; }

        public OrderService(IGameClock clock, IEventBus events, IIdGenerator ids, IContentDatabase content, int drinkCapacity = 1, int cakeCapacity = 1, Action<Exception> observerFaultSink = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            _ids = ids ?? throw new ArgumentNullException(nameof(ids));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            if (drinkCapacity < 0 || cakeCapacity < 0) { throw new ArgumentOutOfRangeException(nameof(drinkCapacity)); }
            _drinkCapacity = drinkCapacity;
            _cakeCapacity = cakeCapacity;
            Active = _active.AsReadOnly();
            Tickets = _tickets.AsReadOnly();
            ObserverFaults = _observerFaults.AsReadOnly();
            _observerFaultSink = observerFaultSink;
        }

        public IReadOnlyOrder Get(OrderId id) => Find(id);
        internal Order Find(OrderId id) => _orders.TryGetValue(id, out Order order) ? order : null;
        internal int Capacity(ItemKind kind) => kind == ItemKind.Drink ? _drinkCapacity : kind == ItemKind.Cake ? _cakeCapacity : 0;

        public Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested)
        {
            if (!origin.IsValid || !customer.IsValid) { return Result<OrderId>.Fail("order.origin.invalid"); }
            Result validation = ValidateItems(requested, false);
            if (!validation.IsSuccess) { return Result<OrderId>.Fail(validation.ReasonKey); }
            foreach (Order active in _active)
            {
                if (active.Origin.Equals(origin)) { return Result<OrderId>.Fail("order.point.occupied"); }
            }
            var id = new OrderId(_ids.Next());
            var order = new Order(id, origin, customer, requested, _clock.Now);
            _orders.Add(id, order);
            _active.Add(order);
            PublishStatus(order);
            return Result<OrderId>.Success(id);
        }

        public Result BeginTaking(OrderId id, ActorRef actor, OrderOrigin point)
        {
            Order order = Find(id);
            if (actor.Value <= 0) { return Result.Fail("order.actor.invalid"); }
            if (order == null || order.Status != OrderStatus.WaitingForLobby) { return Result.Fail("order.transition.invalid"); }
            if (!point.IsValid || !order.Origin.Equals(point)) { return Result.Fail("order.point.wrong"); }
            SetStatus(order, OrderStatus.TakingOrder);
            PublishStatus(order);
            return Result.Success();
        }

        public Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered)
        {
            Order order = Find(id);
            if (order == null || order.Status != OrderStatus.TakingOrder) { return Result.Fail("order.transition.invalid"); }
            Result validation = ValidateItems(entered, true);
            if (!validation.IsSuccess) { return validation; }
            if (!MatchesRequest(order.RequestedItems, entered)) { return Result.Fail("order.items.request_mismatch"); }
            foreach (ItemRequest request in entered)
            {
                _content.TryGetKind(request.ItemDefinitionId, out ItemKind kind);
                for (int i = 0; i < request.Quantity; i++)
                {
                    order.MutableItems.Add(new OrderItem(new OrderItemId(_ids.Next()), request.ItemDefinitionId, kind));
                }
            }
            SetStatus(order, OrderStatus.Entered);
            PublishStatus(order);
            return Result.Success();
        }

        public Result SendToStall(OrderId id)
        {
            Order order = Find(id);
            if (order == null || order.Status != OrderStatus.Entered) { return Result.Fail("order.transition.invalid"); }
            order.SentAt = _clock.Now;
            SetStatus(order, OrderStatus.SentToStall);
            int index = 0;
            while (index < _tickets.Count && ComesBefore(Find(_tickets[index]), order)) { index++; }
            _tickets.Insert(index, id);
            PublishStatus(order);
            return Result.Success();
        }

        public Result Fail(OrderId id, FailureReason reason)
        {
            Order order = Find(id);
            if (order == null || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Failed)
            {
                return Result.Fail("order.transition.invalid");
            }
            if (reason != FailureReason.CustomerLeft && reason != FailureReason.CancelledByDebug) { return Result.Fail("order.failure.invalid"); }
            var released = new List<OrderItemRef>();
            foreach (OrderItem item in order.MutableItems)
            {
                if (item.PreparationId.IsValid) { released.Add(new OrderItemRef(order.Id, item.Id, item.PreparationId)); }
                item.PreparationId = default;
                item.Status = OrderItemStatus.Pending;
                item.Quality = 0;
            }
            order.FailureReason = reason;
            SetStatus(order, OrderStatus.Failed);
            _tickets.Remove(id);
            _active.Remove(order);
            // Shelf participants clear their slots before external observers see Failed.
            foreach (ReadyShelf shelf in _shelves) { shelf.ReleaseOrderSlots(id); }
            foreach (OrderItemRef binding in released) { Enqueue(new OrderItemStatusChanged(binding, OrderItemStatus.Pending)); }
            Enqueue(new OrderStatusChanged(order.Id, order.Status));
            FlushOutbox();
            return Result.Success();
        }

        internal bool IsBound(OrderItemRef binding)
        {
            if (!binding.IsValid) { return false; }
            Order order = Find(binding.OrderId);
            if (order == null || order.Status == OrderStatus.Failed) { return false; }
            OrderItem item = FindItem(order, binding.OrderItemId);
            return item != null && item.PreparationId == binding.PreparationId && item.Status != OrderItemStatus.Pending;
        }

        internal OrderItem BoundItem(OrderItemRef binding) => IsBound(binding) ? FindItem(Find(binding.OrderId), binding.OrderItemId) : null;

        internal bool HasPreparation(PreparationId preparation)
        {
            foreach (Order order in _active)
            {
                foreach (OrderItem item in order.MutableItems)
                {
                    if (item.PreparationId == preparation) { return true; }
                }
            }
            return false;
        }

        internal OrderItemRef Claim(Order order, OrderItem item, PreparationId preparation)
        {
            item.PreparationId = preparation;
            item.Status = OrderItemStatus.InPreparation;
            bool advanced = order.Status == OrderStatus.SentToStall;
            if (advanced) { SetStatus(order, OrderStatus.InPreparation); }
            var binding = new OrderItemRef(order.Id, item.Id, preparation);
            Enqueue(new OrderItemStatusChanged(binding, item.Status));
            if (advanced) { Enqueue(new OrderStatusChanged(order.Id, order.Status)); }
            FlushOutbox();
            return binding;
        }

        internal Result Release(OrderItemRef binding)
        {
            OrderItem item = BoundItem(binding);
            if (item == null || item.Status != OrderItemStatus.InPreparation) { return Result.Fail("stall.ticket.not_bound"); }
            item.PreparationId = default;
            item.Status = OrderItemStatus.Pending;
            item.Quality = 0;
            Enqueue(new OrderItemStatusChanged(binding, item.Status));
            FlushOutbox();
            return Result.Success();
        }

        internal bool CommitReady(OrderItemRef binding, int quality)
        {
            OrderItem item = BoundItem(binding);
            Order order = Find(binding.OrderId);
            if (item == null || item.Status != OrderItemStatus.InPreparation || order.Status != OrderStatus.InPreparation)
            {
                throw new InvalidOperationException("A validated shelf binding changed during MarkReady.");
            }
            item.Status = OrderItemStatus.Ready;
            item.Quality = quality;
            foreach (OrderItem candidate in order.MutableItems)
            {
                if (candidate.Status != OrderItemStatus.Ready) { return false; }
            }
            SetStatus(order, OrderStatus.Ready);
            _tickets.Remove(order.Id);
            return true;
        }

        internal void PublishReady(OrderItemRef binding, bool orderBecameReady)
        {
            Enqueue(new OrderItemStatusChanged(binding, OrderItemStatus.Ready));
            if (orderBecameReady) { Enqueue(new OrderStatusChanged(binding.OrderId, OrderStatus.Ready)); }
            FlushOutbox();
        }

        internal Result PickUp(OrderId id, ActorRef actor)
        {
            Order order = Find(id);
            if (actor.Value <= 0) { return Result.Fail("order.actor.invalid"); }
            if (order == null || order.Status != OrderStatus.Ready) { return Result.Fail("order.transition.invalid"); }
            SetStatus(order, OrderStatus.PickedUpByLobby);
            return Result.Success();
        }

        internal void PublishStatus(Order order)
        {
            Enqueue(new OrderStatusChanged(order.Id, order.Status));
            FlushOutbox();
        }

        internal void RegisterShelf(ReadyShelf shelf) => _shelves.Add(shelf);
        internal void UnregisterShelf(ReadyShelf shelf) => _shelves.Remove(shelf);

        private void Enqueue<TEvent>(TEvent message) where TEvent : struct => _outbox.Enqueue(() => Events.Publish(message));

        private void FlushOutbox()
        {
            if (_flushing) { return; }
            _flushing = true;
            try
            {
                while (_outbox.Count > 0)
                {
                    Action publish = _outbox.Dequeue();
                    try { publish(); }
                    catch (Exception fault)
                    {
                        _observerFaults.Add(fault);
                        try { _observerFaultSink?.Invoke(fault); }
                        catch (Exception sinkFault) { _observerFaults.Add(sinkFault); }
                    }
                }
            }
            finally { _flushing = false; }
        }

        private void SetStatus(Order order, OrderStatus status) { order.Status = status; order.StatusEnteredAt = _clock.Now; }

        private Result ValidateItems(IReadOnlyList<ItemRequest> requests, bool requireCapacity)
        {
            if (requests == null || requests.Count == 0) { return Result.Fail("order.items.empty"); }
            long drinks = 0;
            long cakes = 0;
            foreach (ItemRequest request in requests)
            {
                if (request.Quantity <= 0 || !_content.TryGetKind(request.ItemDefinitionId, out ItemKind kind)) { return Result.Fail("order.items.invalid"); }
                if (kind == ItemKind.Drink) { drinks += request.Quantity; }
                else if (kind == ItemKind.Cake) { cakes += request.Quantity; }
                else { return Result.Fail("order.items.invalid"); }
            }
            return requireCapacity && (drinks > _drinkCapacity || cakes > _cakeCapacity)
                ? Result.Fail("order.too_many_for_shelf") : Result.Success();
        }

        private static bool MatchesRequest(IReadOnlyList<ItemRequest> requested, IReadOnlyList<ItemRequest> entered)
        {
            var quantities = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (ItemRequest request in requested)
            {
                quantities.TryGetValue(request.ItemDefinitionId, out long previous);
                quantities[request.ItemDefinitionId] = previous + request.Quantity;
            }
            foreach (ItemRequest request in entered)
            {
                if (!quantities.TryGetValue(request.ItemDefinitionId, out long remaining)) { return false; }
                quantities[request.ItemDefinitionId] = remaining - request.Quantity;
            }
            foreach (long remaining in quantities.Values) { if (remaining != 0) { return false; } }
            return true;
        }

        internal static OrderItem FindItem(Order order, OrderItemId id)
        {
            foreach (OrderItem item in order.MutableItems) { if (item.Id == id) { return item; } }
            return null;
        }

        private static bool ComesBefore(Order first, Order second) => first.SentAt < second.SentAt || (first.SentAt == second.SentAt && first.Id.Value < second.Id.Value);
    }
}
