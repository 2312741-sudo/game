using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    /// <summary>
    /// MAIN-102 scenarios 2, 6 and 7: with capacity-1 Ready slots per kind, the stall Ready gates (rack / cake Fill refuse
    /// while their kind's slot is occupied) and FIFO ticket claims, no sequence of player actions over several drink, cake
    /// and mixed orders can reach a state where an order is unfinished and no action is possible.
    /// Exhaustive search over the real OrderService / StallTicketQueue / ReadyShelf, replaying each path from scratch.
    /// The single player hand slot is modelled: a held item blocks order entry, cake hand steps and Ready pickup.
    /// </summary>
    public sealed class ReadySlotDeadlockModelTests
    {
        private enum Act { Send, ClaimDrink, PlaceDrink, ClaimCake, PourCake, WrapCake, PlaceCake, PickUpAndDeliver }

        private static IEnumerable<string[]> Scenarios()
        {
            string[] kinds = { "D", "C", "DC" };
            foreach (string a in kinds)
            {
                foreach (string b in kinds)
                {
                    foreach (string c in kinds) { yield return new[] { a, b, c }; }
                }
            }
        }

        [TestCaseSource(nameof(Scenarios))]
        public void MAIN_102_NoReachableDeadlockForThreeOrdersOfAnyMix(string first, string second, string third)
        {
            string[] orders = { first, second, third };
            var visited = new HashSet<string>();
            var stack = new Stack<List<Act>>();
            stack.Push(new List<Act>());
            int states = 0;
            while (stack.Count > 0)
            {
                List<Act> path = stack.Pop();
                using (var world = new World(orders))
                {
                    foreach (Act act in path) { Assert.That(world.Apply(act), Is.True, "replay " + act); }
                    if (!visited.Add(world.Signature())) { continue; }
                    states++;
                    List<Act> enabled = world.Enabled();
                    if (world.AllCompleted) { continue; }
                    Assert.That(enabled, Is.Not.Empty, "Deadlock after: " + string.Join(",", path) + " state " + world.Signature());
                    foreach (Act act in enabled) { stack.Push(new List<Act>(path) { act }); }
                }
            }
            Assert.That(states, Is.GreaterThan(1));
        }

        /// <summary>Without the Ready gates the same search finds the soft-lock that MAIN-002 closed (sanity check of the model).</summary>
        [Test]
        public void MAIN_102_ModelFindsTheSoftLockWhenReadyGatesAreRemoved()
        {
            string[] orders = { "DC", "D", "C" };
            var stack = new Stack<List<Act>>();
            stack.Push(new List<Act>());
            var visited = new HashSet<string>();
            bool deadlock = false;
            while (stack.Count > 0 && !deadlock)
            {
                List<Act> path = stack.Pop();
                using (var world = new World(orders) { Gated = false })
                {
                    foreach (Act act in path) { world.Apply(act); }
                    if (!visited.Add(world.Signature())) { continue; }
                    List<Act> enabled = world.Enabled();
                    if (!world.AllCompleted && enabled.Count == 0) { deadlock = true; }
                    foreach (Act act in enabled) { stack.Push(new List<Act>(path) { act }); }
                }
            }
            Assert.That(deadlock, Is.True);
        }

        private sealed class World : IDisposable
        {
            private readonly string[] _kinds;
            private readonly EventBus _events = new EventBus();
            private readonly ManualClock _clock = new ManualClock();
            private readonly OrderService _orders;
            private readonly StallTicketQueue _queue;
            private readonly ReadyShelf _shelf;
            private readonly List<OrderId> _sent = new List<OrderId>();
            private Item _drink;
            private Item _cake;
            private bool _cakeOnStation;
            private int _nextPreparation = 1000;
            public bool Gated { get; set; } = true;

            public World(string[] kinds)
            {
                _kinds = kinds;
                var content = new ContentDatabase(new[]
                {
                    new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink),
                    new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake),
                });
                _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), content, 1, 1);
                _queue = new StallTicketQueue(_orders);
                _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            }

            // Drink in hand from rack pickup to Ready; cake: cup in hand until pour, on station, then wrapped in hand.
            private bool HandsEmpty => _drink == null && (_cake == null || _cakeOnStation);

            public bool AllCompleted
            {
                get
                {
                    if (_sent.Count < _kinds.Length) { return false; }
                    foreach (OrderId id in _sent) { if (_orders.Get(id).Status != OrderStatus.Completed) { return false; } }
                    return true;
                }
            }

            public List<Act> Enabled()
            {
                var acts = new List<Act>();
                foreach (Act act in (Act[])Enum.GetValues(typeof(Act))) { if (CanApply(act)) { acts.Add(act); } }
                return acts;
            }

            private bool CanApply(Act act)
            {
                switch (act)
                {
                    case Act.Send: return _sent.Count < _kinds.Length && HandsEmpty;
                    case Act.ClaimDrink: return HandsEmpty && _drink == null && _queue.HasPending(ItemKind.Drink) && !(Gated && _shelf.Occupied(ItemKind.Drink));
                    case Act.PlaceDrink: return _drink != null && _shelf.CanPlace(_drink).IsAvailable;
                    case Act.ClaimCake: return HandsEmpty && _cake == null && _queue.HasPending(ItemKind.Cake) && !(Gated && _shelf.Occupied(ItemKind.Cake));
                    case Act.PourCake: return _cake != null && !_cakeOnStation && !_cake.Wrapped;
                    case Act.WrapCake: return _cake != null && _cakeOnStation && _drink == null;
                    case Act.PlaceCake: return _cake != null && _cake.Wrapped && _shelf.CanPlace(_cake).IsAvailable;
                    default: return HandsEmpty && _shelf.NextReadyOrder.IsValid;
                }
            }

            public bool Apply(Act act)
            {
                if (!CanApply(act)) { return false; }
                switch (act)
                {
                    case Act.Send:
                        int index = _sent.Count;
                        var requests = new List<ItemRequest>();
                        if (_kinds[index].Contains("D")) { requests.Add(new ItemRequest("drink", 1)); }
                        if (_kinds[index].Contains("C")) { requests.Add(new ItemRequest("cake", 1)); }
                        OrderOrigin origin = OrderOrigin.ForTable(new TableId(index + 1));
                        OrderId id = _orders.RequestService(origin, new CustomerId(index + 1), requests).Value;
                        _orders.BeginTaking(id, new ActorRef(1), origin);
                        _orders.Enter(id, requests);
                        _orders.SendToStall(id);
                        _clock.Advance(1d);
                        _sent.Add(id);
                        return true;
                    case Act.ClaimDrink: _drink = Claim(ItemKind.Drink); return true;
                    case Act.PlaceDrink: Assert.That(_shelf.PlaceReady(_drink).IsSuccess, Is.True); _drink = null; return true;
                    case Act.ClaimCake: _cake = Claim(ItemKind.Cake); _cakeOnStation = false; return true;
                    case Act.PourCake: _cakeOnStation = true; return true;
                    case Act.WrapCake: _cake.Wrapped = true; _cakeOnStation = false; return true;
                    case Act.PlaceCake: Assert.That(_shelf.PlaceReady(_cake).IsSuccess, Is.True); _cake = null; return true;
                    default:
                        OrderId ready = _shelf.NextReadyOrder;
                        Assert.That(_shelf.PickUp(ready, new ActorRef(2)).IsSuccess, Is.True);
                        IReadOnlyOrder order = _orders.Get(ready);
                        Assert.That(_orders.Deliver(ready, new ActorRef(2), new DeliveryTarget(order.Origin, order.CustomerId)).IsSuccess, Is.True);
                        Assert.That(_orders.Complete(ready).IsSuccess, Is.True);
                        return true;
                }
            }

            private Item Claim(ItemKind kind)
            {
                Result<OrderItemRef> claim = _queue.ClaimNext(kind, new PreparationId(_nextPreparation++));
                Assert.That(claim.IsSuccess, Is.True);
                return new Item(kind, claim.Value);
            }

            public string Signature()
            {
                var text = new StringBuilder();
                foreach (OrderId id in _sent)
                {
                    IReadOnlyOrder order = _orders.Get(id);
                    text.Append(order.Status).Append('[');
                    foreach (IReadOnlyOrderItem item in order.Items) { text.Append(item.Status).Append(' '); }
                    text.Append(']');
                }
                text.Append("|d:").Append(_drink?.BoundItem.OrderId.Value ?? 0);
                text.Append("|c:").Append(_cake?.BoundItem.OrderId.Value ?? 0).Append(_cakeOnStation ? "s" : "").Append(_cake != null && _cake.Wrapped ? "w" : "");
                return text.ToString();
            }

            public void Dispose()
            {
                Assert.That(_orders.ObserverFaults, Is.Empty);
                _shelf.Dispose();
                _events.Dispose();
            }
        }

        private sealed class Item : IPreparedItem
        {
            private bool _ready;
            public Item(ItemKind kind, OrderItemRef binding) { Kind = kind; BoundItem = binding; }
            public ItemKind Kind { get; }
            public OrderItemRef BoundItem { get; }
            public bool Wrapped { get; set; }
            public bool IsFinished => Kind == ItemKind.Drink || Wrapped;
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
