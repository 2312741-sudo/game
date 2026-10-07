using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IReadyShelfPlacement
    {
        Availability CanPlace(IPreparedItem item);
        // Validate -> item state -> order -> slot; publish only after every participant commits.
        // Observer faults are reported after commit and cannot change the successful result.
        Result PlaceReady(IPreparedItem item);
        bool Occupied(ItemKind kind);
    }
}
