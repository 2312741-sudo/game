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
    /// MAIN-102 fixture: real OrderService, ticket queue, Ready shelf and a scene-shaped tea rack with a configurable
    /// capacity. Shared by the drink reliability tests (orphan discard and rack restock).
    /// </summary>
    internal sealed class DrinkReliabilityKit
    {
        private readonly List<Object> _objects = new List<Object>();
        public EventBus Events { get; }
        public ManualClock Clock { get; }
        public OrderService Orders { get; }
        public StallTicketQueue Queue { get; }
        public ReadyShelf Shelf { get; }
        public HeldItemSlot Hands { get; }
        public InteractionContext Context { get; }
        public int OrderEvents { get; private set; }
        public List<string> Blocked { get; } = new List<string>();
        private readonly List<System.IDisposable> _subscriptions = new List<System.IDisposable>();

        public DrinkReliabilityKit()
        {
            Events = new EventBus();
            Clock = new ManualClock();
            Hands = new HeldItemSlot(Events);
            var content = new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake),
            });
            Orders = new OrderService(Clock, Events, new SequentialIdGenerator(), content, 1, 1);
            Queue = new StallTicketQueue(Orders);
            Shelf = new ReadyShelf(Orders, Queue, 1, 1);
            Context = new InteractionContext(new ActorRef(1), ActorRole.Stall | ActorRole.Lobby, Hands, Clock, Events);
            _subscriptions.Add(Events.Subscribe<OrderStatusChanged>(_ => OrderEvents++));
            _subscriptions.Add(Events.Subscribe<OrderItemStatusChanged>(_ => OrderEvents++));
            _subscriptions.Add(Events.Subscribe<ActionBlocked>(blocked => Blocked.Add(blocked.ReasonKey)));
        }

        public void Dispose()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null) { Object.DestroyImmediate(_objects[i]); }
            }
            _objects.Clear();
            foreach (System.IDisposable subscription in _subscriptions) { subscription.Dispose(); }
            Assert.That(Orders.ObserverFaults.Count, Is.Zero);
            Shelf.Dispose();
            Events.Dispose();
        }

        public void Track(Object item) => _objects.Add(item);

        public OrderId SendOrder(int table, params string[] items)
        {
            var requests = new List<ItemRequest>();
            foreach (string item in items) { requests.Add(new ItemRequest(item, 1)); }
            OrderOrigin origin = OrderOrigin.ForTable(new TableId(table));
            OrderId id = Orders.RequestService(origin, new CustomerId(table), requests).Value;
            Assert.That(Orders.BeginTaking(id, new ActorRef(1), origin).IsSuccess, Is.True);
            Assert.That(Orders.Enter(id, requests).IsSuccess, Is.True);
            Assert.That(Orders.SendToStall(id).IsSuccess, Is.True);
            return id;
        }

        public TeaBagItem TakeBag(TeaRackController rack)
        {
            Assert.That(rack.Query(Context).Availability.IsAvailable, Is.True, rack.Query(Context).BlockedReasonKey);
            rack.Execute(Context);
            var bag = Hands.Current as TeaBagItem;
            Assert.That(bag, Is.Not.Null);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.PickedUp));
            return bag;
        }

        public static void Finish(TeaBagItem bag)
        {
            DrinkPreparation preparation = bag.Preparation;
            Assert.That(preparation.Open().IsSuccess && preparation.AddCoconutJelly().IsSuccess
                && preparation.AddLemonJelly().IsSuccess && preparation.AddIce().IsSuccess
                && preparation.Shake().IsSuccess && preparation.Wipe().IsSuccess, Is.True);
        }

        /// <summary>Full single-drink loop: finish, Ready, whole-order pickup, delivery and completion.</summary>
        public void ServeHeldDrink(OrderId order, int table)
        {
            var bag = (TeaBagItem)Hands.Current;
            Finish(bag);
            Assert.That(Shelf.PlaceReady(bag).IsSuccess, Is.True);
            Assert.That(Hands.TryRelease(), Is.True);
            Assert.That(Shelf.PickUp(order, new ActorRef(2)).IsSuccess, Is.True);
            OrderOrigin origin = OrderOrigin.ForTable(new TableId(table));
            Assert.That(Orders.Deliver(order, new ActorRef(2), new DeliveryTarget(origin, new CustomerId(table))).IsSuccess, Is.True);
            Assert.That(Orders.Complete(order).IsSuccess, Is.True);
            // The served bundle owns the visual and destroys it with itself.
            Object.DestroyImmediate(bag.gameObject);
        }

        public TeaRackController CreateRack(int capacity, int initialStock, bool gated = true)
        {
            var balance = ScriptableObject.CreateInstance<BalanceConfig>();
            Set(balance, "_teaRackCapacity", capacity);
            Set(balance, "_teaRackInitialStock", initialStock);
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            var recipe = ScriptableObject.CreateInstance<DrinkRecipe>();
            Set(recipe, "_itemDefinition", definition);
            Set(recipe, "_shakeHoldSeconds", 1.75f);
            Set(recipe, "_wipeHoldSeconds", 0.8f);
            Track(balance);
            Track(definition);
            Track(recipe);

            var prefab = new GameObject("TeaBagTemplate");
            Track(prefab);
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
            Track(root);
            var slots = new Transform[capacity];
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
            if (gated) { rack.Initialize(Queue, new SequentialIdGenerator(), Events, Shelf); }
            else { rack.Initialize(Queue, new SequentialIdGenerator(), Events); }
            return rack;
        }

        public static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
