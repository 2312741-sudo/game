using System;
using System.Collections.Generic;

namespace TramChanh.Orders
{
    internal static class ItemRequestSnapshot
    {
        public static IReadOnlyList<ItemRequest> Copy(IReadOnlyList<ItemRequest> source)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var items = new ItemRequest[source.Count];
            for (int i = 0; i < items.Length; i++) { items[i] = source[i]; }
            return Array.AsReadOnly(items);
        }
    }
}
