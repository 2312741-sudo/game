using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Makes a card draggable with PointerDown/Move/Up events and pointer capture. It owns no rules: it forwards the
    /// pointer to a <see cref="DragSession"/>, moves a ghost label under the pointer and raises events for the view.
    /// A refused drop sends the ghost back to the card and hides it.
    /// </summary>
    public sealed class DragCardManipulator : PointerManipulator
    {
        private const float ReturnSeconds = 0.15f;
        private readonly DragSession _session;
        private readonly DragPayload _payload;
        private readonly VisualElement _ghostLayer;
        private readonly Label _ghost;
        private readonly Func<Vector2, DropZone> _zoneAt;

        /// <summary>Raised once when the pointer has moved far enough to count as a drag.</summary>
        public event Action DragStarted;
        /// <summary>Raised on every move while dragging: the zone under the pointer and whether a drop there would buy.</summary>
        public event Action<DropZone, DropVerdict> Hovering;
        /// <summary>Raised when a real drag ends, bought or not.</summary>
        public event Action<DropOutcome> Dropped;
        /// <summary>Raised whenever a gesture is over, including a cancel, so the view can reset its highlights.</summary>
        public event Action Ended;

        public DragCardManipulator(DragSession session, DragPayload payload, VisualElement ghostLayer, Label ghost, Func<Vector2, DropZone> zoneAt)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _payload = payload;
            _ghostLayer = ghostLayer ?? throw new ArgumentNullException(nameof(ghostLayer));
            _ghost = ghost ?? throw new ArgumentNullException(nameof(ghost));
            _zoneAt = zoneAt ?? throw new ArgumentNullException(nameof(zoneAt));
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnDown);
            target.RegisterCallback<PointerMoveEvent>(OnMove);
            target.RegisterCallback<PointerUpEvent>(OnUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnDown);
            target.UnregisterCallback<PointerMoveEvent>(OnMove);
            target.UnregisterCallback<PointerUpEvent>(OnUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        private void OnDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !target.enabledInHierarchy) { return; }
            Vector2 p = evt.position;
            _session.Press(_payload, p.x, p.y);
            target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnMove(PointerMoveEvent evt)
        {
            if (!target.HasPointerCapture(evt.pointerId)) { return; }
            Vector2 p = evt.position;
            bool wasDragging = _session.State == DragState.Dragging;
            if (!_session.Move(p.x, p.y)) { return; }
            if (!wasDragging) { BeginGhost(); DragStarted?.Invoke(); }
            PlaceGhost(p);
            DropZone zone = _zoneAt(p);
            Hovering?.Invoke(zone, _session.Hover(zone));
            evt.StopPropagation();
        }

        private void OnUp(PointerUpEvent evt)
        {
            if (!target.HasPointerCapture(evt.pointerId)) { return; }
            DragRelease release = _session.Release(_zoneAt(evt.position));
            target.ReleasePointer(evt.pointerId);
            if (release.WasDrag)
            {
                if (release.Outcome.Bought) { _ghost.style.display = DisplayStyle.None; }
                else { SendGhostBack(); }
                Dropped?.Invoke(release.Outcome);
            }
            Ended?.Invoke();
            evt.StopPropagation();
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_session.State == DragState.Idle) { return; }
            _session.Cancel();
            _ghost.style.display = DisplayStyle.None;
            Ended?.Invoke();
        }

        private void BeginGhost()
        {
            _ghost.text = DragRules.GhostText(_payload);
            _ghost.style.transitionProperty = new List<StylePropertyName>();
            _ghost.style.transitionDuration = new List<TimeValue>();
            _ghost.style.display = DisplayStyle.Flex;
        }

        private void PlaceGhost(Vector2 panelPosition)
        {
            Vector2 local = _ghostLayer.WorldToLocal(panelPosition);
            _ghost.style.left = local.x - 24f;
            _ghost.style.top = local.y - 16f;
        }

        private void SendGhostBack()
        {
            Vector2 home = _ghostLayer.WorldToLocal(target.worldBound.position);
            _ghost.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("left"), new StylePropertyName("top") };
            _ghost.style.transitionDuration = new List<TimeValue> { new TimeValue(ReturnSeconds, TimeUnit.Second) };
            _ghost.style.left = home.x;
            _ghost.style.top = home.y;
            Label ghost = _ghost;
            ghost.schedule.Execute(() =>
            {
                if (_session.State == DragState.Idle) { ghost.style.display = DisplayStyle.None; }
            }).ExecuteLater((long)(ReturnSeconds * 1000f) + 30L);
        }
    }
}
