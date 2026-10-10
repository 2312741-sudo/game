#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.PlayMode.Cakes
{
    /// <summary>
    /// MAIN-102 scenario 9/12: the batter cup used to leave the hands only by pouring. Picked up with no cake ticket it
    /// blocked the hands forever (no order entry, no tea bag), so no cake ticket could ever arrive. An empty cup can now
    /// be put back on its stand with its held action (<c>cake.cup.return</c>).
    /// </summary>
    public sealed class CakeCupReturnTests
    {
        private GameObject _stationObject;
        private CakeStation _station;
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private InteractionContext _context;
        private CakeRecipe _recipe;

        [UnitySetUp] public IEnumerator SetUp()
        {
            _recipe = AssetDatabase.LoadAssetAtPath<CakeRecipe>("Assets/TramChanh/Data/ACCEL01/Cakes/SO_CakeRecipe_DEV_TBD.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_CakeStation.prefab");
            Assert.That(prefab, Is.Not.Null); Assert.That(_recipe, Is.Not.Null);
            _events = new EventBus(); _clock = new ManualClock(); var ids = new SequentialIdGenerator();
            _orders = new OrderService(_clock, _events, ids, new ContentDatabase(new[] { _recipe.ItemDefinition }));
            _queue = new StallTicketQueue(_orders); _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall, new HeldItemSlot(_events), _clock, _events);
            _stationObject = Object.Instantiate(prefab); _station = _stationObject.GetComponent<CakeStation>();
            _station.Initialize(_queue, ids, _events, _clock, new CakeRecipeCatalog(_queue, new[] { _recipe }), _shelf);
            yield return null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            Object.Destroy(_stationObject); yield return null; _shelf.Dispose(); _events.Dispose();
        }

        [UnityTest] public IEnumerator MAIN_102_CupPickedUpWithoutCakeTicketCanBePutBack()
        {
            Transform home = _station.Cup.transform.parent;
            _station.Cup.Execute(_context);
            Assert.That(_context.Hands.Current, Is.SameAs(_station.Cup));
            Assert.That(Point(CakeStationAction.Fill).Query(_context).BlockedReasonKey, Is.EqualTo("stall.no_ticket.cake"));

            InteractionQuery use = _station.Cup.QueryUse(_context);
            Assert.That(use.PromptKey, Is.EqualTo(BatterMeasureCup.ReturnPromptKey));
            Assert.That(use.Availability.IsAvailable, Is.True);
            new InteractionActionDriver(_context).BeginHeld();
            Assert.That(_context.Hands.Current, Is.Null, "The hands are free for order entry and the tea rack again.");
            Assert.That(_station.Cup.transform.parent, Is.SameAs(home));
            Assert.That(_station.Current, Is.Null);

            _station.Cup.Execute(_context);
            Assert.That(_context.Hands.Current, Is.SameAs(_station.Cup), "The returned cup can be picked up again.");
            yield return null;
        }

        [UnityTest] public IEnumerator MAIN_102_MeasuredCupEmptiesFirstThenReturns()
        {
            SendOrder(1);
            _station.Cup.Execute(_context);
            Assert.That(_station.Measure(_context, 2f).IsSuccess, Is.True);
            Assert.That(_station.Cup.QueryUse(_context).PromptKey, Is.EqualTo("cake.empty_back"));
            _station.Cup.ExecuteUse(_context);
            Assert.That(_station.Current, Is.Null);
            Assert.That(_context.Hands.Current, Is.SameAs(_station.Cup), "Emptying keeps the cup in hand (existing top-up flow).");
            Assert.That(_queue.HasPending(ItemKind.Cake), Is.True);

            Assert.That(_station.Cup.QueryUse(_context).PromptKey, Is.EqualTo(BatterMeasureCup.ReturnPromptKey));
            _station.Cup.ExecuteUse(_context);
            Assert.That(_context.Hands.Current, Is.Null);
            yield return null;
        }

        [UnityTest] public IEnumerator MAIN_102_CupHeldWhenTheOrderFailsCanBeReturned()
        {
            OrderId order = SendOrder(1);
            _station.Cup.Execute(_context);
            Assert.That(_station.Measure(_context, 2f).IsSuccess, Is.True);
            Assert.That(_orders.Fail(order, FailureReason.CustomerLeft).IsSuccess, Is.True);
            _station.Advance();
            Assert.That(_station.Current, Is.Null);
            _station.Cup.ExecuteUse(_context);
            Assert.That(_context.Hands.Current, Is.Null);
            yield return null;
        }

        [UnityTest] public IEnumerator MAIN_102_CupReturnIsRoleAndPauseGuarded()
        {
            _station.Cup.Execute(_context);
            _clock.Pause();
            Assert.That(_station.Cup.QueryUse(_context).BlockedReasonKey, Is.EqualTo("game.paused"));
            _station.Cup.ExecuteUse(_context);
            Assert.That(_context.Hands.Current, Is.SameAs(_station.Cup));
            _clock.Resume();
            var lobby = new InteractionContext(new ActorRef(2), ActorRole.Lobby, _context.Hands, _clock, _events);
            Assert.That(_station.Cup.QueryUse(lobby).BlockedReasonKey, Is.EqualTo("role.not_stall"));
            _station.Cup.ExecuteUse(_context);
            Assert.That(_context.Hands.Current, Is.Null);
            yield return null;
        }

        private OrderId SendOrder(int table)
        {
            var items = new[] { new ItemRequest(_recipe.ItemDefinition.Id, 1) }; var origin = OrderOrigin.ForTable(new TableId(table));
            OrderId order = _orders.RequestService(origin, new CustomerId(table), items).Value;
            Assert.That(_orders.BeginTaking(order, new ActorRef(1), origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(order, items).IsSuccess, Is.True); Assert.That(_orders.SendToStall(order).IsSuccess, Is.True);
            return order;
        }

        private CakeStationPoint Point(CakeStationAction action) => _station.GetComponentsInChildren<CakeStationPoint>().Single(point => point.Action == action);
    }
}
#endif
