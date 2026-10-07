using System;
using TramChanh.Core;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Lobby
{
    /// <summary>Dine-in order point on a table (GT-005). Knows nothing about seats or furniture.</summary>
    public sealed class TableOrderPoint : OrderPoint
    {
        [SerializeField] private int _tableId;
        public TableId TableId => _tableId > 0 ? new TableId(_tableId) : default;
        public override OrderOrigin Origin => TableId.IsValid ? OrderOrigin.ForTable(TableId) : default;

        public void Initialize(int interactableId, Transform interactionPoint, IOrderService orders, LobbyOrderController controller, TableId tableId)
        {
            if (!tableId.IsValid) { throw new ArgumentException("Table identity is required.", nameof(tableId)); }
            Configure(interactableId, interactionPoint, orders, controller);
            _tableId = tableId.Value;
        }
    }
}
