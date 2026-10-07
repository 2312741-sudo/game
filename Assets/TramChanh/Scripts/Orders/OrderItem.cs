using TramChanh.Core;

namespace TramChanh.Orders
{
    internal sealed class OrderItem : IReadOnlyOrderItem
    {
        public OrderItemId Id { get; }
        public string ItemDefinitionId { get; }
        public ItemKind Kind { get; }
        public OrderItemStatus Status { get; internal set; }
        public PreparationId PreparationId { get; internal set; }
        public int Quality { get; internal set; }

        public OrderItem(OrderItemId id, string definition, ItemKind kind)
        {
            Id = id;
            ItemDefinitionId = definition;
            Kind = kind;
        }
    }
}
