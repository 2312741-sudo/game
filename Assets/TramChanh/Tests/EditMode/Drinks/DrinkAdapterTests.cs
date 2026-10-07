using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Orders;
using TramChanh.Tests.EditMode.Orders;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Drinks
{
    public sealed class DrinkAdapterTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private EventBus _events;
        private ManualClock _clock;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private DrinkRecipe _recipe;
        private ItemDefinition _definition;
        private TeaBagItem _bag;
        private ToppingBin _coconut;
        private ToppingBin _lemon;
        private IceBin _ice;
        private WipeInteraction _wipe;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall, _hands, _clock, _events);
            _definition = ScriptableObject.CreateInstance<ItemDefinition>();
            _recipe = ScriptableObject.CreateInstance<DrinkRecipe>();
            var data = new SerializedObject(_recipe);
            data.FindProperty("_itemDefinition").objectReferenceValue = _definition;
            data.FindProperty("_shakeHoldSeconds").floatValue = 1.75f;
            data.FindProperty("_wipeHoldSeconds").floatValue = 0.8f;
            data.ApplyModifiedPropertiesWithoutUndo();
            _bag = CreateBag(1);
            Assert.That(_hands.TryPickUp(_bag), Is.True);
            _coconut = AddStation<ToppingBin>(11);
            _lemon = AddStation<ToppingBin>(12);
            var lemonData = new SerializedObject(_lemon);
            lemonData.FindProperty("_toppingType").enumValueIndex = (int)ToppingType.LemonJelly;
            lemonData.ApplyModifiedPropertiesWithoutUndo();
            _ice = AddStation<IceBin>(13);
            _wipe = AddStation<WipeInteraction>(14);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_definition);
            _events.Dispose();
        }

        [Test]
        public void TC_DRINK_002_PickedUpBagOffersOpenThroughHeldActionContract()
        {
            Assert.That(_bag, Is.InstanceOf<IHeldItemAction>());
            Assert.That(_bag.QueryUse(_context).Availability.IsAvailable, Is.True);
            _bag.ExecuteUse(_context);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Opened));
            Assert.That(_bag.QueryUse(_context).BlockedReasonKey, Is.EqualTo("drink.need_ice_first"));
        }

        [Test]
        public void TC_DRINK_007_WrongStatesAreBlockedAndUnrelatedHeldItemsAreHidden()
        {
            Assert.That(_coconut.Query(_context).BlockedReasonKey, Is.EqualTo("drink.need_open"));
            Assert.That(_lemon.Query(_context).BlockedReasonKey, Is.EqualTo("drink.need_coconut_first"));
            Assert.That(_ice.Query(_context).BlockedReasonKey, Is.EqualTo("drink.need_lemon_first"));
            Assert.That(_wipe.Query(_context).BlockedReasonKey, Is.EqualTo("drink.need_shake_first"));
            _hands.TryRelease();
            AssertStationsHidden();
            _hands.TryPickUp(new BatterMeasureCup());
            AssertStationsHidden();
        }

        [Test]
        public void GT_007_RealAdaptersRunExactSequenceAndBothHoldsRequireCompletion()
        {
            PrepareWithIce();
            Assert.That(_bag.QueryUse(_context).Kind, Is.EqualTo(InteractionKind.Hold));
            Assert.That(_bag.QueryUse(_context).HoldDuration, Is.EqualTo(_recipe.ShakeHoldSeconds));
            var driver = new InteractionActionDriver(_context);
            driver.Begin(_bag);
            Assert.That(_bag.GetComponent<TeaBagStateView>().IsShaking, Is.True);
            _clock.Advance(_recipe.ShakeHoldSeconds / 2f);
            driver.Tick(_bag);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.IceAdded));
            driver.Release();
            Assert.That(_bag.GetComponent<TeaBagStateView>().IsShaking, Is.False);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.IceAdded));
            driver.Begin(_bag);
            _clock.Advance(_recipe.ShakeHoldSeconds);
            driver.Tick(_bag);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Shaken));
            Assert.That(_bag.GetComponent<TeaBagStateView>().IsShaking, Is.False);
            Assert.That(_wipe.Query(_context).HoldDuration, Is.EqualTo(_recipe.WipeHoldSeconds));
            driver.Begin(_wipe);
            Assert.That(_wipe.IsWiping, Is.True);
            _clock.Advance(_recipe.WipeHoldSeconds / 2f);
            driver.Tick(_wipe);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Shaken));
            driver.Release();
            Assert.That(_wipe.IsWiping, Is.False);
            driver.Begin(_wipe);
            _clock.Advance(_recipe.WipeHoldSeconds);
            driver.Tick(_wipe);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Wiped));
            Assert.That(_wipe.IsWiping, Is.False);
        }

        [Test]
        public void TC_DRINK_007_StaleStatePausedAndWrongRoleExecuteNeverAdvanceBag()
        {
            _bag.ExecuteUse(_context);
            Assert.That(_coconut.Query(_context).Availability.IsAvailable, Is.True);
            _clock.Pause();
            _coconut.Execute(_context);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Opened));
            _clock.Resume();
            var lobby = new InteractionContext(new ActorRef(2), ActorRole.Lobby, _hands, _clock, _events);
            _coconut.Execute(lobby);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Opened));
            _coconut.Execute(_context);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.CoconutJellyAdded));
            _coconut.Execute(_context);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.CoconutJellyAdded));
            _hands.TryRelease();
            _bag.ExecuteUse(_context);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.CoconutJellyAdded));
        }

        [Test]
        public void TC_DRINK_007_WipeCancellationDoesNotFinishAReplacementBag()
        {
            PrepareWithIce();
            _bag.ExecuteUse(_context);
            _wipe.OnHoldStarted(_context);
            Assert.That(_wipe.IsWiping, Is.True);
            _hands.TryRelease();
            TeaBagItem replacement = CreateBag(2);
            replacement.Preparation.Open();
            replacement.Preparation.AddCoconutJelly();
            replacement.Preparation.AddLemonJelly();
            replacement.Preparation.AddIce();
            replacement.Preparation.Shake();
            _hands.TryPickUp(replacement);
            Assert.That(_wipe.Query(_context).BlockedReasonKey, Is.EqualTo("interaction.held_item_changed"));
            _wipe.Execute(_context);
            Assert.That(replacement.State, Is.EqualTo(TeaBagState.Shaken));
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Shaken));
            Assert.That(_wipe.IsWiping, Is.False);
        }

        [Test]
        public void TC_INT_007_DrinkQueriesAllocateZeroBytesAndDoNotMutateState()
        {
            _bag.ExecuteUse(_context);
            _bag.QueryUse(_context);
            _coconut.Query(_context);
            _lemon.Query(_context);
            _ice.Query(_context);
            _wipe.Query(_context);
            Assert.That((TestDelegate)(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    _bag.QueryUse(_context);
                    _coconut.Query(_context);
                    _lemon.Query(_context);
                    _ice.Query(_context);
                    _wipe.Query(_context);
                }
            }), Is.Not.AllocatingGCMemory());
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.Opened));
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void TC_DRINK_003_004_RuntimePreparedContractDoesNotPublishOrMutateOnFailure(int completedSteps)
        {
            PrepareWithIce();
            if (completedSteps >= 6)
            {
                _bag.ExecuteUse(_context);
            }
            if (completedSteps >= 7)
            {
                _wipe.Execute(_context);
            }
            int count = 0;
            using var subscription = _events.Subscribe<DrinkStepCompleted>(_ => count++);
            if (_bag.IsFinished)
            {
                PreparedItemContractAssertions.AssertReadyTransition(_bag, () => _bag.State, TeaBagState.Ready, () => count);
            }
            else
            {
                PreparedItemContractAssertions.AssertUnfinishedTransition(_bag, () => _bag.State, () => count);
            }
        }

        [Test]
        public void TC_DRINK_007_MissingOrInvalidRecipeBlocksTimedActions()
        {
            PrepareWithIce();
            var serialized = new SerializedObject(_recipe);
            serialized.FindProperty("_shakeHoldSeconds").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_bag.QueryUse(_context).BlockedReasonKey, Is.EqualTo("drink.recipe.not_configured"));
            _bag.ExecuteUse(_context);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.IceAdded));
        }

        [Test]
        public void TC_DRINK_015_RuntimeUsesSameInstanceUnfinishedFinishReadyLifecycle()
        {
            PrepareWithIce(); int count = 0;
            using var step = _events.Subscribe<DrinkStepCompleted>(_ => count++);
            using var ready = _events.Subscribe<OrderItemStatusChanged>(_ => count++);
            PreparedItemContractAssertions.AssertLifecycle(_bag, () => { _bag.ExecuteUse(_context); _wipe.Execute(_context); }, () => _bag.State, TeaBagState.Ready, () => count);
            Assert.That(count, Is.EqualTo(2), "Only Shake and Wipe adapter notifications precede the silent Ready mutation.");
        }
        [Test]
        public void TC_DRINK_014_AdaptersPublishExactlySixD2ThroughD7Steps()
        {
            var observed = new List<TeaBagState>();
            using var step = _events.Subscribe<DrinkStepCompleted>(e => observed.Add(e.State));
            PrepareWithIce(); _bag.ExecuteUse(_context); _wipe.Execute(_context); _bag.MarkReady();
            Assert.That(observed, Is.EqualTo(new[] { TeaBagState.Opened, TeaBagState.CoconutJellyAdded, TeaBagState.LemonJellyAdded, TeaBagState.IceAdded, TeaBagState.Shaken, TeaBagState.Wiped }));
        }

        private void PrepareWithIce()
        {
            _bag.ExecuteUse(_context);
            _coconut.Execute(_context);
            _lemon.Execute(_context);
            _ice.Execute(_context);
            Assert.That(_bag.State, Is.EqualTo(TeaBagState.IceAdded));
        }

        private void AssertStationsHidden()
        {
            Assert.That(_coconut.Query(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            Assert.That(_lemon.Query(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            Assert.That(_ice.Query(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            Assert.That(_wipe.Query(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
        }

        private TeaBagItem CreateBag(int id)
        {
            var root = new GameObject("Bag");
            _objects.Add(root);
            root.AddComponent<TeaBagStateView>();
            var item = root.AddComponent<TeaBagItem>();
            var model = new DrinkPreparation(new PreparationId(id));
            model.TryPickUp();
            item.Initialize(model, _recipe, _events);
            return item;
        }

        private T AddStation<T>(int id) where T : MonoBehaviour
        {
            var root = new GameObject(typeof(T).Name);
            _objects.Add(root);
            var station = root.AddComponent<T>();
            var data = new SerializedObject(station);
            data.FindProperty("_id").intValue = id;
            data.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }

        private sealed class BatterMeasureCup : IHoldable
        {
            public Transform HandGrip => null;
            public Transform PlacementPoint => null;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }
    }
}
