using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.DevTools.Drinks
{
    /// <summary>Isolated DRINK-001 development fixture. Creates no domain order.</summary>
    public sealed class DevelopmentPickupTicketQueue : IStallTicketQueue
    {
        private readonly Dictionary<PreparationId, OrderItemRef> _claims = new Dictionary<PreparationId, OrderItemRef>();
        private readonly IReadOnlyList<OrderId> _tickets = System.Array.Empty<OrderId>();
        private int _nextItem;
        public IReadOnlyList<OrderId> Tickets => _tickets;
        public bool HasPending(ItemKind kind) => Debug.isDebugBuild && kind == ItemKind.Drink;
        public Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparation)
        {
            if (!HasPending(kind) || !preparation.IsValid) { return Result<OrderItemRef>.Fail("stall.no_ticket.drink"); }
            var item = new OrderItemRef(new OrderId(1), new OrderItemId(++_nextItem), preparation);
            _claims.Add(preparation, item);
            return Result<OrderItemRef>.Success(item);
        }
        public bool IsBound(OrderItemRef item) => _claims.TryGetValue(item.PreparationId, out OrderItemRef current) && current == item;
        public Result Release(OrderItemRef item)
        {
            if (!IsBound(item)) { return Result.Fail("stall.ticket.not_bound"); }
            _claims.Remove(item.PreparationId);
            return Result.Success();
        }
    }
}
