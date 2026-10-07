using System;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Stall.Runtime
{
    public sealed class ReadyCounterPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private Transform _drinkPlacementPoint;
        [SerializeField] private Transform _cakePlacementPoint;
        private IReadyShelfPlacement _shelf;
        private IEventBus _events;
        private IDisposable _subscription;
        private Component _drinkVisual;
        private Component _cakeVisual;
        private OrderId _drinkOrder;
        private OrderId _cakeOrder;
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public Transform DrinkPlacementPoint => _drinkPlacementPoint;
        public Transform CakePlacementPoint => _cakePlacementPoint;

        public void Initialize(IReadyShelfPlacement shelf, IEventBus events)
        {
            Disconnect();
            _shelf = shelf ?? throw new ArgumentNullException(nameof(shelf));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Subscribe();
        }

        public InteractionQuery Query(InteractionContext context)
        {
            Availability available;
            if (_shelf == null || _drinkPlacementPoint == null || _cakePlacementPoint == null)
            {
                available = Availability.Blocked("ready.counter.not_configured");
            }
            else if ((context.Role & ActorRole.Stall) == 0)
            {
                available = Availability.Blocked("interaction.stall_role_required");
            }
            else if (context.Clock.IsPaused)
            {
                available = Availability.Blocked("interaction.paused");
            }
            else if (context.Hands.Current is not IPreparedItem prepared || context.Hands.Current is not Component)
            {
                available = Availability.Blocked("ready.need_prepared_item");
            }
            else
            {
                available = _shelf.CanPlace(prepared);
            }
            return new InteractionQuery(available, "ready.place_item");
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey));
                return;
            }
            IHoldable held = context.Hands.Current;
            var prepared = (IPreparedItem)held;
            var visual = (Component)held;
            Transform placement = visual.transform.Find("Anchors/PlacementPoint");
            if (placement == null)
            {
                context.Events.Publish(new ActionBlocked(Id, "ready.item_missing_placement_point"));
                return;
            }
            Result result = _shelf.PlaceReady(prepared);
            if (!result.IsSuccess)
            {
                context.Events.Publish(new ActionBlocked(Id, result.ReasonKey));
                return;
            }
            if (ReferenceEquals(context.Hands.Current, held))
            {
                context.Hands.TryRelease();
            }
            Transform destination = prepared.Kind == ItemKind.Drink ? _drinkPlacementPoint : _cakePlacementPoint;
            Transform root = visual.transform;
            Quaternion placementRotation = Quaternion.Inverse(root.rotation) * placement.rotation;
            Vector3 placementPosition = root.InverseTransformPoint(placement.position);
            root.SetParent(destination, false);
            root.localRotation = Quaternion.Inverse(placementRotation);
            root.localPosition = -(root.localRotation * Vector3.Scale(placementPosition, root.localScale));
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = true;
            }
            if (prepared.Kind == ItemKind.Drink)
            {
                _drinkVisual = visual;
                _drinkOrder = prepared.BoundItem.OrderId;
            }
            else
            {
                _cakeVisual = visual;
                _cakeOrder = prepared.BoundItem.OrderId;
            }
        }

        private void Subscribe()
        {
            if (isActiveAndEnabled && _events != null && _subscription == null)
            {
                _subscription = _events.Subscribe<OrderStatusChanged>(OnOrderChanged);
            }
        }
        private void OnOrderChanged(OrderStatusChanged change)
        {
            if (change.Status != OrderStatus.PickedUpByLobby && change.Status != OrderStatus.Failed)
            {
                return;
            }
            ClearVisual(ref _drinkVisual, ref _drinkOrder, change);
            ClearVisual(ref _cakeVisual, ref _cakeOrder, change);
        }
        private static void ClearVisual(ref Component visual, ref OrderId order, OrderStatusChanged change)
        {
            if (visual == null || order != change.OrderId)
            {
                return;
            }
            GameObject previous = visual.gameObject;
            visual = null;
            order = default;
            previous.transform.SetParent(null, true);
            if (change.Status == OrderStatus.Failed)
            {
                previous.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(previous);
                }
                else
                {
                    DestroyImmediate(previous);
                }
            }
        }
        private void Disconnect()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() => Disconnect();
        private void OnDestroy() => Disconnect();
    }
}
