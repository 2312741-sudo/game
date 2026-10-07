using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    internal sealed class Order : IReadOnlyOrder
    {
        public OrderId Id { get; }
        public OrderOrigin Origin { get; }
        public CustomerId CustomerId { get; }
        public OrderStatus Status { get; internal set; }
        public IReadOnlyList<ItemRequest> RequestedItems { get; }
        public IReadOnlyList<IReadOnlyOrderItem> Items { get; }
        public double CreatedAt { get; }
        public double SentAt { get; internal set; }
        public double StatusEnteredAt { get; internal set; }
        public FailureReason? FailureReason { get; internal set; }
        internal List<OrderItem> MutableItems { get; } = new List<OrderItem>();

        public Order(OrderId id, OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested, double createdAt)
        {
            Id = id;
            Origin = origin;
            CustomerId = customer;
            RequestedItems = ItemRequestSnapshot.Copy(requested);
            CreatedAt = createdAt;
            StatusEnteredAt = createdAt;
            Status = OrderStatus.WaitingForLobby;
            Items = MutableItems.AsReadOnly();
        }
    }
}
