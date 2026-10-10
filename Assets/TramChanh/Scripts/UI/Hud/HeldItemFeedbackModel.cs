using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;

namespace TramChanh.UI.Hud
{
    /// <summary>
    /// Engine-free state of the held-item panel: name, preparation state, next action and the order the
    /// item belongs to. It reads only generic contracts (<see cref="IHoldable"/>, <see cref="IPreparationFeedback"/>,
    /// <see cref="IPreparedItem"/>, read-only orders) and never references Drinks, Cakes or Lobby types.
    /// </summary>
    public sealed class HeldItemFeedbackModel : IDisposable
    {
        private readonly IHeldItemSlot _hands;
        private readonly IOrderService _orders;
        private readonly Predicate<string> _hasKey;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private InteractionQuery _heldQuery;
        private bool _disposed;

        /// <param name="hands">The player's hand slot.</param>
        /// <param name="events">Bus carrying HeldItemChanged, InteractionPromptChanged and Orders events.</param>
        /// <param name="orders">Optional read-only order lookup used for item names and destinations.</param>
        /// <param name="hasKey">Optional localization probe; per-type names (held. plus the type name) are used only when it accepts them.</param>
        public HeldItemFeedbackModel(IHeldItemSlot hands, IEventBus events, IOrderService orders = null, Predicate<string> hasKey = null)
        {
            _hands = hands ?? throw new ArgumentNullException(nameof(hands));
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            _orders = orders;
            _hasKey = hasKey;
            _subscriptions.Add(events.Subscribe<HeldItemChanged>(_ => { _heldQuery = default; Refresh(); }));
            _subscriptions.Add(events.Subscribe<InteractionPromptChanged>(OnPrompt));
            _subscriptions.Add(events.Subscribe<OrderStatusChanged>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<OrderItemStatusChanged>(_ => Refresh()));
            Refresh();
        }

        public bool HasItem { get; private set; }
        public IHoldable Current { get; private set; }
        public HudText Name { get; private set; }
        public HudText State { get; private set; }
        public HudText NextAction { get; private set; }
        /// <summary>"For order Vehicle 1" or "Order cancelled"; empty when the item is not order-bound.</summary>
        public HudText Order { get; private set; }
        public Exception LastFault { get; private set; }
        public event Action Changed;

        /// <summary>
        /// Recomputes from the live hand slot; raises <see cref="Changed"/> only when the visible text changes.
        /// Safe to call every frame (a cooking cake changes state without an event). Never throws.
        /// </summary>
        public void Refresh()
        {
            if (_disposed) { return; }
            IHoldable current;
            HudText name, state, next, order;
            try
            {
                current = _hands.Current;
                if (current is UnityEngine.Object unityObject && unityObject == null) { current = null; }
                Describe(current, out name, out state, out next, out order);
            }
            catch (Exception fault)
            {
                LastFault = fault;
                return;
            }
            bool changed = !ReferenceEquals(current, Current) || !name.Equals(Name) || !state.Equals(State) || !next.Equals(NextAction) || !order.Equals(Order);
            Current = current;
            HasItem = current != null;
            Name = name;
            State = state;
            NextAction = next;
            Order = order;
            if (changed) { Changed?.Invoke(); }
        }

        public void Dispose()
        {
            if (_disposed) { return; }
            _disposed = true;
            foreach (IDisposable subscription in _subscriptions) { subscription.Dispose(); }
            _subscriptions.Clear();
        }

        private void OnPrompt(InteractionPromptChanged prompt)
        {
            // The held-use query is the HUD's only view of an item's own action when it has no feedback contract.
            _heldQuery = prompt.IsHeldUse ? prompt.Query : prompt.HasHeldPrompt ? prompt.HeldQuery : default;
            Refresh();
        }

        private void Describe(IHoldable item, out HudText name, out HudText state, out HudText next, out HudText order)
        {
            name = state = next = order = HudText.Empty;
            if (item == null)
            {
                name = HudText.Of(HudKeys.HeldEmpty);
                next = HudText.Of(HudKeys.HeldEmptyHint);
                return;
            }
            var feedback = item as IPreparationFeedback;
            var prepared = item as IPreparedItem;
            IReadOnlyOrder boundOrder = null;
            IReadOnlyOrderItem boundItem = null;
            if (prepared != null && prepared.BoundItem.IsValid && _orders != null)
            {
                boundOrder = _orders.Get(prepared.BoundItem.OrderId);
                boundItem = FindItem(boundOrder, prepared.BoundItem.OrderItemId);
            }
            IReadOnlyOrder carried = prepared == null ? CarriedOrder(feedback) : null;

            // Name: the ordered dish when known, else a per-type name, else the generic kind.
            string typeKey = HudKeys.HeldType(item.GetType());
            if (boundItem != null && !string.IsNullOrEmpty(boundItem.ItemDefinitionId)) { name = HudText.Of(HudKeys.ItemName(boundItem.ItemDefinitionId)); }
            else if (_hasKey == null || _hasKey(typeKey)) { name = HudText.Of(typeKey); }
            else if (prepared != null) { name = HudText.Of(HudKeys.Kind(prepared.Kind)); }
            else { name = HudText.Of(HudKeys.HeldUnknown); }

            // State: the item's own feedback, else its order item status.
            if (feedback != null && !string.IsNullOrEmpty(feedback.PreparationStateKey)) { state = HudText.Of(feedback.PreparationStateKey); }
            else if (prepared != null && prepared.IsFinished) { state = HudText.Of(HudKeys.HeldStateFinished); }
            else if (boundItem != null) { state = HudText.Of(HudKeys.Status(boundItem.Status)); }

            // Order binding / destination.
            if (boundOrder != null)
            {
                order = boundOrder.Status == OrderStatus.Failed ? HudText.Of(HudKeys.HeldOrderCancelled) : HudText.Of(HudKeys.HeldForOrder, HudKeys.Origin(boundOrder.Origin));
            }

            // Next action: carried order goes to its origin; then feedback; then the item's own available action.
            if (carried != null) { next = HudText.Of(HudKeys.NextDeliverTo, HudKeys.Origin(carried.Origin)); }
            else if (feedback != null && !string.IsNullOrEmpty(feedback.NextActionKey)) { next = HudText.Of(feedback.NextActionKey); }
            else if (_heldQuery.Availability.IsAvailable && !string.IsNullOrEmpty(_heldQuery.PromptKey)) { next = HudText.Of(_heldQuery.PromptKey); }
            else if (prepared != null && prepared.IsFinished) { next = HudText.Of("ready.place"); }
            else if (prepared != null) { next = HudText.Of(prepared.Kind == ItemKind.Cake ? HudKeys.NextCakeSteps : HudKeys.NextDrinkSteps); }
        }

        /// <summary>
        /// A non-prepared held item whose feedback state is an order status (the Lobby carrying bundle) carries
        /// the single order that is PickedUpByLobby. Lead request: an Orders-level carrier contract would make this exact.
        /// </summary>
        private IReadOnlyOrder CarriedOrder(IPreparationFeedback feedback)
        {
            if (_orders == null || feedback == null || feedback.PreparationStateKey == null
                || !feedback.PreparationStateKey.StartsWith(HudKeys.OrderStatusPrefix, StringComparison.Ordinal)) { return null; }
            IReadOnlyOrder found = null;
            foreach (IReadOnlyOrder order in _orders.Active ?? Array.Empty<IReadOnlyOrder>())
            {
                if (order == null || order.Status != OrderStatus.PickedUpByLobby) { continue; }
                if (found != null) { return null; }
                found = order;
            }
            return found;
        }

        private static IReadOnlyOrderItem FindItem(IReadOnlyOrder order, OrderItemId id)
        {
            if (order?.Items == null) { return null; }
            foreach (IReadOnlyOrderItem item in order.Items) { if (item != null && item.Id == id) { return item; } }
            return null;
        }
    }
}
