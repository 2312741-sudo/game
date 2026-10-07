using System;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using UnityEngine;

namespace TramChanh.Interaction
{
    /// <summary>Aligns a component-backed held item by its grip to the camera hold anchor.</summary>
    public sealed class HeldItemView : MonoBehaviour
    {
        [SerializeField] private Transform _holdAnchor;
        private IHeldItemSlot _hands;
        private IEventBus _events;
        private IDisposable _subscription;
        public Transform HoldAnchor => _holdAnchor;

        public void Initialize(IHeldItemSlot hands, IEventBus events)
        {
            Disconnect();
            _hands = hands;
            _events = events;
            if (_holdAnchor == null)
            {
                throw new InvalidOperationException("HeldItemView requires an explicit hold anchor.");
            }
            Connect();
        }

        private void OnEnable() => Connect();
        private void OnDisable() => Disconnect();

        private void Connect()
        {
            if (!isActiveAndEnabled || _events == null || _subscription != null)
            {
                return;
            }
            _subscription = _events.Subscribe<HeldItemChanged>(OnChanged);
            Attach(_hands.Current);
        }

        public void Disconnect()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void OnChanged(HeldItemChanged change)
        {
            // Release is invoked only by a valid placement adapter, which owns the
            // next parent/pose/collision state. No floor-drop input is implemented.
            Attach(change.Current);
        }

        private void Attach(IHoldable item)
        {
            if (!(item is Component component) || item.HandGrip == null)
            {
                return;
            }
            Transform root = component.transform;
            Quaternion gripRotation = Quaternion.Inverse(root.rotation) * item.HandGrip.rotation;
            Vector3 gripPosition = root.InverseTransformPoint(item.HandGrip.position);
            root.SetParent(_holdAnchor, false);
            root.localRotation = Quaternion.Inverse(gripRotation);
            root.localPosition = -(root.localRotation * Vector3.Scale(gripPosition, root.localScale));
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = TramChanhLayers.HeldItemIndex;
            }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }
    }
}
