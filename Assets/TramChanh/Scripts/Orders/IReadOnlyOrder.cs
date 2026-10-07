using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IReadOnlyOrder
    {
        OrderId Id { get; }
        OrderOrigin Origin { get; }
        CustomerId CustomerId { get; }
        OrderStatus Status { get; }
        IReadOnlyList<ItemRequest> RequestedItems { get; }
        IReadOnlyList<IReadOnlyOrderItem> Items { get; }
        double CreatedAt { get; }
        double SentAt { get; }
        double StatusEnteredAt { get; }
        FailureReason? FailureReason { get; }
    }
}
