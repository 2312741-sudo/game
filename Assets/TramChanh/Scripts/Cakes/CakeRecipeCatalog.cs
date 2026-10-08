using TramChanh.Content;
using System;
using System.Collections.Generic;
using TramChanh.Orders;

namespace TramChanh.Cakes
{
    public interface ICakeRecipeCatalog { bool TryResolve(OrderItemRef binding, out CakeRecipe recipe); }
    public sealed class CakeRecipeCatalog : ICakeRecipeCatalog
    {
        private readonly IOrderItemCatalog _items;
        private readonly IReadOnlyList<CakeRecipe> _recipes;
        public CakeRecipeCatalog(IOrderItemCatalog items, IReadOnlyList<CakeRecipe> recipes)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
        }
        public bool TryResolve(OrderItemRef binding, out CakeRecipe recipe)
        {
            recipe = null;
            if (!_items.TryGetItemDefinition(binding, out string definitionId)) { return false; }
            foreach (var candidate in _recipes)
            {
                if (candidate != null && candidate.IsConfigured && candidate.ItemDefinition.Id == definitionId)
                { recipe = candidate; return true; }
            }
            return false;
        }
    }
}
