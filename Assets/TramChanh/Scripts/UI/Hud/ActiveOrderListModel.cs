using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.UI.Hud
{
    /// <summary>One live order as the player sees it: where it is from, what it holds, its stage and where to go next.</summary>
    public sealed class OrderRow
    {
        internal OrderRow(OrderId id, OrderStatus status, HudText origin, HudText stage, IReadOnlyList<HudText> items, HudText next,
            int readyCount, int itemCount, bool isPrimary)
        {
            Id = id;
            Status = status;
            Origin = origin;
            Stage = stage;
            Items = items;
            NextStep = next;
            ReadyCount = readyCount;
            ItemCount = itemCount;
            IsPrimary = isPrimary;
        }

        public OrderId Id { get; }
        public OrderStatus Status { get; }
        public HudText Origin { get; }
        /// <summary>"order.status.*", with ready/total progress while the stall works on it.</summary>
        public HudText Stage { get; }
        public IReadOnlyList<HudText> Items { get; }
        public HudText NextStep { get; }
        public int ReadyCount { get; }
        public int ItemCount { get; }
        /// <summary>The single order the HUD recommends acting on now.</summary>
        public bool IsPrimary { get; }
        public HudText Header => HudText.Of(HudKeys.OrderHeader, Origin, Stage);
    }

    /// <summary>
    /// Engine-free projection of <see cref="IOrderService.Active"/> (read-only queries only) refreshed by
    /// Orders events. It never calls an order command.
    /// </summary>
    public sealed class ActiveOrderListModel : IDisposable
    {
        public const int MaxRows = 4;
        private readonly IOrderService _orders;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private IReadOnlyList<OrderRow> _rows = Array.Empty<OrderRow>();
        private bool _disposed;

        public ActiveOrderListModel(IOrderService orders, IEventBus events)
        {
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            _subscriptions.Add(events.Subscribe<OrderStatusChanged>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<OrderItemStatusChanged>(_ => Refresh()));
            Refresh();
        }

        public IReadOnlyList<OrderRow> Rows => _rows;
        /// <summary>Live orders beyond <see cref="MaxRows"/>.</summary>
        public int HiddenCount { get; private set; }
        public OrderRow Primary { get; private set; }
        /// <summary>"Next: ..." for the primary order, or the idle hint when there is no order.</summary>
        public HudText PrimaryNextStep => Primary != null ? HudText.Of(HudKeys.NextPrefix, Primary.NextStep) : HudText.Of(HudKeys.OrdersEmpty);
        public HudText Title => HudText.Of(HudKeys.OrdersTitle, _rows.Count + HiddenCount);
        public int Version { get; private set; }
        public Exception LastFault { get; private set; }
        public event Action Changed;

        /// <summary>Rebuilds from the live service. Never throws into the event publisher.</summary>
        public void Refresh()
        {
            if (_disposed) { return; }
            try
            {
                Build();
            }
            catch (Exception fault)
            {
                LastFault = fault;
                return;
            }
            Version++;
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) { return; }
            _disposed = true;
            foreach (IDisposable subscription in _subscriptions) { subscription.Dispose(); }
            _subscriptions.Clear();
        }

        private void Build()
        {
            IReadOnlyList<IReadOnlyOrder> active = _orders.Active ?? Array.Empty<IReadOnlyOrder>();
            IReadOnlyOrder primary = null;
            int bestPriority = int.MaxValue;
            foreach (IReadOnlyOrder order in active)
            {
                if (order == null || IsTerminal(order.Status)) { continue; }
                int priority = Priority(order.Status);
                if (priority < bestPriority) { bestPriority = priority; primary = order; }
            }
            var live = new List<IReadOnlyOrder>();
            foreach (IReadOnlyOrder order in active)
            {
                if (order != null && !IsTerminal(order.Status)) { live.Add(order); }
            }
            // Keep service order (oldest first); the primary order is always listed even when the list overflows.
            if (live.Count > MaxRows && primary != null && live.IndexOf(primary) >= MaxRows)
            {
                live.Remove(primary);
                live.Insert(MaxRows - 1, primary);
            }
            int hidden = Math.Max(0, live.Count - MaxRows);
            var rows = new List<OrderRow>();
            OrderRow primaryRow = null;
            for (int i = 0; i < live.Count - hidden; i++)
            {
                bool isPrimary = ReferenceEquals(live[i], primary);
                OrderRow row = Describe(live[i], isPrimary);
                if (isPrimary) { primaryRow = row; }
                rows.Add(row);
            }
            _rows = rows.AsReadOnly();
            HiddenCount = hidden;
            Primary = primaryRow;
        }

        internal static bool IsTerminal(OrderStatus status) => status == OrderStatus.Completed || status == OrderStatus.Failed;

        /// <summary>Lower is more urgent: finish what is in hand, then pick up, then cook, then greet.</summary>
        internal static int Priority(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.PickedUpByLobby: return 0;
                case OrderStatus.Delivered: return 1;
                case OrderStatus.TakingOrder:
                case OrderStatus.Entered: return 2;
                case OrderStatus.Ready: return 3;
                case OrderStatus.SentToStall:
                case OrderStatus.InPreparation: return 4;
                case OrderStatus.WaitingForLobby: return 5;
                default: return 9;
            }
        }

        public static OrderRow Describe(IReadOnlyOrder order, bool isPrimary = false)
        {
            if (order == null) { throw new ArgumentNullException(nameof(order)); }
            HudText origin = HudKeys.Origin(order.Origin);
            IReadOnlyList<IReadOnlyOrderItem> items = order.Items ?? Array.Empty<IReadOnlyOrderItem>();
            var lines = new List<HudText>();
            int ready = 0;
            if (items.Count == 0)
            {
                foreach (ItemRequest request in order.RequestedItems ?? Array.Empty<ItemRequest>())
                {
                    lines.Add(HudText.Of(HudKeys.OrderItemQuantity, HudText.Of(HudKeys.ItemName(request.ItemDefinitionId)), request.Quantity));
                }
            }
            else
            {
                foreach (IReadOnlyOrderItem item in items)
                {
                    if (item.Status == OrderItemStatus.Ready) { ready++; }
                    lines.Add(HudText.Of(HudKeys.OrderItemStatus, HudText.Of(HudKeys.ItemName(item.ItemDefinitionId)), HudText.Of(HudKeys.Status(item.Status))));
                }
            }
            HudText stage = HudText.Of(HudKeys.Status(order.Status));
            if ((order.Status == OrderStatus.SentToStall || order.Status == OrderStatus.InPreparation) && items.Count > 0)
            {
                stage = HudText.Of(HudKeys.OrderStageProgress, stage, ready, items.Count);
            }
            return new OrderRow(order.Id, order.Status, origin, stage, lines.AsReadOnly(), NextStep(order, origin), ready, items.Count, isPrimary);
        }

        public static HudText NextStep(IReadOnlyOrder order, HudText origin)
        {
            switch (order.Status)
            {
                case OrderStatus.WaitingForLobby: return HudText.Of(HudKeys.NextTakeOrder, origin);
                case OrderStatus.TakingOrder: return HudText.Of(HudKeys.NextEnterOrder);
                case OrderStatus.Entered: return HudText.Of(HudKeys.NextSendOrder);
                case OrderStatus.SentToStall:
                case OrderStatus.InPreparation: return StallStep(order);
                case OrderStatus.Ready: return HudText.Of(HudKeys.NextPickUp);
                case OrderStatus.PickedUpByLobby: return HudText.Of(HudKeys.NextDeliverTo, origin);
                case OrderStatus.Delivered: return HudText.Of(HudKeys.NextCompleteAt, origin);
                default: return HudText.Empty;
            }
        }

        private static HudText StallStep(IReadOnlyOrder order)
        {
            IReadOnlyList<IReadOnlyOrderItem> items = order.Items ?? Array.Empty<IReadOnlyOrderItem>();
            // An item already being made comes first; then the first item no station has claimed.
            foreach (IReadOnlyOrderItem item in items)
            {
                if (item.Status == OrderItemStatus.InPreparation)
                {
                    return HudText.Of(HudKeys.NextFinishItem, HudText.Of(HudKeys.ItemName(item.ItemDefinitionId)));
                }
            }
            foreach (IReadOnlyOrderItem item in items)
            {
                if (item.Status == OrderItemStatus.Pending)
                {
                    return HudText.Of(item.Kind == ItemKind.Cake ? HudKeys.NextMakeCake : HudKeys.NextMakeDrink);
                }
            }
            return HudText.Of(HudKeys.NextMakeItems);
        }
    }
}
