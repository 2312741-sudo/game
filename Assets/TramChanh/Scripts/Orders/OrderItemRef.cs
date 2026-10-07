using System;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct OrderItemRef : IEquatable<OrderItemRef>
    {
        public OrderId OrderId { get; }
        public OrderItemId OrderItemId { get; }
        public PreparationId PreparationId { get; }
        public bool IsValid => OrderId.IsValid && OrderItemId.IsValid && PreparationId.IsValid;

        public OrderItemRef(OrderId orderId, OrderItemId itemId, PreparationId preparationId)
        {
            if (!orderId.IsValid || !itemId.IsValid || !preparationId.IsValid)
            {
                throw new ArgumentException("A preparation binding requires three positive identities.");
            }
            OrderId = orderId;
            OrderItemId = itemId;
            PreparationId = preparationId;
        }

        public bool Equals(OrderItemRef other) => OrderId == other.OrderId && OrderItemId == other.OrderItemId && PreparationId == other.PreparationId;
        public override bool Equals(object obj) => obj is OrderItemRef other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(OrderId, OrderItemId, PreparationId);
        public static bool operator ==(OrderItemRef left, OrderItemRef right) => left.Equals(right);
        public static bool operator !=(OrderItemRef left, OrderItemRef right) => !left.Equals(right);
    }
}
