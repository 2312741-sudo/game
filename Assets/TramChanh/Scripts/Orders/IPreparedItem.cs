using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IPreparedItem
    {
        ItemKind Kind { get; }
        OrderItemRef BoundItem { get; }
        bool IsFinished { get; }
        int Quality { get; }
        // Changes only preparation state on success. No events are published here;
        // the shelf publishes after its order and slot commits. Failure changes nothing.
        Result MarkReady();
    }
}
