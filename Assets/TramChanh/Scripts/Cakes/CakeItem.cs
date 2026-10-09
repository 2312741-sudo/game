using System;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Cakes
{
    public sealed class CakeItem : MonoBehaviour, IHoldable, IPreparedItem, IPreparationFeedback, IHeldItemAction
    {
        [SerializeField] private Transform _handGrip;
        [SerializeField] private GameObject _flatVisual;
        [SerializeField] private GameObject _rolledVisual;
        [SerializeField] private GameObject _paperVisual;
        private IDisposable _readySubscription, _deliverySubscription;
        private CakePreparation _preparation;
        public CakePreparation Preparation => _preparation;
        public Transform HandGrip => _handGrip;
        public ItemKind Kind => ItemKind.Cake;
        public OrderItemRef BoundItem => _preparation?.BoundItem ?? default;
        public bool IsFinished => _preparation != null && _preparation.IsFinished;
        public int Quality => _preparation?.Quality ?? 0;
        public string PreparationStateKey => _preparation?.PreparationStateKey ?? "cake.state.waiting";
        public string NextActionKey => _preparation?.NextActionKey ?? "cake.fill";
        public void Initialize(CakePreparation preparation, IEventBus events)
        {
            _preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
            _readySubscription?.Dispose(); _deliverySubscription?.Dispose();
            _readySubscription = events.Subscribe<OrderItemStatusChanged>(message =>
            {
                if (message.Item.PreparationId == _preparation.PreparationId) { Apply(); }
            });
            OrderId order = preparation.BoundItem.OrderId;
            _deliverySubscription = events.Subscribe<OrderStatusChanged>(message =>
            {
                if (message.OrderId == order && message.Status == OrderStatus.Delivered) { _preparation.Delivered(); Apply(); }
            });
            Apply();
        }
        public Result MarkReady() => _preparation.MarkReady();
        public void OnPickedUp(IHeldItemSlot hands)
        {
            if (!ReferenceEquals(hands.Current, this) || (_preparation.State != CakeState.Wrapped && _preparation.State != CakeState.Ruined))
            { throw new InvalidOperationException("Only wrapped or removed burnt cake enters hands."); }
        }
        public void OnReleased() { }
        public InteractionQuery QueryUse(InteractionContext context) => new InteractionQuery(
            ReferenceEquals(context.Hands.Current, this) && _preparation.State == CakeState.Ruined
                ? CakeStation.GuardActor(context) : Availability.Hidden, "cake.discard_burnt");
        public void ExecuteUse(InteractionContext context)
        {
            if (!QueryUse(context).Availability.IsAvailable) { return; }
            context.Hands.TryRelease(); Destroy(gameObject);
        }
        public void Apply()
        {
            bool rolled = _preparation.State >= CakeState.Rolled && _preparation.State != CakeState.Ruined;
            if (_flatVisual != null) { _flatVisual.SetActive(!rolled); }
            if (_rolledVisual != null) { _rolledVisual.SetActive(rolled); }
            if (_paperVisual != null) { _paperVisual.SetActive(_preparation.State >= CakeState.Wrapped && _preparation.State != CakeState.Ruined); }
            if (_flatVisual != null)
            {
                float cooked = Mathf.Clamp01((float)(_preparation.Doneness / _preparation.Recipe.BurnThreshold));
                var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", Color.Lerp(new Color(1f, .85f, .45f), new Color(.12f, .06f, .02f), cooked));
                foreach (var renderer in _flatVisual.GetComponentsInChildren<Renderer>()) { renderer.SetPropertyBlock(block); }
            }
        }
        private void OnDestroy() { _readySubscription?.Dispose(); _deliverySubscription?.Dispose(); }
    }
}
