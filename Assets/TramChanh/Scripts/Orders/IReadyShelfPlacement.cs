using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IReadyShelfPlacement
    {
        Availability CanPlace(IPreparedItem item);
        Result PlaceReady(IPreparedItem item);
        bool Occupied(ItemKind kind);
    }
}
