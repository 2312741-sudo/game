using System;
using TramChanh.Core;

namespace TramChanh.Interaction
{
    /// <summary>One clock-driven action; no timers read Unity time or modify products.</summary>
    public sealed class InteractionActionDriver
    {
        private readonly InteractionContext _context;
        private readonly HeldActionTarget _heldTarget;
        private IHoldable _heldAtStart;
        private IInteractable _target;
        private InteractionQuery _query;
        private double _startedAt;
        public bool IsRunning => _target != null;
        public bool IsHeldUse => IsRunning && ReferenceEquals(_target, _heldTarget);
        public float Progress { get; private set; }

        public InteractionActionDriver(InteractionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _heldTarget = new HeldActionTarget();
        }

        public void Begin(IInteractable target)
        {
            if (target == null || IsRunning || _context.Clock.IsPaused)
            {
                return;
            }
            InteractionQuery query = target.Query(_context);
            if (!query.Availability.IsAvailable)
            {
                if (query.Availability.Status == AvailabilityStatus.Blocked)
                {
                    _context.Events.Publish(new ActionBlocked(target.Id, query.BlockedReasonKey));
                }
                return;
            }
            if (query.Kind == InteractionKind.Press)
            {
                target.Execute(_context);
                return;
            }
            _target = target;
            _query = query;
            _heldAtStart = _context.Hands.Current;
            _startedAt = _context.Clock.Now;
            Progress = 0f;
            target.OnHoldStarted(_context);
        }

        public void BeginHeld()
        {
            if (IsRunning || _context.Hands.Current is not IHeldItemAction action)
            {
                return;
            }
            if (_context.Clock.IsPaused) { return; }
            InteractionQuery query = action.QueryUse(_context);
            if (query.Kind == InteractionKind.Continuous)
            {
                _context.Events.Publish(new ActionBlocked((action as IInteractable)?.Id ?? default, "interaction.held_continuous_not_supported"));
                return;
            }
            _heldTarget.Bind(action);
            Begin(_heldTarget);
        }

        public void Tick(IInteractable focused)
        {
            if (!IsRunning)
            {
                return;
            }
            if (!ReferenceEquals(_heldAtStart, _context.Hands.Current)
                || (_heldAtStart is UnityEngine.Object itemObject && itemObject == null))
            {
                Release();
                return;
            }
            // Paused queries may be blocked: preserve the action until resume or physical release.
            if (_context.Clock.IsPaused)
            {
                return;
            }
            if (!IsHeldUse && !ReferenceEquals(focused, _target))
            {
                Release();
                return;
            }
            InteractionQuery current = _target.Query(_context);
            if (!current.Equals(_query))
            {
                Release();
                return;
            }
            if (_query.Kind == InteractionKind.Hold)
            {
                Progress = (float)Math.Min(1d, (_context.Clock.Now - _startedAt) / _query.HoldDuration);
                if (Progress >= 1f)
                {
                    IInteractable completed = _target;
                    _target = null;
                    _heldAtStart = null;
                    Progress = 0f;
                    completed.Execute(_context);
                }
            }
        }

        public void Release()
        {
            if (!IsRunning)
            {
                return;
            }
            IInteractable previous = _target;
            InteractionKind kind = _query.Kind;
            float seconds = (float)(_context.Clock.Now - _startedAt);
            _target = null;
            _heldAtStart = null;
            Progress = 0f;
            if (kind == InteractionKind.Continuous)
            {
                previous.OnContinuousReleased(_context, seconds);
            }
            else
            {
                previous.OnHoldCancelled(_context);
            }
        }
        /// <summary>Cached adapter reuses the approved held and target contracts.</summary>
        private sealed class HeldActionTarget : IInteractable
        {
            private IHeldItemAction _action;
            public InteractableId Id => (_action as IInteractable)?.Id ?? default;
            public UnityEngine.Transform InteractionPoint => null;
            public void Bind(IHeldItemAction action) => _action = action;
            public InteractionQuery Query(InteractionContext context) => _action.QueryUse(context);
            public void Execute(InteractionContext context) => _action.ExecuteUse(context);
            public void OnHoldStarted(InteractionContext context) => (_action as IInteractable)?.OnHoldStarted(context);
            public void OnHoldCancelled(InteractionContext context) => (_action as IInteractable)?.OnHoldCancelled(context);
            public void OnContinuousReleased(InteractionContext context, float seconds) => (_action as IInteractable)?.OnContinuousReleased(context, seconds);
        }
    }
}
