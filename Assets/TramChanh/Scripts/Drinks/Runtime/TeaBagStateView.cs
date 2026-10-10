using System;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Drinks.Runtime
{
    public sealed class TeaBagStateView : MonoBehaviour
    {
        [SerializeField] private GameObject _bagClosed;
        [SerializeField] private GameObject _bagOpen;
        [SerializeField] private GameObject _coconutJelly;
        [SerializeField] private GameObject _lemonJelly;
        [SerializeField] private GameObject _ice;
        [SerializeField] private GameObject _condensation;
        [SerializeField] private Animator _animator;
        private DrinkPreparation _preparation;
        private IEventBus _events;
        private IDisposable _subscription;
        private IDisposable _readySubscription;
        private static readonly int ShakingParameter = Animator.StringToHash("Shaking");
        public bool IsShaking { get; private set; }

        public void Initialize(DrinkPreparation preparation, IEventBus events)
        {
            Disconnect();
            _preparation = preparation;
            _events = events;
            if (isActiveAndEnabled)
            {
                Connect();
            }
            Apply(preparation.State);
        }

        private void OnEnable()
        {
            Connect();
            if (_preparation != null)
            {
                Apply(_preparation.State);
            }
        }
        private void OnDisable()
        {
            Disconnect();
            SetShaking(false);
        }
        private void Connect()
        {
            if (_subscription == null && _events != null)
            {
                _subscription = _events.Subscribe<DrinkStepCompleted>(OnStepCompleted);
                _readySubscription = _events.Subscribe<OrderItemStatusChanged>(OnOrderItemChanged);
            }
        }
        private void Disconnect()
        {
            _subscription?.Dispose();
            _subscription = null;
            _readySubscription?.Dispose();
            _readySubscription = null;
        }
        private void OnStepCompleted(DrinkStepCompleted step)
        {
            if (_preparation != null && step.PreparationId == _preparation.PreparationId)
            {
                Apply(_preparation.State);
            }
        }

        private void OnOrderItemChanged(OrderItemStatusChanged change)
        {
            if (_preparation != null && change.Status == OrderItemStatus.Ready
                && change.Item.PreparationId == _preparation.PreparationId
                && change.Item == _preparation.BoundItem)
            {
                Apply(_preparation.State);
            }
        }

        public void Apply(TeaBagState state)
        {
            bool open = (int)state >= (int)TeaBagState.Opened;
            SetVisible(_bagClosed, !open);
            SetVisible(_bagOpen, open);
            SetVisible(_coconutJelly, (int)state >= (int)TeaBagState.CoconutJellyAdded);
            SetVisible(_lemonJelly, (int)state >= (int)TeaBagState.LemonJellyAdded);
            SetVisible(_ice, (int)state >= (int)TeaBagState.IceAdded);
            SetVisible(_condensation, state == TeaBagState.Shaken);
        }

        // Unity's == null, not ?.: an unassigned serialized field is a fake-null object that ?. would call into.
        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }

        public void SetShaking(bool shaking)
        {
            IsShaking = shaking;
            if (_animator != null && _animator.isActiveAndEnabled && _animator.isInitialized && _animator.runtimeAnimatorController != null)
            {
                _animator.SetBool(ShakingParameter, shaking);
            }
        }
    }
}
