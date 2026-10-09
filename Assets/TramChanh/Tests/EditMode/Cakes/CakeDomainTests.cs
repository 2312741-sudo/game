using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Cakes
{
    public sealed class CakeDomainTests
    {
        private CakeRecipe _recipe;
        private ItemDefinition _definition;
        private FakeQueue _queue;
        private ManualClock _clock;
        private CakePreparation _cake;
        private GrillModel _grill;
        [SetUp] public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<ItemDefinition>(); Set(_definition, "_id", "test.cake.small"); Set(_definition, "_kind", ItemKind.Cake);
            _recipe = ScriptableObject.CreateInstance<CakeRecipe>(); Set(_recipe, "_itemDefinition", _definition);
            Set(_recipe, "_targetBatterMl", 120f); Set(_recipe, "_batterToleranceMl", 10f); Set(_recipe, "_cookedThreshold", 4f); Set(_recipe, "_burnThreshold", 12f);
            Set(_recipe, "_openLidHeatFactor", .25f); Set(_recipe, "_sauce", "test.sauce"); Set(_recipe, "_cutHoldSeconds", 1f); Set(_recipe, "_sauceHoldSeconds", 1f); Set(_recipe, "_rollHoldSeconds", 1f);
            Set(_recipe, "_deviationPenaltyPerMl", 1f); _queue = new FakeQueue(); _clock = new ManualClock();
            _cake = new CakePreparation(_queue.Binding, _queue, _recipe); _grill = new GrillModel(_clock, 2d); _grill.PowerOn();
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_recipe); Object.DestroyImmediate(_definition); }
        [Test] public void TC_CAKE_StrictSequenceRejectsEveryPrematureStepWithoutMutation()
        {
            Assert.That(_cake.Cut().ReasonKey, Is.EqualTo("cake.need_flip_first")); Assert.That(_cake.Sauce("test.sauce").IsSuccess, Is.False);
            Assert.That(_cake.RollVertically().IsSuccess, Is.False); Assert.That(_cake.Wrap().IsSuccess, Is.False); Assert.That(_cake.MarkReady().IsSuccess, Is.False);
            Assert.That(_cake.State, Is.EqualTo(CakeState.Waiting)); Assert.That(_grill.Pour(_cake).IsSuccess, Is.False);
        }
        [TestCase(-1f)] [TestCase(501f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void TC_CAKE_InvalidMeasureIsAtomic(float amount)
        { _cake.Measure(120f); Assert.That(_cake.Measure(amount).IsSuccess, Is.False); Assert.That(_cake.Measurement.MeasuredMl, Is.EqualTo(120f)); }
        [TestCase(110f, BatterMeasureResult.WithinTolerance)] [TestCase(130f, BatterMeasureResult.WithinTolerance)]
        [TestCase(109f, BatterMeasureResult.Under)] [TestCase(131f, BatterMeasureResult.Over)]
        public void TC_CAKE_PerRecipeToleranceIncludesBothEdges(float ml, BatterMeasureResult result)
        { Assert.That(_cake.Measure(ml).IsSuccess, Is.True); Assert.That(_cake.Measurement.Result, Is.EqualTo(result)); }
        [Test] public void TC_CAKE_EmptyAndTopupRemainMeasurementCorrections()
        { _cake.Measure(60f); _cake.Measure(120f); Assert.That(_cake.Measurement.MeasuredMl, Is.EqualTo(120f)); _cake.Measure(0f); Assert.That(_cake.State, Is.EqualTo(CakeState.Waiting)); Assert.That(_cake.CanPour().IsSuccess, Is.False); }
        [Test] public void TC_CAKE_BlockPolicyRejectsUntilCorrected()
        { Set(_recipe, "_outOfTolerancePolicy", BatterOutOfTolerancePolicy.BlockPour); _cake.Measure(80f); Assert.That(_cake.CanPour().ReasonKey, Is.EqualTo("cake.batter_out_of_tolerance")); _cake.Measure(120f); Assert.That(_cake.CanPour().IsSuccess, Is.True); }
        [Test] public void TC_CAKE_AllowPenaltyPreservesActualPouredAmount()
        { _cake.Measure(80f); Open(); Assert.That(_grill.Pour(_cake).IsSuccess, Is.True); Assert.That(_cake.PouredMl, Is.EqualTo(80f)); Assert.That(_cake.Quality, Is.EqualTo(70)); Assert.That(_cake.Measure(120f).IsSuccess, Is.False); }
        [Test] public void TC_CAKE_GrillPreheatLidAndOccupancyGuard()
        { Assert.That(_grill.OpenLid().ReasonKey, Is.EqualTo("grill.preheating")); _cake.Measure(120f); Assert.That(_grill.Pour(_cake).IsSuccess, Is.False); Open(); _grill.Pour(_cake); Assert.That(_grill.Pour(_cake).ReasonKey, Is.EqualTo("grill.occupied")); Assert.That(_grill.CanFlip().ReasonKey, Is.EqualTo("cake.not_cooked")); }
        [Test] public void TC_CAKE_PausedClockCannotCookOrBurn()
        { CookStart(); _clock.Pause(); _clock.Advance(100d); _grill.Advance(); Assert.That(_cake.Doneness, Is.EqualTo(0d)); _clock.Resume(); _clock.Advance(4d); _grill.Advance(); Assert.That(_cake.State, Is.EqualTo(CakeState.Cooked)); }
        [Test] public void TC_CAKE_OpenLidUsesConfiguredHeatAndEventuallyBurns()
        { CookStart(); _grill.OpenLid(); _clock.Advance(8d); _grill.Advance(); Assert.That(_cake.Doneness, Is.EqualTo(2d)); _clock.Advance(40d); _grill.Advance(); Assert.That(_cake.State, Is.EqualTo(CakeState.Ruined)); Assert.That(_cake.IsFinished, Is.False); Assert.That(_cake.Quality, Is.Zero); }
        [Test] public void TC_CAKE_FlipLeavesGrillAndStopsCookingThenExactFinishingSequence()
        {
            CookStart(); _clock.Advance(4d); _grill.Advance(); Assert.That(_grill.CanFlip().ReasonKey, Is.EqualTo("grill.lid_closed")); _grill.OpenLid();
            Assert.That(_grill.Flip(new FakeFlip()).IsSuccess, Is.True); Assert.That(_grill.CakeOnPlate, Is.Null); _clock.Advance(100d); _grill.Advance(); Assert.That(_cake.Doneness, Is.EqualTo(4d));
            Assert.That(_cake.Cut().IsSuccess, Is.True); Assert.That(_cake.Cut().IsSuccess, Is.False); Assert.That(_cake.Sauce("wrong").ReasonKey, Is.EqualTo("cake.wrong_sauce")); Assert.That(_cake.State, Is.EqualTo(CakeState.Cut));
            Assert.That(_cake.Sauce("test.sauce").IsSuccess, Is.True); Assert.That(_cake.RollVertically().IsSuccess, Is.True); Assert.That(_cake.Wrap().IsSuccess, Is.True);
            Assert.That(_cake.IsFinished, Is.True); Assert.That(_cake.MarkReady().IsSuccess, Is.True); Assert.That(_cake.MarkReady().ReasonKey, Is.EqualTo("ready.already_ready"));
        }
        [Test] public void TC_CAKE_LostBindingBlocksMeasurementAndReady()
        { _cake.Measure(120f); _queue.Live = false; Assert.That(_cake.BoundItem.IsValid, Is.False); Assert.That(_cake.Measure(130f).ReasonKey, Is.EqualTo("ready.no_order")); Assert.That(_cake.MarkReady().IsSuccess, Is.False); Assert.That(_cake.State, Is.EqualTo(CakeState.BatterMeasured)); }
        [Test] public void TC_CAKE_CatalogResolvesClaimedDefinitionWithoutIntakeCapability()
        { var catalog = new CakeRecipeCatalog(_queue, new[] { _recipe }); Assert.That(catalog.TryResolve(_queue.Binding, out var recipe), Is.True); Assert.That(recipe, Is.SameAs(_recipe)); _queue.Live = false; Assert.That(catalog.TryResolve(_queue.Binding, out _), Is.False); }
        [Test] public void TC_CAKE_CupCapacityIs500AndUnconfiguredRecipeIsRejected()
        { Assert.That(MeasureCupDefinition.NominalCapacityMl, Is.EqualTo(500f)); Set(_recipe, "_targetBatterMl", 0f); Assert.That(_recipe.IsConfigured, Is.False); Assert.Throws<ArgumentException>(() => new CakePreparation(_queue.Binding, _queue, _recipe)); }
        [Test] public void TC_CAKE_UninitializedCupIsBlockedInsteadOfThrowing()
        {
            var cupObject = new GameObject("PF_BatterMeasureCup_500ml"); var events = new EventBus();
            try
            {
                var cup = cupObject.AddComponent<BatterMeasureCup>(); var hands = new HeldItemSlot(events);
                var context = new InteractionContext(new ActorRef(1), ActorRole.Stall, hands, _clock, events);
                Assert.That(cup.Query(context).BlockedReasonKey, Is.EqualTo("cake.station_uninitialized"));
                Assert.That(hands.TryPickUp(cup), Is.True);
                Assert.That(cup.QueryUse(context).BlockedReasonKey, Is.EqualTo("cake.station_uninitialized"));
                Assert.DoesNotThrow(() => cup.ExecuteUse(context));
            }
            finally { events.Dispose(); Object.DestroyImmediate(cupObject); }
        }
        private void Open() { _clock.Advance(2d); _grill.Advance(); Assert.That(_grill.OpenLid().IsSuccess, Is.True); }
        private void CookStart() { _cake.Measure(120f); Open(); _grill.Pour(_cake); Assert.That(_grill.CloseLid().IsSuccess, Is.True); }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private sealed class FakeFlip : IFlipAction
        { public Transform Destination => null; public Availability CanFlip(GrillModel grill, CakePreparation cake) => Availability.Available; public void Present(CakeItem item) { } }
        private sealed class FakeQueue : IStallTicketQueue, IOrderItemCatalog
        {
            public bool Live = true; public OrderItemRef Binding = new OrderItemRef(new OrderId(1), new OrderItemId(2), new PreparationId(3));
            public IReadOnlyList<OrderId> Tickets => Array.Empty<OrderId>(); public bool HasPending(ItemKind kind) => false;
            public Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId id) => Result<OrderItemRef>.Fail("no.ticket");
            public Result Release(OrderItemRef item) { Live = false; return Result.Success(); }
            public bool IsBound(OrderItemRef item) => Live && item == Binding;
            public bool TryGetItemDefinition(OrderItemRef item, out string definition) { definition = IsBound(item) ? "test.cake.small" : null; return definition != null; }
        }
    }
}
