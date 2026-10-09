using System;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.Lobby
{
    public sealed class ReadyOrderPickupPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        private IReadyShelfPickup _shelf;
        private IOrderService _orders;
        private IEventBus _events;
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;

        public void Initialize(IReadyShelfPickup shelf, IOrderService orders, IEventBus events)
        {
            if (shelf == null) { throw new ArgumentNullException(nameof(shelf)); }
            if (orders == null) { throw new ArgumentNullException(nameof(orders)); }
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            _shelf = shelf;
            _orders = orders;
            _events = events;
        }
        public InteractionQuery Query(InteractionContext context)
        {
            Availability available;
            if (_shelf == null)
            {
                available = Availability.Blocked("ready.pickup.not_configured");
            }
            else if ((context.Role & ActorRole.Lobby) == 0)
            {
                available = Availability.Blocked("interaction.lobby_role_required");
            }
            else if (context.Clock.IsPaused)
            {
                available = Availability.Blocked("interaction.paused");
            }
            else if (context.Hands.Current != null)
            {
                available = Availability.Blocked("hands.full");
            }
            else
            {
                available = _shelf.NextReadyOrder.IsValid ? Availability.Available : Availability.Blocked("ready.no_ready_order");
            }
            return new InteractionQuery(available, "ready.pick_up_order");
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey));
                return;
            }
            OrderId orderId = _shelf.NextReadyOrder;
            var reservation = new GameObject("ServedOrder");
            SceneManager.MoveGameObjectToScene(reservation, gameObject.scene);
            var bundle = reservation.AddComponent<ServedOrder>();
            bool committed = false;
            try
            {
                bundle.Initialize(orderId);
                bundle.BindDelivery(_orders, _events);
                if (bundle == null || bundle.IsRetired)
                {
                    context.Events.Publish(new ActionBlocked(Id, "ready.no_complete_order"));
                    return;
                }
                // Reserve only empty carrying space. Order/item events remain owned by shelf commit.
                if (!context.Hands.TryPickUp(bundle) || !ReferenceEquals(context.Hands.Current, bundle))
                {
                    context.Events.Publish(new ActionBlocked(Id, "hands.full"));
                    return;
                }
                if (context.Clock.IsPaused || _shelf.NextReadyOrder != orderId)
                {
                    context.Events.Publish(new ActionBlocked(Id, context.Clock.IsPaused ? "interaction.paused" : "ready.no_ready_order"));
                    return;
                }
                var result = _shelf.PickUp(orderId, context.Actor);
                if (!result.IsSuccess)
                {
                    context.Events.Publish(new ActionBlocked(Id, result.ReasonKey));
                    return;
                }
                // Shelf observers may fail or deliver the order before the reservation can adopt visuals.
                if (bundle == null || bundle.IsRetired || !ReferenceEquals(context.Hands.Current, bundle) ||
                    _orders.Get(orderId)?.Status != OrderStatus.PickedUpByLobby)
                {
                    context.Events.Publish(new ActionBlocked(Id, "ready.no_complete_order"));
                    return;
                }
                bundle.Populate(result.Value);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    if (bundle != null) { bundle.ReleaseAndRetire(); }
                }
            }
        }
    }
}
