using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Stall.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Drinks
{
    /// <summary>
    /// MAIN-002: the canonical drink sequence from ticket to handoff through the real rack inventory,
    /// real drink adapters, real Ready counter/pickup adapters and the real order services.
    /// </summary>
    public sealed class DrinkReadyHandoffTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private DrinkRecipe _recipe;
        private ItemDefinition _definition;
        private TeaRackInventory _rack;
        private TeaBagItem[] _rackBags;
        private ToppingBin _coconut;
        private ToppingBin _lemon;
        private IceBin _ice;
        private WipeInteraction _wipe;
        private ReadyCounterPoint _counter;
        private ReadyOrderPickupPoint _pickup;
        private Transform _drinkPlacement;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            var content = new ContentDatabase(new[] { new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink) });
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), content, 1, 1);
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall | ActorRole.Lobby, _hands, _clock, _events);
            _definition = ScriptableObject.CreateInstance<ItemDefinition>();
            _recipe = ScriptableObject.CreateInstance<DrinkRecipe>();
            Set(_recipe, "_itemDefinition", _definition);
            Set(_recipe, "_shakeHoldSeconds", 1.75f);
            Set(_recipe, "_wipeHoldSeconds", 0.8f);
            _objects.Add(_recipe);
            _objects.Add(_definition);

            _rack = new TeaRackInventory(2, 2, new SequentialIdGenerator());
            _rackBags = new TeaBagItem[_rack.Capacity];
            for (int i = 0; i < _rack.Capacity; i++)
            {
                _rackBags[i] = CreateBag(_rack.BagAt(i));
            }
            _coconut = Station<ToppingBin>(11);
            _lemon = Station<ToppingBin>(12);
            Set(_lemon, "_toppingType", ToppingType.LemonJelly);
            _ice = Station<IceBin>(13);
            _wipe = Station<WipeInteraction>(14);

            _counter = Station<ReadyCounterPoint>(15);
            _drinkPlacement = new GameObject("DrinkPlacement").transform;
            _objects.Add(_drinkPlacement.gameObject);
            var cakePlacement = new GameObject("CakePlacement").transform;
            _objects.Add(cakePlacement.gameObject);
            Set(_counter, "_interactionPoint", _counter.transform);
            Set(_counter, "_drinkPlacementPoint", _drinkPlacement);
            Set(_counter, "_cakePlacementPoint", cakePlacement);
            _counter.Initialize(_shelf, _events);
            _pickup = Station<ReadyOrderPickupPoint>(16);
            Set(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf, _orders, _events);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hands.Current is Component held && held != null)
            {
                Object.DestroyImmediate(held.gameObject);
            }
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }
            _objects.Clear();
            Assert.That(_orders.ObserverFaults.Count, Is.Zero);
            _shelf.Dispose();
            _events.Dispose();
        }

        [Test]
        public void MAIN_002_TicketToHandoffRunsOnlyTheCanonicalSequenceAtRealAdapters()
        {
            // Step 1: no ticket, no bag.
            Assert.That(TakeBag().ReasonKey, Is.EqualTo("stall.no_ticket.drink"));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_rack.Stock, Is.EqualTo(2));
            OrderId order = SendDrinkOrder(1);

            // Step 2: the pre-portioned bag from the rack claims the ticket.
            var released = new List<HeldItemChanged>();
            using var heldChanges = _events.Subscribe<HeldItemChanged>(released.Add);
            Assert.That(TakeBag().IsSuccess, Is.True);
            var bag = _hands.Current as TeaBagItem;
            Assert.That(bag, Is.SameAs(_rackBags[0]));
            Assert.That(bag.State, Is.EqualTo(TeaBagState.PickedUp));
            Assert.That(bag.BoundItem.OrderId, Is.EqualTo(order));
            Assert.That(_rack.Stock, Is.EqualTo(1));
            Assert.That(_orders.Get(order).Status, Is.EqualTo(OrderStatus.InPreparation));

            // Steps 3-8, with Ready refused and the bag kept in hand at every unfinished state.
            var steps = new List<TeaBagState>();
            using var stepEvents = _events.Subscribe<DrinkStepCompleted>(e => steps.Add(e.State));
            AssertUnfinishedStaysInHand(bag);
            bag.ExecuteUse(_context);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Opened));
            AssertUnfinishedStaysInHand(bag);
            _coconut.Execute(_context);
            AssertUnfinishedStaysInHand(bag);
            _lemon.Execute(_context);
            AssertUnfinishedStaysInHand(bag);
            _ice.Execute(_context);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.IceAdded));
            AssertUnfinishedStaysInHand(bag);
            bag.ExecuteUse(_context);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Shaken));
            AssertUnfinishedStaysInHand(bag);
            _wipe.Execute(_context);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Wiped));
            Assert.That(steps, Is.EqualTo(new[] { TeaBagState.Opened, TeaBagState.CoconutJellyAdded,
                TeaBagState.LemonJellyAdded, TeaBagState.IceAdded, TeaBagState.Shaken, TeaBagState.Wiped }));

            // Step 9: Ready placement releases the hand exactly once and parks the same bag on the counter.
            Assert.That(_counter.Query(_context).Availability.IsAvailable, Is.True);
            released.Clear();
            _counter.Execute(_context);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(released.Count, Is.EqualTo(1));
            Assert.That(released[0].Previous, Is.SameAs(bag));
            Assert.That(released[0].Current, Is.Null);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Ready));
            Assert.That(bag.transform.parent, Is.SameAs(_drinkPlacement));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.True);
            Assert.That(_orders.Get(order).Status, Is.EqualTo(OrderStatus.Ready));

            // The placed bag cannot come back into the stall's hands or be worked again.
            AssertPlacedBagCannotBeRepicked(bag);

            // Step 10: Lobby handoff carries the same bag, then delivery retires it.
            _pickup.Execute(_context);
            var bundle = _hands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            Assert.That(bundle.Items.Count, Is.EqualTo(1));
            Assert.That(bundle.Items[0], Is.SameAs(bag));
            Assert.That(_drinkPlacement.childCount, Is.Zero);
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            Assert.That(_orders.Get(order).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            OrderOrigin origin = OrderOrigin.ForTable(new TableId(1));
            Assert.That(_orders.Deliver(order, new ActorRef(1), new DeliveryTarget(origin, new CustomerId(1))).IsSuccess, Is.True);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(bag == null, Is.True, "Delivered drink leaves the world with its carried bundle.");
            Assert.That(_rack.Stock, Is.EqualTo(1));
        }

        [Test]
        public void MAIN_002_StationsHideCompletedStepsInsteadOfTeachingAPassedStep()
        {
            SendDrinkOrder(1);
            Assert.That(TakeBag().IsSuccess, Is.True);
            var bag = (TeaBagItem)_hands.Current;
            var stations = new IInteractable[] { _coconut, _lemon, _ice, _wipe };
            var expected = new[] { TeaBagState.Opened, TeaBagState.CoconutJellyAdded, TeaBagState.LemonJellyAdded, TeaBagState.Shaken };
            var reasons = new[] { "drink.need_open", "drink.need_coconut_first", "drink.need_lemon_first", "drink.need_shake_first" };
            Action[] advance =
            {
                () => bag.ExecuteUse(_context), () => _coconut.Execute(_context), () => _lemon.Execute(_context),
                () => _ice.Execute(_context), () => bag.ExecuteUse(_context), () => _wipe.Execute(_context),
            };
            for (int step = 0; step <= advance.Length; step++)
            {
                for (int s = 0; s < stations.Length; s++)
                {
                    InteractionQuery query = stations[s].Query(_context);
                    string where = stations[s].GetType().Name + " at " + bag.State;
                    if (bag.State < expected[s])
                    {
                        Assert.That(query.Availability.Status, Is.EqualTo(AvailabilityStatus.Blocked), where);
                        Assert.That(query.BlockedReasonKey, Is.EqualTo(reasons[s]), where);
                    }
                    else if (bag.State == expected[s])
                    {
                        Assert.That(query.Availability.IsAvailable, Is.True, where);
                    }
                    else
                    {
                        Assert.That(query.Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden), where);
                        TeaBagState before = bag.State;
                        int blocked = 0;
                        using (_events.Subscribe<DrinkStepCompleted>(_ => blocked++))
                        {
                            stations[s].Execute(_context);
                        }
                        Assert.That(bag.State, Is.EqualTo(before), where);
                        Assert.That(blocked, Is.Zero, where);
                    }
                }
                if (step < advance.Length)
                {
                    advance[step]();
                }
            }
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Wiped));
            Assert.That(bag.QueryUse(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
        }

        [Test]
        public void MAIN_002_ShakeAndWipeCannotRunOutOfOrderThroughTheHoldDriver()
        {
            SendDrinkOrder(1);
            Assert.That(TakeBag().IsSuccess, Is.True);
            var bag = (TeaBagItem)_hands.Current;
            var driver = new InteractionActionDriver(_context);
            bag.ExecuteUse(_context);
            Action[] toppings = { () => _coconut.Execute(_context), () => _lemon.Execute(_context), () => _ice.Execute(_context) };
            for (int i = 0; i < toppings.Length; i++)
            {
                TeaBagState before = bag.State;
                driver.BeginHeld();
                driver.Begin(_wipe);
                Assert.That(driver.IsRunning, Is.False, "No hold may start before ice (shake) or shake (wipe).");
                _clock.Advance(10f);
                driver.Tick(_wipe);
                Assert.That(bag.State, Is.EqualTo(before));
                toppings[i]();
            }
            Assert.That(bag.State, Is.EqualTo(TeaBagState.IceAdded));
            driver.Begin(_wipe);
            Assert.That(driver.IsRunning, Is.False, "Wipe cannot start before shake.");
            driver.BeginHeld();
            _clock.Advance(_recipe.ShakeHoldSeconds);
            driver.Tick(null);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Shaken));
            driver.BeginHeld();
            Assert.That(driver.IsRunning, Is.False, "A shaken bag offers no second shake.");
            driver.Begin(_wipe);
            _clock.Advance(_recipe.WipeHoldSeconds);
            driver.Tick(_wipe);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Wiped));
            driver.Begin(_wipe);
            Assert.That(driver.IsRunning, Is.False, "A wiped bag offers no second wipe.");
        }

        private void AssertUnfinishedStaysInHand(TeaBagItem bag)
        {
            TeaBagState state = bag.State;
            Assert.That(_counter.Query(_context).BlockedReasonKey, Is.EqualTo("ready.not_finished"), state.ToString());
            _counter.Execute(_context);
            Assert.That(_hands.Current, Is.SameAs(bag), state.ToString());
            Assert.That(bag.State, Is.EqualTo(state));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            Assert.That(_drinkPlacement.childCount, Is.Zero);
        }

        private void AssertPlacedBagCannotBeRepicked(TeaBagItem bag)
        {
            Assert.That(_counter.Query(_context).BlockedReasonKey, Is.EqualTo("ready.need_prepared_item"));
            Assert.That(TakeBag().ReasonKey, Is.EqualTo("stall.no_ticket.drink"));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_rack.Stock, Is.EqualTo(1));
            Assert.That(bag.QueryUse(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            Assert.That(bag.Query(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            foreach (IInteractable station in new IInteractable[] { _coconut, _lemon, _ice, _wipe })
            {
                Assert.That(station.Query(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            }
            Assert.That(bag.MarkReady().ReasonKey, Is.EqualTo("ready.already_ready"));
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Ready));
            Assert.That(bag.transform.parent, Is.SameAs(_drinkPlacement));
        }

        private Result TakeBag()
        {
            return _rack.TryTakeBag(_hands.Current == null, _queue, index => _hands.TryPickUp(_rackBags[index]));
        }

        private OrderId SendDrinkOrder(int table)
        {
            var requests = new List<ItemRequest> { new ItemRequest("drink", 1) };
            OrderOrigin origin = OrderOrigin.ForTable(new TableId(table));
            OrderId id = _orders.RequestService(origin, new CustomerId(table), requests).Value;
            Assert.That(_orders.BeginTaking(id, new ActorRef(1), origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, requests).IsSuccess, Is.True);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            return id;
        }

        private TeaBagItem CreateBag(DrinkPreparation preparation)
        {
            var root = new GameObject("TeaBag");
            _objects.Add(root);
            root.AddComponent<BoxCollider>();
            var anchors = new GameObject("Anchors");
            anchors.transform.SetParent(root.transform, false);
            var grip = new GameObject("HandGrip");
            grip.transform.SetParent(anchors.transform, false);
            var placement = new GameObject("PlacementPoint");
            placement.transform.SetParent(anchors.transform, false);
            var view = root.AddComponent<TeaBagStateView>();
            var item = root.AddComponent<TeaBagItem>();
            Set(item, "_handGrip", grip.transform);
            Set(item, "_placementPoint", placement.transform);
            Set(item, "_stateView", view);
            item.Initialize(preparation, _recipe, _events);
            return item;
        }

        private T Station<T>(int id) where T : MonoBehaviour
        {
            var root = new GameObject(typeof(T).Name);
            _objects.Add(root);
            var station = root.AddComponent<T>();
            Set(station, "_id", id);
            return station;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
