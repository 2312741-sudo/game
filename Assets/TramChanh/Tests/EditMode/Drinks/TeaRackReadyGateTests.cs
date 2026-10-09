using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Drinks
{
    /// <summary>
    /// MAIN-002 follow-up: with a Ready-slot gate, the rack never hands out a bag (or claims a ticket) while the
    /// drink Ready slot is occupied, so a single player cannot soft-lock holding an unplaceable drink.
    /// </summary>
    public sealed class TeaRackReadyGateTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private int _actionsBlocked;
        private string _lastBlocked;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            var content = new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake),
            });
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), content, 1, 1);
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall | ActorRole.Lobby, _hands, _clock, _events);
            _actionsBlocked = 0;
            _lastBlocked = null;
        }

        [TearDown]
        public void TearDown()
        {
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
        public void MAIN_002_GatedRackIsBlockedWhileDrinkReadySlotIsOccupiedAndClaimsNoTicket()
        {
            TeaRackController rack = CreateRack(gated: true);
            OrderId first = SendOrder(1, "drink", "cake");
            OrderItemRef drinkA = PlaceFinishedDrink(rack);
            Assert.That(drinkA.OrderId, Is.EqualTo(first));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.True);
            Assert.That(_orders.Get(first).Status, Is.EqualTo(OrderStatus.InPreparation), "Order A still waits for its cake.");

            OrderId second = SendOrder(2, "drink");
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.True);
            int stock = rack.Stock;

            InteractionQuery query = rack.Query(_context);
            Assert.That(query.Availability.Status, Is.EqualTo(AvailabilityStatus.Blocked));
            Assert.That(query.BlockedReasonKey, Is.EqualTo("ready.slot_full"));

            using (_events.Subscribe<ActionBlocked>(OnBlocked))
            {
                rack.Execute(_context);
                rack.Execute(_context);
            }
            Assert.That(_actionsBlocked, Is.EqualTo(2));
            Assert.That(_lastBlocked, Is.EqualTo("ready.slot_full"));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(rack.Stock, Is.EqualTo(stock));
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.True, "No ticket may be claimed while the slot is occupied.");
            Assert.That(_orders.Get(second).Status, Is.EqualTo(OrderStatus.SentToStall));
            Assert.That(_orders.Get(second).Items[0].Status, Is.EqualTo(OrderItemStatus.Pending));
        }

        [Test]
        public void MAIN_002_GatedRackIsAvailableAgainAfterTheReadyOrderIsPickedUp()
        {
            TeaRackController rack = CreateRack(gated: true);
            OrderId first = SendOrder(1, "drink", "cake");
            PlaceFinishedDrink(rack);
            OrderId second = SendOrder(2, "drink");
            Assert.That(rack.Query(_context).BlockedReasonKey, Is.EqualTo("ready.slot_full"));

            PlaceFinishedCake();
            Assert.That(_orders.Get(first).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(rack.Query(_context).BlockedReasonKey, Is.EqualTo("ready.slot_full"), "Ready but not yet picked up.");
            Assert.That(_shelf.PickUp(first, new ActorRef(2)).IsSuccess, Is.True);
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);

            Assert.That(rack.Query(_context).Availability.IsAvailable, Is.True);
            int stock = rack.Stock;
            rack.Execute(_context);
            var bag = _hands.Current as TeaBagItem;
            Assert.That(bag, Is.Not.Null);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.PickedUp));
            Assert.That(bag.BoundItem.OrderId, Is.EqualTo(second));
            Assert.That(rack.Stock, Is.EqualTo(stock - 1));
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.False);
        }

        [Test]
        public void MAIN_002_UngatedThreeArgumentRackKeepsItsPreviousBehaviour()
        {
            TeaRackController rack = CreateRack(gated: false);
            SendOrder(1, "drink", "cake");
            PlaceFinishedDrink(rack);
            OrderId second = SendOrder(2, "drink");
            Assert.That(rack.Query(_context).Availability.IsAvailable, Is.True);
            rack.Execute(_context);
            Assert.That((_hands.Current as TeaBagItem)?.BoundItem.OrderId, Is.EqualTo(second));
        }

        [Test]
        public void MAIN_002_GateMustNotBeNullAndDoesNotChangeEarlierReasons()
        {
            TeaRackController unused = CreateRackObject();
            Assert.Throws<System.ArgumentNullException>(() => unused.Initialize(_queue, new SequentialIdGenerator(), _events, null));

            TeaRackController rack = CreateRack(gated: true);
            Assert.That(rack.Query(_context).BlockedReasonKey, Is.EqualTo("stall.no_ticket.drink"));
        }

        private void OnBlocked(ActionBlocked blocked)
        {
            _actionsBlocked++;
            _lastBlocked = blocked.ReasonKey;
        }

        private OrderItemRef PlaceFinishedDrink(TeaRackController rack)
        {
            rack.Execute(_context);
            var bag = (TeaBagItem)_hands.Current;
            DrinkPreparation preparation = bag.Preparation;
            Assert.That(preparation.Open().IsSuccess && preparation.AddCoconutJelly().IsSuccess
                && preparation.AddLemonJelly().IsSuccess && preparation.AddIce().IsSuccess
                && preparation.Shake().IsSuccess && preparation.Wipe().IsSuccess, Is.True);
            OrderItemRef binding = bag.BoundItem;
            Assert.That(_shelf.PlaceReady(bag).IsSuccess, Is.True);
            Assert.That(_hands.TryRelease(), Is.True);
            return binding;
        }

        private void PlaceFinishedCake()
        {
            var cake = new FinishedCake();
            cake.BoundItem = _queue.ClaimNext(ItemKind.Cake, new PreparationId(900)).Value;
            Assert.That(_shelf.PlaceReady(cake).IsSuccess, Is.True);
        }

        private OrderId SendOrder(int table, params string[] items)
        {
            var requests = new List<ItemRequest>();
            foreach (string item in items) { requests.Add(new ItemRequest(item, 1)); }
            OrderOrigin origin = OrderOrigin.ForTable(new TableId(table));
            OrderId id = _orders.RequestService(origin, new CustomerId(table), requests).Value;
            Assert.That(_orders.BeginTaking(id, new ActorRef(1), origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, requests).IsSuccess, Is.True);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            return id;
        }

        private TeaRackController CreateRack(bool gated)
        {
            TeaRackController rack = CreateRackObject();
            if (gated)
            {
                rack.Initialize(_queue, new SequentialIdGenerator(), _events, _shelf);
            }
            else
            {
                rack.Initialize(_queue, new SequentialIdGenerator(), _events);
            }
            return rack;
        }

        private TeaRackController CreateRackObject()
        {
            var balance = ScriptableObject.CreateInstance<BalanceConfig>();
            Set(balance, "_teaRackCapacity", 3);
            Set(balance, "_teaRackInitialStock", 3);
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            var recipe = ScriptableObject.CreateInstance<DrinkRecipe>();
            Set(recipe, "_itemDefinition", definition);
            Set(recipe, "_shakeHoldSeconds", 1.75f);
            Set(recipe, "_wipeHoldSeconds", 0.8f);
            _objects.Add(balance);
            _objects.Add(definition);
            _objects.Add(recipe);

            var prefab = new GameObject("TeaBagTemplate");
            _objects.Add(prefab);
            var anchors = new GameObject("Anchors");
            anchors.transform.SetParent(prefab.transform, false);
            var grip = new GameObject("HandGrip");
            grip.transform.SetParent(anchors.transform, false);
            var placement = new GameObject("PlacementPoint");
            placement.transform.SetParent(anchors.transform, false);
            var view = prefab.AddComponent<TeaBagStateView>();
            var template = prefab.AddComponent<TeaBagItem>();
            Set(template, "_handGrip", grip.transform);
            Set(template, "_placementPoint", placement.transform);
            Set(template, "_stateView", view);

            var root = new GameObject("TeaRack");
            _objects.Add(root);
            var slots = new Transform[3];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new GameObject("Slot" + i).transform;
                slots[i].SetParent(root.transform, false);
            }
            var rack = root.AddComponent<TeaRackController>();
            Set(rack, "_id", 21);
            Set(rack, "_interactionPoint", root.transform);
            Set(rack, "_balance", balance);
            Set(rack, "_recipe", recipe);
            Set(rack, "_bagPrefab", template);
            Set(rack, "_bagSlots", slots);
            return rack;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class FinishedCake : IPreparedItem
        {
            private bool _ready;
            public ItemKind Kind => ItemKind.Cake;
            public OrderItemRef BoundItem { get; set; }
            public bool IsFinished => true;
            public int Quality => 100;

            public Result MarkReady()
            {
                if (_ready) { return Result.Fail("ready.already_ready"); }
                _ready = true;
                return Result.Success();
            }
        }
    }
}
