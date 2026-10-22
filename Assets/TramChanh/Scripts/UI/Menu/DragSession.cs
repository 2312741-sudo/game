using System;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// State of one drag gesture: press, move past a small threshold, hover over zones, release. Pure C#.
    /// The pointer manipulator only forwards events here and draws what this says.
    /// </summary>
    public sealed class DragSession
    {
        public const float StartDistance = 6f;
        private readonly DragRules _rules;
        private float _startX;
        private float _startY;

        public DragSession(DragRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public DragState State { get; private set; }
        public DragPayload Payload { get; private set; }
        public DropZone Zone { get; private set; }

        public void Press(DragPayload payload, float x, float y)
        {
            Payload = payload;
            _startX = x;
            _startY = y;
            Zone = DropZone.None;
            State = DragState.Pressed;
        }

        /// <summary>Feeds a pointer move. Returns true while really dragging (after the threshold).</summary>
        public bool Move(float x, float y)
        {
            if (State == DragState.Idle) { return false; }
            if (State == DragState.Pressed)
            {
                float dx = x - _startX;
                float dy = y - _startY;
                if (dx * dx + dy * dy < StartDistance * StartDistance) { return false; }
                State = DragState.Dragging;
            }
            return true;
        }

        /// <summary>Sets the zone under the pointer and tells whether a drop there would buy. Only while dragging.</summary>
        public DropVerdict Hover(DropZone zone)
        {
            Zone = zone;
            return State == DragState.Dragging ? _rules.Evaluate(Payload, zone) : DropVerdict.OutsideZone;
        }

        /// <summary>Ends the gesture. A drag buys through the rules when the zone is valid; a click that never moved does nothing.</summary>
        public DragRelease Release(DropZone zone)
        {
            bool wasDrag = State == DragState.Dragging;
            DragPayload payload = Payload;
            State = DragState.Idle;
            Zone = DropZone.None;
            if (!wasDrag) { return new DragRelease(false, new DropOutcome(DropVerdict.OutsideZone, false, string.Empty)); }
            return new DragRelease(true, _rules.Drop(payload, zone));
        }

        /// <summary>Abandons the gesture (pointer capture lost). Nothing is bought.</summary>
        public void Cancel()
        {
            State = DragState.Idle;
            Zone = DropZone.None;
        }
    }
}
