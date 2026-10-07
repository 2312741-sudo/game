using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IStallTicketQueue
    {
        IReadOnlyList<OrderId> Tickets { get; }
        bool HasPending(ItemKind kind);
        Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparationId);
        Result Release(OrderItemRef item);
        bool IsBound(OrderItemRef item);
    }
}
