using System;
using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    /// <summary>Ready placement transaction; event observers see item, order and shelf fully committed.</summary>
    public sealed class ReadyShelf : IReadyShelf, IDisposable
    {
        private readonly OrderService _orders;
        private readonly IStallTicketQueue _queue;
        private readonly IPreparedItem[][] _slots;
        private readonly OrderItemRef[][] _bindings;

        public ReadyShelf(OrderService orders, IStallTicketQueue queue, int drinkCapacity, int cakeCapacity)
        {
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            if (drinkCapacity != orders.Capacity(ItemKind.Drink) || cakeCapacity != orders.Capacity(ItemKind.Cake))
            {
                throw new ArgumentException("Entry limits and ready-shelf capacities must agree.");
            }
            _slots = new[] { new IPreparedItem[drinkCapacity], new IPreparedItem[cakeCapacity] };
            _bindings = new[] { new OrderItemRef[drinkCapacity], new OrderItemRef[cakeCapacity] };
            _orders.RegisterShelf(this);
        }

        public OrderId NextReadyOrder
        {
            get
            {
                Order oldest = null;
                for (int kind = 0; kind < _slots.Length; kind++)
                {
                    for (int slot = 0; slot < _slots[kind].Length; slot++)
                    {
                        IPreparedItem item = _slots[kind][slot];
                        if (item == null) { continue; }
                        Order order = _orders.Find(_bindings[kind][slot].OrderId);
                        if (order == null || order.Status != OrderStatus.Ready) { continue; }
                        if (oldest == null || order.StatusEnteredAt < oldest.StatusEnteredAt ||
                            (order.StatusEnteredAt == oldest.StatusEnteredAt && order.Id.Value < oldest.Id.Value))
                        {
                            oldest = order;
                        }
                    }
                }
                return oldest?.Id ?? default;
            }
        }

        public Availability CanPlace(IPreparedItem item)
        {
            if (item == null || !item.IsFinished) { return Availability.Blocked("ready.not_finished"); }
            if (!_queue.IsBound(item.BoundItem)) { return Availability.Blocked("ready.no_order"); }
            OrderItem bound = _orders.BoundItem(item.BoundItem);
            if (bound == null || bound.Kind != item.Kind) { return Availability.Blocked("ready.no_order"); }
            if (bound.Status == OrderItemStatus.Ready) { return Availability.Blocked("ready.already_ready"); }
            if (item.Quality < 0 || item.Quality > 100) { return Availability.Blocked("ready.quality.invalid"); }
            return FreeSlot(item.Kind) < 0 ? Availability.Blocked("ready.slot_full") : Availability.Available;
        }

        public Result PlaceReady(IPreparedItem item)
        {
            Availability availability = CanPlace(item);
            if (!availability.IsAvailable) { return Result.Fail(availability.ReasonKey); }
            ItemKind kind = item.Kind;
            OrderItemRef binding = item.BoundItem;
            int quality = item.Quality;
            int slot = FreeSlot(kind);
            Result prepared = item.MarkReady();
            if (!prepared.IsSuccess) { return prepared; }
            if (item.Kind != kind || item.BoundItem != binding || item.Quality != quality || !item.IsFinished)
            {
                throw new InvalidOperationException("MarkReady violated the prepared-item contract.");
            }
            bool orderReady = _orders.CommitReady(binding, quality);
            _slots[(int)kind][slot] = item;
            _bindings[(int)kind][slot] = binding;
            _orders.PublishReady(binding, orderReady);
            return Result.Success();
        }

        public bool Occupied(ItemKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= _slots.Length) { return false; }
            for (int i = 0; i < _slots[index].Length; i++) { if (_slots[index][i] != null) { return true; } }
            return false;
        }

        public Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId orderId, ActorRef actor)
        {
            if (actor.Value <= 0) { return Result<IReadOnlyList<IPreparedItem>>.Fail("order.actor.invalid"); }
            Order order = _orders.Find(orderId);
            if (order == null || order.Status != OrderStatus.Ready || orderId != NextReadyOrder)
            {
                return Result<IReadOnlyList<IPreparedItem>>.Fail("ready.no_complete_order");
            }
            var items = new List<IPreparedItem>();
            for (int kind = 0; kind < _slots.Length; kind++)
            {
                for (int slot = 0; slot < _slots[kind].Length; slot++)
                {
                    IPreparedItem item = _slots[kind][slot];
                    if (item != null && _bindings[kind][slot].OrderId == orderId) { items.Add(item); }
                }
            }
            if (items.Count != order.Items.Count) { throw new InvalidOperationException("A Ready order must have every item on the shelf."); }
            items.Sort((first, second) => first.BoundItem.OrderItemId.Value.CompareTo(second.BoundItem.OrderItemId.Value));
            Result pickup = _orders.PickUp(orderId, actor);
            if (!pickup.IsSuccess) { throw new InvalidOperationException("A validated ready order changed before pickup commit."); }
            ReleaseOrderSlots(orderId);
            _orders.PublishStatus(order);
            return Result<IReadOnlyList<IPreparedItem>>.Success(items.AsReadOnly());
        }

        public void Dispose() => _orders.UnregisterShelf(this);

        private int FreeSlot(ItemKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= _slots.Length) { return -1; }
            for (int i = 0; i < _slots[index].Length; i++) { if (_slots[index][i] == null) { return i; } }
            return -1;
        }

        internal void ReleaseOrderSlots(OrderId id)
        {
            for (int kind = 0; kind < _slots.Length; kind++)
            {
                for (int slot = 0; slot < _slots[kind].Length; slot++)
                {
                    IPreparedItem item = _slots[kind][slot];
                    if (item != null && _bindings[kind][slot].OrderId == id)
                    {
                        _slots[kind][slot] = null;
                        _bindings[kind][slot] = default;
                    }
                }
            }
        }
    }
}
