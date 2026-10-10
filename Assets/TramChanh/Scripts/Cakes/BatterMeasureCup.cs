using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using UnityEngine;

namespace TramChanh.Cakes
{
    public sealed class BatterMeasureCup : MonoBehaviour, IHoldable, IInteractable, IHeldItemAction, IPreparationFeedback
    {
        [SerializeField] private Transform _handGrip;
        [SerializeField] private Transform _liquid;
        [SerializeField] private MeasureCupDefinition _definition;
        [SerializeField] private Transform _home;
        private CakeStation _station;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        public Transform HandGrip => _handGrip;
        public MeasureCupDefinition Definition => _definition;
        public InteractableId Id => new InteractableId(1101);
        public Transform InteractionPoint => _handGrip;
        public float MeasuredMl => _station?.Current?.Measurement.MeasuredMl ?? 0f;
        public string PreparationStateKey => _station?.Current?.PreparationStateKey ?? "cake.state.waiting";
        public string NextActionKey => _station?.Current?.NextActionKey ?? "cake.fill";
        public void Initialize(CakeStation station)
        { _station = station; _homePosition = transform.localPosition; _homeRotation = transform.localRotation; }
        public InteractionQuery Query(InteractionContext context)
        {
            Availability guard = CakeStation.GuardActor(context);
            if (guard.IsAvailable && _station == null) { guard = Availability.Blocked("cake.station_uninitialized"); }
            if (guard.IsAvailable && _station != null && _station.Current != null && _station.Current.State != CakeState.Waiting && _station.Current.State != CakeState.BatterMeasured) { guard = Availability.Blocked("cake.station_busy"); }
            if (guard.IsAvailable && context.Hands.Current != null) { guard = Availability.Blocked("hands.full"); }
            return new InteractionQuery(guard, "cake.cup.pickup");
        }
        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable) { context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey)); return; }
            context.Hands.TryPickUp(this);
        }
        public void OnPickedUp(IHeldItemSlot hands) { }
        public void OnReleased() { }
        public InteractionQuery QueryUse(InteractionContext context)
        {
            if (!ReferenceEquals(context.Hands.Current, this)) { return new InteractionQuery(Availability.Hidden, "cake.empty_back"); }
            Availability guard = CakeStation.GuardActor(context);
            if (guard.IsAvailable && _station == null) { guard = Availability.Blocked("cake.station_uninitialized"); }
            // MAIN-102: an empty cup (no measurement / claim in progress) can be put back on its stand. Without this the
            // cup could only leave the hands by pouring, so picking it up with no cake ticket blocked the hands forever
            // (no order entry, no tea bag, hence no cake ticket could ever arrive).
            if (_station != null && _station.Current == null) { return new InteractionQuery(guard, ReturnPromptKey); }
            if (guard.IsAvailable && _station.Current != null && _station.Current.State != CakeState.Waiting && _station.Current.State != CakeState.BatterMeasured)
            { guard = Availability.Blocked("cake.already_poured"); }
            return new InteractionQuery(guard, "cake.empty_back");
        }
        public const string ReturnPromptKey = "cake.cup.return";
        public void ExecuteUse(InteractionContext context)
        {
            InteractionQuery query = QueryUse(context);
            if (!query.Availability.IsAvailable) { context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey ?? "cake.cup_not_held")); return; }
            if (query.PromptKey == ReturnPromptKey) { context.Hands.TryRelease(); ReturnHome(); return; }
            if (_station.Current != null) { _station.EmptyMeasurement(); } ApplyLevel(0f);
        }
        internal void ReturnHome()
        {
            transform.SetParent(_home, false); transform.localPosition = _homePosition; transform.localRotation = _homeRotation;
            foreach (var node in GetComponentsInChildren<Transform>(true)) { node.gameObject.layer = Core.GroundTruth.TramChanhLayers.InteractableIndex; }
            foreach (var collider in GetComponentsInChildren<Collider>(true)) { collider.enabled = true; }
            ApplyLevel(0f);
        }
        internal void ApplyLevel(float ml)
        {
            if (_liquid == null || _definition == null) { return; }
            float level = _definition.Level01(ml); _liquid.gameObject.SetActive(level > 0f);
            Vector3 scale = _liquid.localScale; scale.y = Mathf.Max(.001f, .1f * level); _liquid.localScale = scale;
            Vector3 position = _liquid.localPosition; position.y = .005f + .05f * level; _liquid.localPosition = position;
        }
    }
}
