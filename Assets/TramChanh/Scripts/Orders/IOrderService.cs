using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    /// <summary>Lobby command surface; preparation adapters receive only the ticket queue.</summary>
    public interface IOrderService
    {
        IReadOnlyList<IReadOnlyOrder> Active { get; }
        IReadOnlyOrder Get(OrderId id);
        Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested);
        Result BeginTaking(OrderId id, ActorRef actor, OrderOrigin point);
        Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered);
        Result SendToStall(OrderId id);
        Result PickUp(OrderId id, ActorRef actor);
        Result Fail(OrderId id, FailureReason reason);
    }
}
