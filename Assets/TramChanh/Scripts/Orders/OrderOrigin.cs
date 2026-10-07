using System;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct OrderOrigin : IEquatable<OrderOrigin>
    {
        public OrderType Type { get; }
        public TableId TableId { get; }
        public VehicleId VehicleId { get; }
        public bool IsValid => Type == OrderType.DineIn ? TableId.IsValid && !VehicleId.IsValid : VehicleId.IsValid && !TableId.IsValid;
        private OrderOrigin(OrderType type, TableId table, VehicleId vehicle) { Type = type; TableId = table; VehicleId = vehicle; }
        public static OrderOrigin ForTable(TableId table)
        {
            if (!table.IsValid) { throw new ArgumentException("Table identity is required.", nameof(table)); }
            return new OrderOrigin(OrderType.DineIn, table, default);
        }
        public static OrderOrigin ForVehicle(VehicleId vehicle)
        {
            if (!vehicle.IsValid) { throw new ArgumentException("Vehicle identity is required.", nameof(vehicle)); }
            return new OrderOrigin(OrderType.TakeawayVehicle, default, vehicle);
        }
        public bool Equals(OrderOrigin other) => Type == other.Type && TableId == other.TableId && VehicleId == other.VehicleId;
        public override bool Equals(object obj) => obj is OrderOrigin other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, TableId, VehicleId);
    }
}
