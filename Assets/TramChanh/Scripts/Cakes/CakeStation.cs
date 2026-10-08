using TramChanh.Content;
using System;
using TramChanh.Core;
using TramChanh.Core.Provisional;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Cakes
{
    /// <summary>Explicit station seam. Production requires a sent Cake ticket and its resolved recipe.</summary>
    public sealed class CakeStation : MonoBehaviour
    {
        [SerializeField] private BatterMeasureCup _cup;
        [SerializeField] private CakeItem _cakePrefab;
        [SerializeField] private Transform _cakePlacement;
        [SerializeField] private Transform _lidPivot;
        [SerializeField] private Transform _scissors;
        [SerializeField] private Transform _sauceBag;
        [SerializeField] private Transform _wrappingArea;
        [SerializeField] private MonoBehaviour _flipAction;
        [SerializeField, Tbd("DEC-007")] private float _preheatSeconds;
        [SerializeField, Tbd("DEC-013")] private float _openLidDegrees;
        private Quaternion _closedLidRotation;
        private Quaternion _scissorsRest, _sauceRest;
        private IStallTicketQueue _tickets;
        private IIdGenerator _ids;
        private IEventBus _events;
        private ICakeRecipeCatalog _recipes;
        public IGameClock Clock { get; private set; }
        public CakePreparation Current { get; private set; }
        public CakeItem CurrentItem { get; private set; }
        public GrillModel Grill { get; private set; }
        public BatterMeasureCup Cup => _cup;
        public Transform CakePlacement => _cakePlacement;
        public Transform WrappingArea => _wrappingArea;
        public void Initialize(IStallTicketQueue queue, IIdGenerator ids, IEventBus events, IGameClock clock, ICakeRecipeCatalog recipes)
        {
            if (Grill != null) { throw new InvalidOperationException("Cake station is already initialized."); }
            _tickets = queue ?? throw new ArgumentNullException(nameof(queue)); _ids = ids ?? throw new ArgumentNullException(nameof(ids));
            _events = events ?? throw new ArgumentNullException(nameof(events)); Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            if (_cup == null || _cup.Definition == null || !_cup.Definition.IsConfigured || _cakePrefab == null || _cakePlacement == null
                || _lidPivot == null || _wrappingArea == null || _flipAction is not IFlipAction || !CakeRecipe.Finite(_openLidDegrees))
            { throw new InvalidOperationException("Cake station anchors/tools/tuning are not configured."); }
            _closedLidRotation = _lidPivot.localRotation; _scissorsRest = _scissors != null ? _scissors.localRotation : Quaternion.identity;
            _sauceRest = _sauceBag != null ? _sauceBag.localRotation : Quaternion.identity;
            _cup.Initialize(this); Grill = new GrillModel(clock, _preheatSeconds); Grill.PowerOn(); Advance();
        }
        public static Availability GuardActor(InteractionContext context)
        {
            if ((context.Role & ActorRole.Stall) == 0) { return Availability.Blocked("role.not_stall"); }
            return context.Clock.IsPaused ? Availability.Blocked("game.paused") : Availability.Available;
        }
        public void Advance()
        {
            if (Grill == null) { return; }
            if (Current != null && !Current.BoundItem.IsValid)
            {
                if (CurrentItem != null) { Destroy(CurrentItem.gameObject); }
                Current = null; CurrentItem = null; Grill.ClearInvalidated(); _cup.ApplyLevel(0f);
            }
            CakeState before = Current?.State ?? CakeState.Waiting; Grill.Advance();
            if (_lidPivot != null) { _lidPivot.localRotation = _closedLidRotation * Quaternion.Euler(Grill.State == GrillState.Open ? -_openLidDegrees : 0f, 0f, 0f); }
            if (CurrentItem != null) { CurrentItem.Apply(); }
            if (Current != null && Current.State != before) { PublishStep(); }
        }
        private void Update() => Advance();
        public InteractionQuery Query(InteractionContext context, CakeStationAction action, string sauce = null)
        {
            string prompt = action switch { CakeStationAction.Fill => "cake.fill", CakeStationAction.Grill => "grill.open",
                CakeStationAction.RollArea => Current?.State == CakeState.Sauced ? "cake.roll_vertical" : "cake.cut",
                CakeStationAction.Sauce => "cake.sauce", _ => "cake.wrap" };
            Availability guard = GuardActor(context);
            InteractionKind kind = action == CakeStationAction.Fill ? InteractionKind.Continuous : InteractionKind.Press;
            float duration = 0f;
            if (!guard.IsAvailable) { return new InteractionQuery(guard, prompt, kind); }
            if (Grill == null) { return new InteractionQuery(Availability.Blocked("cake.station_uninitialized"), prompt); }
            if (Current != null && !Current.BoundItem.IsValid) { return new InteractionQuery(Availability.Blocked("ready.no_order"), prompt); }
            string reason = null;
            switch (action)
            {
                case CakeStationAction.Fill:
                    if (!ReferenceEquals(context.Hands.Current, _cup)) { reason = "cake.cup_not_held"; }
                    else if (Current == null && !_tickets.HasPending(ItemKind.Cake)) { reason = "stall.no_ticket.cake"; }
                    else if (Current != null && Current.State != CakeState.Waiting && Current.State != CakeState.BatterMeasured) { reason = "cake.already_poured"; }
                    break;
                case CakeStationAction.Grill:
                    if (Grill.State == GrillState.Preheating || Grill.State == GrillState.Off) { reason = "grill.preheating"; break; }
                    if (Grill.State == GrillState.Open)
                    {
                        if (Grill.CakeOnPlate == null && ReferenceEquals(context.Hands.Current, _cup))
                        { prompt = "cake.pour"; Result pour = Grill.CanPour(Current); if (!pour.IsSuccess) { reason = pour.ReasonKey; } }
                        else if (Grill.CakeOnPlate?.State == CakeState.Cooked)
                        { prompt = "cake.flip"; if (context.Hands.Current != null) { reason = "hands.full"; } else { var can = ((IFlipAction)_flipAction).CanFlip(Grill, Current); reason = can.IsAvailable ? null : can.ReasonKey; } }
                        else if (Grill.CakeOnPlate?.State == CakeState.Ruined)
                        { prompt = "cake.remove_burnt"; if (context.Hands.Current != null) { reason = "hands.full"; } }
                        else { prompt = "grill.close"; if (Grill.CakeOnPlate != null && context.Hands.Current != null) { reason = "hands.full"; } }
                    }
                    break;
                case CakeStationAction.RollArea:
                    if (context.Hands.Current != null) { reason = "hands.full"; break; }
                    if (Current?.State == CakeState.Flipped) { kind = InteractionKind.Hold; duration = Current.Recipe.CutHoldSeconds; }
                    else if (Current?.State == CakeState.Sauced) { kind = InteractionKind.Hold; duration = Current.Recipe.RollHoldSeconds; }
                    else { reason = Current?.State == CakeState.Cut ? "cake.need_sauce_first" : "cake.need_flip_first"; }
                    break;
                case CakeStationAction.Sauce:
                    if (context.Hands.Current != null) { reason = "hands.full"; }
                    else if (Current?.State != CakeState.Cut) { reason = "cake.need_cut_first"; }
                    else if (!string.Equals(Current.Recipe.Sauce, sauce, StringComparison.Ordinal)) { reason = "cake.wrong_sauce"; }
                    else { kind = InteractionKind.Hold; duration = Current.Recipe.SauceHoldSeconds; }
                    break;
                case CakeStationAction.Wrap:
                    if (Current?.State != CakeState.Rolled) { reason = "cake.need_roll_first"; }
                    else if (context.Hands.Current != null) { reason = "hands.full"; }
                    break;
            }
            return new InteractionQuery(reason == null ? Availability.Available : Availability.Blocked(reason), prompt, kind, duration);
        }
        public Result Measure(InteractionContext context, float heldSeconds)
        {
            var query = Query(context, CakeStationAction.Fill);
            if (!query.Availability.IsAvailable) { return Result.Fail(query.BlockedReasonKey); }
            if (!CakeRecipe.Positive(heldSeconds)) { return Result.Fail("cake.invalid_measurement"); }
            bool claimed = false;
            if (Current == null)
            {
                var claim = _tickets.ClaimNext(ItemKind.Cake, new PreparationId(_ids.Next()));
                if (!claim.IsSuccess) { return Result.Fail(claim.ReasonKey); }
                if (!_recipes.TryResolve(claim.Value, out CakeRecipe recipe)) { _tickets.Release(claim.Value); return Result.Fail("cake.recipe_not_configured"); }
                Current = new CakePreparation(claim.Value, _tickets, recipe); claimed = true;
            }
            float ml = Mathf.Min(_cup.Definition.CapacityMl, Current.Measurement.MeasuredMl + _cup.Definition.FillRateMlPerSecond * heldSeconds);
            Result result = Current.Measure(ml);
            if (!result.IsSuccess && claimed) { _tickets.Release(Current.BoundItem); Current = null; }
            if (result.IsSuccess) { _events.Publish(new BatterMeasured(Current.PreparationId, Current.Measurement)); PublishStep(); }
            return result;
        }
        internal void EmptyMeasurement()
        {
            if (Current == null) { return; }
            var previous = Current;
            Result result = previous.Measure(0f);
            if (!result.IsSuccess) { return; }
            OrderItemRef claim = previous.BoundItem;
            Result released = _tickets.Release(claim);
            if (!released.IsSuccess) { return; }
            Current = null;
            _events.Publish(new BatterMeasured(previous.PreparationId, previous.Measurement));
            _events.Publish(new CakeStepCompleted(previous.PreparationId, CakeState.Waiting));
        }
        public void Execute(InteractionContext context, CakeStationAction action, string sauce, InteractableId id)
        {
            Advance(); InteractionQuery query = Query(context, action, sauce);
            if (!query.Availability.IsAvailable) { context.Events.Publish(new ActionBlocked(id, query.BlockedReasonKey)); return; }
            Result result = Result.Success();
            switch (action)
            {
                case CakeStationAction.Fill: return;
                case CakeStationAction.Grill:
                    if (Grill.State != GrillState.Open) { result = Grill.OpenLid(); }
                    else if (Grill.CakeOnPlate == null && ReferenceEquals(context.Hands.Current, _cup))
                    {
                        result = Grill.Pour(Current);
                        if (result.IsSuccess)
                        {
                            CurrentItem = Instantiate(_cakePrefab, _cakePlacement, false); CurrentItem.Initialize(Current, _events);
                            context.Hands.TryRelease(); _cup.ReturnHome();
                        }
                    }
                    else if (Grill.CakeOnPlate?.State == CakeState.Cooked)
                    {
                        result = Grill.Flip((IFlipAction)_flipAction);
                        if (result.IsSuccess) { ((IFlipAction)_flipAction).Present(CurrentItem); }
                    }
                    else if (Grill.CakeOnPlate?.State == CakeState.Ruined)
                    {
                        CakePreparation burnt = Current; result = Grill.RemoveBurnt();
                        if (result.IsSuccess)
                        {
                            _tickets.Release(burnt.BoundItem); context.Hands.TryPickUp(CurrentItem); Current = null; CurrentItem = null;
                        }
                    }
                    else { result = Grill.CloseLid(); }
                    break;
                case CakeStationAction.RollArea:
                    result = Current.State == CakeState.Flipped ? Current.Cut() : Current.RollVertically(); break;
                case CakeStationAction.Sauce: result = Current.Sauce(sauce); break;
                case CakeStationAction.Wrap:
                    result = Current.Wrap();
                    if (result.IsSuccess && !context.Hands.TryPickUp(CurrentItem)) { Current.UndoWrap(); result = Result.Fail("hands.full"); }
                    break;
            }
            SetToolMotion(action, false);
            if (!result.IsSuccess) { context.Events.Publish(new ActionBlocked(id, result.ReasonKey)); }
            else if (Current != null)
            {
                CurrentItem?.Apply(); PublishStep();
                if (Current.State == CakeState.Wrapped) { Current = null; CurrentItem = null; }
            }
            Advance();
        }
        public void SetToolMotion(CakeStationAction action, bool active)
        {
            if (action == CakeStationAction.RollArea && _scissors != null) { _scissors.localRotation = _scissorsRest * Quaternion.Euler(0f, active ? 25f : 0f, 0f); }
            if (action == CakeStationAction.Sauce && _sauceBag != null) { _sauceBag.localRotation = _sauceRest * Quaternion.Euler(active ? -25f : 0f, 0f, 0f); }
        }
        private void PublishStep() => _events.Publish(new CakeStepCompleted(Current.PreparationId, Current.State));
    }
}
