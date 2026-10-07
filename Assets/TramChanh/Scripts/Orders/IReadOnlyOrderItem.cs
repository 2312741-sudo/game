using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IReadOnlyOrderItem
    {
        OrderItemId Id { get; }
        string ItemDefinitionId { get; }
        ItemKind Kind { get; }
        OrderItemStatus Status { get; }
        PreparationId PreparationId { get; }
        int Quality { get; }
    }
}
