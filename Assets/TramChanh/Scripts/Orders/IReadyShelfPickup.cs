using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IReadyShelfPickup
    {
        OrderId NextReadyOrder { get; }
        Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId orderId, ActorRef actor);
    }
}
