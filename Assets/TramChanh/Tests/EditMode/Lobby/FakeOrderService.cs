using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Lobby
{
    /// <summary>Scriptable fake of the frozen <see cref="IOrderService"/>; records every call. Not a production service.</summary>
    public sealed class FakeOrderService : IOrderService
    {
        public sealed class FakeOrder : IReadOnlyOrder
        {
            public OrderId Id { get; set; }
            public OrderOrigin Origin { get; set; }
            public CustomerId CustomerId { get; set; }
            public OrderStatus Status { get; set; }
            public IReadOnlyList<ItemRequest> RequestedItems { get; set; }
            public IReadOnlyList<IReadOnlyOrderItem> Items { get; } = Array.Empty<IReadOnlyOrderItem>();
            public double CreatedAt { get; set; }
            public double SentAt { get; set; }
            public double StatusEnteredAt { get; set; }
            public FailureReason? FailureReason { get; set; }
        }

        private readonly Dictionary<int, FakeOrder> _orders = new Dictionary<int, FakeOrder>();
        private readonly IEventBus _events;
        private int _next = 1;

        public List<string> Calls { get; } = new List<string>();
        public List<ActorRef> BeginActors { get; } = new List<ActorRef>();
        public List<OrderOrigin> BeginPoints { get; } = new List<OrderOrigin>();
        public List<IReadOnlyList<ItemRequest>> Entered { get; } = new List<IReadOnlyList<ItemRequest>>();
        public string RequestFailure { get; set; }
        public string BeginFailure { get; set; }
        public string EnterFailure { get; set; }
        public string SendFailure { get; set; }

        public FakeOrderService(IEventBus events = null) { _events = events; }

        public IReadOnlyList<IReadOnlyOrder> Active => new List<IReadOnlyOrder>(_orders.Values);

        public IReadOnlyOrder Get(OrderId id) => id.IsValid && _orders.TryGetValue(id.Value, out FakeOrder order) ? order : null;

        public FakeOrder Order(OrderId id) => _orders[id.Value];

        public Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested)
        {
            Calls.Add("RequestService");
            if (RequestFailure != null) { return Result<OrderId>.Fail(RequestFailure); }
            var id = new OrderId(_next++);
            _orders[id.Value] = new FakeOrder { Id = id, Origin = origin, CustomerId = customer, Status = OrderStatus.WaitingForLobby, RequestedItems = requested };
            return Result<OrderId>.Success(id);
        }

        public Result BeginTaking(OrderId id, ActorRef actor, OrderOrigin point)
        {
            Calls.Add("BeginTaking");
            BeginActors.Add(actor);
            BeginPoints.Add(point);
            if (BeginFailure != null) { return Result.Fail(BeginFailure); }
            return Transition(id, OrderStatus.WaitingForLobby, OrderStatus.TakingOrder);
        }

        public Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered)
        {
            Calls.Add("Enter");
            Entered.Add(entered);
            if (EnterFailure != null) { return Result.Fail(EnterFailure); }
            return Transition(id, OrderStatus.TakingOrder, OrderStatus.Entered);
        }

        public Result SendToStall(OrderId id)
        {
            Calls.Add("SendToStall");
            if (SendFailure != null) { return Result.Fail(SendFailure); }
            return Transition(id, OrderStatus.Entered, OrderStatus.SentToStall);
        }

        public Result Fail(OrderId id, FailureReason reason)
        {
            Calls.Add("Fail");
            return Transition(id, Get(id)?.Status ?? OrderStatus.Failed, OrderStatus.Failed);
        }

        public void SetStatus(OrderId id, OrderStatus status)
        {
            Order(id).Status = status;
            _events?.Publish(new OrderStatusChanged(id, status));
        }

        private Result Transition(OrderId id, OrderStatus from, OrderStatus to)
        {
            if (!(Get(id) is FakeOrder order)) { return Result.Fail("order.not_found"); }
            if (order.Status != from) { return Result.Fail("order.transition.invalid"); }
            SetStatus(id, to);
            return Result.Success();
        }
    }
}
