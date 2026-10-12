using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Lobby;
using TramChanh.Orders;

namespace TramChanh.Customers
{
    /// <summary>Adapter over a Lobby <see cref="OrderPoint"/>: every request goes through the point (Lobby intake).</summary>
    public sealed class OrderPointSeat : ICustomerSeat
    {
        public const string MissingPointReasonKey = "customers.seat.missing_point";
        private readonly OrderPoint _point;

        public OrderPointSeat(OrderPoint point, int tableNumber)
        {
            if (point == null) { throw new ArgumentNullException(nameof(point)); }
            if (tableNumber <= 0) { throw new ArgumentOutOfRangeException(nameof(tableNumber), "Table number must be positive."); }
            if (point is TableOrderPoint table && table.TableId.IsValid && table.TableId.Value != tableNumber)
            {
                throw new ArgumentException("Table number must match the point's TableId.", nameof(tableNumber));
            }
            _point = point;
            TableNumber = tableNumber;
        }

        public int TableNumber { get; }
        public OrderPoint Point => _point;
        public bool HasLiveOrder => _point != null && _point.ActiveOrder.IsValid;

        public Result<OrderId> Request(CustomerId customer, IReadOnlyList<ItemRequest> items)
        {
            if (_point == null) { return Result<OrderId>.Fail(MissingPointReasonKey); }
            return _point.RequestCustomerService(customer, items);
        }
    }
}
