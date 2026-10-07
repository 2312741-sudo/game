using System;

namespace TramChanh.Orders
{
    public readonly struct ItemRequest
    {
        public string ItemDefinitionId { get; }
        public int Quantity { get; }
        public ItemRequest(string itemDefinitionId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemDefinitionId) || quantity <= 0)
            {
                throw new ArgumentException("Item request requires an identity and positive quantity.");
            }
            ItemDefinitionId = itemDefinitionId;
            Quantity = quantity;
        }
    }
}
