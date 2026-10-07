using System;
using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public sealed class StallTicketQueue : IStallTicketQueue
    {
        private readonly OrderService _orders;
        public IReadOnlyList<OrderId> Tickets => _orders.Tickets;
        public StallTicketQueue(OrderService orders) { _orders = orders ?? throw new ArgumentNullException(nameof(orders)); }

        public bool HasPending(ItemKind kind)
        {
            for (int ticket = 0; ticket < Tickets.Count; ticket++)
            {
                Order order = _orders.Find(Tickets[ticket]);
                for (int i = 0; i < order.MutableItems.Count; i++)
                {
                    OrderItem item = order.MutableItems[i];
                    if (item.Kind == kind && item.Status == OrderItemStatus.Pending) { return true; }
                }
            }
            return false;
        }

        public Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparationId)
        {
            if (!preparationId.IsValid) { return Result<OrderItemRef>.Fail("stall.ticket.preparation_invalid"); }
            if (kind != ItemKind.Drink && kind != ItemKind.Cake) { return Result<OrderItemRef>.Fail("stall.ticket.kind_invalid"); }
            if (_orders.HasPreparation(preparationId)) { return Result<OrderItemRef>.Fail("stall.ticket.preparation_bound"); }
            for (int ticket = 0; ticket < Tickets.Count; ticket++)
            {
                Order order = _orders.Find(Tickets[ticket]);
                for (int i = 0; i < order.MutableItems.Count; i++)
                {
                    OrderItem item = order.MutableItems[i];
                    if (item.Kind == kind && item.Status == OrderItemStatus.Pending)
                    {
                        return Result<OrderItemRef>.Success(_orders.Claim(order, item, preparationId));
                    }
                }
            }
            return Result<OrderItemRef>.Fail(kind == ItemKind.Drink ? "stall.no_ticket.drink" : "stall.no_ticket.cake");
        }

        public Result Release(OrderItemRef item) => _orders.Release(item);
        public bool IsBound(OrderItemRef item) => _orders.IsBound(item);
    }
}
