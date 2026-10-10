using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Lobby
{
    /// <summary>Generic Lobby carrying bundle; preparation and delivery remain in their owning modules.</summary>
    public sealed class ServedOrder : MonoBehaviour, IHoldable, IPreparationFeedback
    {
        [SerializeField] private Transform _handGrip;
        private IReadOnlyList<IPreparedItem> _items = Array.Empty<IPreparedItem>();
        private IHeldItemSlot _hands;
        private IReadOnlyOrder _order;
        private IDisposable _status;
        private bool _retired;
        public bool IsRetired => _retired;
        public string PreparationStateKey => "order.status.PickedUpByLobby";
        public string NextActionKey => "hud.next.delivery";
        public OrderId OrderId { get; private set; }
        public IReadOnlyList<IPreparedItem> Items => _items;
        public Transform HandGrip => _handGrip;

        public void Initialize(OrderId orderId)
        {
            if (!orderId.IsValid)
            {
                throw new ArgumentException("A served bundle requires a bound order.", nameof(orderId));
            }
            OrderId = orderId;
            if (_handGrip == null)
            {
                var anchors = new GameObject("Anchors");
                anchors.transform.SetParent(transform, false);
                _handGrip = new GameObject("HandGrip").transform;
                _handGrip.SetParent(anchors.transform, false);
            }
        }

        public void BindDelivery(IOrderService orders, IEventBus events)
        {
            if (orders == null) { throw new ArgumentNullException(nameof(orders)); }
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            if (!OrderId.IsValid || _retired) { throw new InvalidOperationException("Bind an initialized live bundle."); }
            _order = orders.Get(OrderId) ?? throw new InvalidOperationException("The carried order must exist.");
            _status?.Dispose();
            _status = events.Subscribe<OrderStatusChanged>(OnOrderStatusChanged);
            RetireIfTerminal();
        }

        private void OnOrderStatusChanged(OrderStatusChanged message)
        {
            if (message.OrderId == OrderId) { RetireIfTerminal(); }
        }
        private void LateUpdate() => RetireIfTerminal();
        private void RetireIfTerminal()
        {
            // A preceding faulty observer can stop EventBus delivery; the live order is authoritative.
            if (_order != null && (_order.Status == OrderStatus.Failed || _order.Status == OrderStatus.Delivered || _order.Status == OrderStatus.Completed))
            { ReleaseAndRetire(); }
        }

        public void ReleaseAndRetire()
        {
            if (_retired) { return; }
            _retired = true;
            _status?.Dispose();
            _status = null;
            try
            {
                if (_hands != null && ReferenceEquals(_hands.Current, this)) { _hands.TryRelease(); }
            }
            catch (Exception fault) { Debug.LogException(fault); }
            finally
            {
                if (this != null)
                {
                    // A release observer may adopt a prepared child as its new held item.
                    if (_hands?.Current is Component replacement && replacement != null && replacement.transform.IsChildOf(transform))
                    { replacement.transform.SetParent(null, true); }
                    transform.SetParent(null, true);
                    gameObject.SetActive(false);
                    if (Application.isPlaying) { Destroy(gameObject); }
                    else { DestroyImmediate(gameObject); }
                }
            }
        }
        private void OnDestroy() => _status?.Dispose();

        public void Populate(IReadOnlyList<IPreparedItem> items)
        {
            if (_retired || items == null || items.Count == 0 || _items.Count != 0)
            {
                throw new InvalidOperationException("A reserved bundle accepts one nonempty prepared-item snapshot.");
            }
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || items[i].BoundItem.OrderId != OrderId)
                {
                    throw new InvalidOperationException("All prepared items must belong to the carried order.");
                }
            }
            _items = items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not Component visual)
                {
                    continue;
                }
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = TramChanhLayers.HeldItemIndex;
                }
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }
            }
        }

        public void OnPickedUp(IHeldItemSlot hands)
        {
            if (_retired || !ReferenceEquals(hands.Current, this) || !OrderId.IsValid)
            {
                throw new InvalidOperationException("Only a bound bundle can reserve its receiving held slot.");
            }
            _hands = hands;
        }
        public void OnReleased() { }
    }
}
