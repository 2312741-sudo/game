using TramChanh.Core;
using TramChanh.Interaction;
using UnityEngine;

namespace TramChanh.Cakes
{
    public enum CakeStationAction { Fill, Grill, RollArea, Sauce, Wrap }
    public sealed class CakeStationPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private CakeStation _station;
        [SerializeField] private CakeStationAction _action;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private int _id;
        [SerializeField] private string _sauce;
        private BatterMeasureCup _fillingCup;
        private double _fillStarted;
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public CakeStationAction Action => _action;
        public InteractionQuery Query(InteractionContext context) => _station.Query(context, _action, _sauce);
        public void Execute(InteractionContext context) => _station.Execute(context, _action, _sauce, Id);
        public void OnHoldStarted(InteractionContext context)
        {
            if (_action != CakeStationAction.Fill) { _station.SetToolMotion(_action, true); return; }
            if (!Query(context).Availability.IsAvailable) { return; }
            _fillingCup = context.Hands.Current as BatterMeasureCup; _fillStarted = context.Clock.Now;
        }
        private void Update()
        {
            if (_fillingCup == null) { return; }
            float elapsed = (float)(_station.Clock.Now - _fillStarted);
            _fillingCup.ApplyLevel(Mathf.Min(_fillingCup.Definition.CapacityMl, _fillingCup.MeasuredMl + elapsed * _fillingCup.Definition.FillRateMlPerSecond));
        }
        public void OnContinuousReleased(InteractionContext context, float heldSeconds)
        {
            var cup = _fillingCup; _fillingCup = null;
            // Driver may release after ownership changes or focus loss. Never fill a different hand's cup.
            if (cup == null || !ReferenceEquals(context.Hands.Current, cup)) { if (cup != null) { cup.ApplyLevel(cup.MeasuredMl); } return; }
            Result result = _station.Measure(context, heldSeconds);
            cup.ApplyLevel(cup.MeasuredMl);
            if (!result.IsSuccess) { context.Events.Publish(new ActionBlocked(Id, result.ReasonKey)); }
        }
        public void OnHoldCancelled(InteractionContext context) { _station.SetToolMotion(_action, false); }
    }
}
