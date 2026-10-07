using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Lobby
{
    /// <summary>
    /// Shared Lobby adapter for a place where one customer is served (table or vehicle point).
    /// Thin: it forwards to <see cref="IOrderService"/> and opens the entry flow through the controller.
    /// Delivery (T8) and completion are not implemented here yet.
    /// </summary>
    public abstract class OrderPoint : MonoBehaviour, IInteractable
    {
        public const string TakeOrderPromptKey = "order.point.take";
        public const string ContinueOrderPromptKey = "order.point.continue";

        [SerializeField] private int _interactableId;
        [SerializeField] private Transform _interactionPoint;
        private IOrderService _orders;
        private LobbyOrderController _controller;
        private OrderId _active;

        public abstract OrderOrigin Origin { get; }
        public InteractableId Id => _interactableId > 0 ? new InteractableId(_interactableId) : default;
        public Transform InteractionPoint => _interactionPoint;
        /// <summary>The live order served here, or default when the point is free (terminal orders free the point).</summary>
        public OrderId ActiveOrder => LiveOrder() != null ? _active : default;
        protected bool IsConfigured => _orders != null && _controller != null && _interactableId > 0 && _interactionPoint != null;

        protected void Configure(int interactableId, Transform interactionPoint, IOrderService orders, LobbyOrderController controller)
        {
            if (interactableId <= 0) { throw new ArgumentOutOfRangeException(nameof(interactableId)); }
            _interactableId = interactableId;
            _interactionPoint = interactionPoint != null ? interactionPoint : throw new ArgumentNullException(nameof(interactionPoint));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _controller = controller != null ? controller : throw new ArgumentNullException(nameof(controller));
            _active = default;
        }

        /// <summary>T1 entry point for the customer side: one live order per point.</summary>
        public Result<OrderId> RequestCustomerService(CustomerId customer, IReadOnlyList<ItemRequest> requested)
        {
            if (!IsConfigured) { return Result<OrderId>.Fail("order.point.not_configured"); }
            if (!customer.IsValid) { return Result<OrderId>.Fail("order.point.invalid_customer"); }
            if (requested == null || requested.Count == 0) { return Result<OrderId>.Fail("order.point.empty_request"); }
            if (LiveOrder() != null) { return Result<OrderId>.Fail("order.point.occupied"); }
            Result<OrderId> result = _orders.RequestService(Origin, customer, requested);
            if (!result.IsSuccess) { return result; }
            if (!result.Value.IsValid) { return Result<OrderId>.Fail("order.point.invalid_service_result"); }
            _active = result.Value;
            return result;
        }

        public InteractionQuery Query(InteractionContext context)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }
            if (!IsConfigured) { return new InteractionQuery(Availability.Blocked("order.point.not_configured"), TakeOrderPromptKey); }
            IReadOnlyOrder order = LiveOrder();
            string prompt = PromptFor(order);
            if (prompt == null) { return new InteractionQuery(Availability.Hidden, null); }
            Availability availability;
            if ((context.Role & ActorRole.Lobby) == 0) { availability = Availability.Blocked("interaction.lobby_role_required"); }
            else if (context.Clock.IsPaused) { availability = Availability.Blocked("interaction.paused"); }
            else if (context.Hands.Current != null) { availability = Availability.Blocked("hands.full"); }
            else { availability = Availability.Available; }
            return new InteractionQuery(availability, prompt);
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (query.Availability.Status == AvailabilityStatus.Hidden) { return; }
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey));
                return;
            }
            Result opened = _controller.OpenEntry(Id, _active, context.Actor, Origin);
            if (!opened.IsSuccess)
            {
                context.Events.Publish(new ActionBlocked(Id, opened.ReasonKey));
            }
        }

        private IReadOnlyOrder LiveOrder()
        {
            if (_orders == null || !_active.IsValid) { return null; }
            IReadOnlyOrder order = _orders.Get(_active);
            if (order == null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Failed) { return null; }
            return order;
        }

        private static string PromptFor(IReadOnlyOrder order)
        {
            if (order == null) { return null; }
            switch (order.Status)
            {
                case OrderStatus.WaitingForLobby: return TakeOrderPromptKey;
                case OrderStatus.TakingOrder:
                case OrderStatus.Entered: return ContinueOrderPromptKey;
                default: return null;
            }
        }
    }
}
