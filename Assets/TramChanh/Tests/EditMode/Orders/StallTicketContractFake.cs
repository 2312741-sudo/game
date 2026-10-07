using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    public sealed class StallTicketContractFake : IStallTicketQueue
    {
        private OrderItemRef _binding;
        public IReadOnlyList<OrderId> Tickets { get; }
        public StallTicketContractFake(OrderItemRef binding) { _binding = binding; Tickets = new[] { binding.OrderId }; }
        public bool HasPending(ItemKind kind) => false;
        public Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparationId) => Result<OrderItemRef>.Fail("stall.no_ticket.drink");
        public bool IsBound(OrderItemRef item) => item.IsValid && item == _binding;
        public Result Release(OrderItemRef item)
        {
            if (!IsBound(item)) { return Result.Fail("stall.ticket.not_bound"); }
            _binding = default;
            return Result.Success();
        }
    }
}
