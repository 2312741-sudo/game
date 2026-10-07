using System;
using TramChanh.Core;

namespace TramChanh.Interaction
{
    /// <summary>One clock-driven action; no timers read Unity time or modify products.</summary>
    public sealed class InteractionActionDriver
    {
        private readonly InteractionContext _context;
        private IInteractable _target;
        private InteractionQuery _query;
        private double _startedAt;
        public bool IsRunning => _target != null;
        public float Progress { get; private set; }

        public InteractionActionDriver(InteractionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
            _startedAt = _context.Clock.Now;
            Progress = 0f;
            target.OnHoldStarted(_context);
        }

        public void Tick(IInteractable focused)
        {
            if (!IsRunning)
            {
                return;
            }
            if (!ReferenceEquals(focused, _target))
            {
                Release();
                return;
            }
            InteractionQuery current = _target.Query(_context);
            if (!current.Availability.IsAvailable || current.Kind != _query.Kind || current.HoldDuration != _query.HoldDuration)
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
    }
}
