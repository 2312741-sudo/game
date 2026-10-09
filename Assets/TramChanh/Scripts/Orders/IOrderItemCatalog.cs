namespace TramChanh.Orders
{
    /// <summary>Read-only recipe lookup for a live preparation binding, without Lobby commands.</summary>
    public interface IOrderItemCatalog
    {
        bool TryGetItemDefinition(OrderItemRef binding, out string itemDefinitionId);
    }
}
