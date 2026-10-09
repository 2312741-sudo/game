using System;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct DeliveryTarget : IEquatable<DeliveryTarget>
    {
        public OrderOrigin Origin { get; }
        public CustomerId Customer { get; }
        public bool IsValid => Origin.IsValid && Customer.IsValid;
        public DeliveryTarget(OrderOrigin origin, CustomerId customer)
        {
            if (!origin.IsValid || !customer.IsValid) { throw new ArgumentException("Delivery requires a valid point and customer."); }
            Origin = origin;
            Customer = customer;
        }
        public bool Equals(DeliveryTarget other) => Origin.Equals(other.Origin) && Customer == other.Customer;
        public override bool Equals(object obj) => obj is DeliveryTarget other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Origin, Customer);
        public static bool operator ==(DeliveryTarget left, DeliveryTarget right) => left.Equals(right);
        public static bool operator !=(DeliveryTarget left, DeliveryTarget right) => !left.Equals(right);
    }
}
