using System;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct DeliveryTarget
    {
        public OrderOrigin Origin { get; }
        public CustomerId Customer { get; }
        public DeliveryTarget(OrderOrigin origin, CustomerId customer)
        {
            if (!origin.IsValid || !customer.IsValid) { throw new ArgumentException("Delivery requires a valid point and customer."); }
            Origin = origin;
            Customer = customer;
        }
    }
}
