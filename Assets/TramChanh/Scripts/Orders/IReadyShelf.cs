using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IReadyShelf
    {
        OrderId NextReadyOrder { get; }
        Availability CanPlace(IPreparedItem item);
        Result PlaceReady(IPreparedItem item);
        bool Occupied(ItemKind kind);
        Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId orderId, ActorRef actor);
    }
}
