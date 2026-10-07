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
    public sealed class ServedOrder : MonoBehaviour, IHoldable
    {
        [SerializeField] private Transform _handGrip;
        private IReadOnlyList<IPreparedItem> _items = Array.Empty<IPreparedItem>();
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

        public void Populate(IReadOnlyList<IPreparedItem> items)
        {
            if (items == null || items.Count == 0 || _items.Count != 0)
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
            if (!ReferenceEquals(hands.Current, this) || !OrderId.IsValid)
            {
                throw new InvalidOperationException("Only a bound bundle can reserve its receiving held slot.");
            }
        }
        public void OnReleased() { }
    }
}
