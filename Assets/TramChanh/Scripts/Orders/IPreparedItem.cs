using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IPreparedItem
    {
        ItemKind Kind { get; }
        OrderItemRef BoundItem { get; }
        bool IsFinished { get; }
        int Quality { get; }
        Result MarkReady();
    }
}
