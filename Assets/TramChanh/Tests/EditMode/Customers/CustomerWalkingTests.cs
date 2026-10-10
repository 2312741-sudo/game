using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Customers;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Tests.EditMode.Lobby;
using TramChanh.UI.Orders;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Customers
{
    /// <summary>PEOPLE-001: opt-in walking mode (reserve, walk, order on arrival, eat, leave, depart) against scriptable seats.</summary>
    public sealed class CustomerWalkingTests
    {
        private const string Drink = "drink.test";
        private const string Cake = "cake.test";
        private readonly List<string> _log = new List<string>();
        private readonly List<CustomerArrivingEvent> _arriving = new List<CustomerArrivingEvent>();
        private readonly List<CustomerSeatedEvent> _seated = new List<CustomerSeatedEvent>();
        private readonly List<Exception> _faults = new List<Exception>();

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _arriving.Clear();
            _seated.Clear();
            _faults.Clear();
        }

        private static CustomerDirectorSettings Walking(int max = 10, float interval = 1000f, float eat = 4f, float arrivalTimeout = 30f, float departureTimeout = 20f) =>
            new CustomerDirectorSettings(max, interval, 2f, 1f, 1f, 1f, true, eat, arrivalTimeout, departureTimeout);

        private CustomerDirector Director(IReadOnlyList<ICustomerSeat> seats, CustomerDirectorSettings settings, int firstId = 100)
        {
            var director = new CustomerDirector(seats, settings, Drink, Cake, 7, firstId, _faults.Add);
            director.CustomerArriving += e => { _arriving.Add(e); _log.Add("arriving:" + e.SeatIndex); };
            director.CustomerSeated += e => { _seated.Add(e); _log.Add("seated:" + e.SeatIndex); };
            director.CustomerRequestFailed += e => _log.Add("failed:" + e.SeatIndex + ":" + e.ReasonKey);
            director.CustomerEating += i => _log.Add("eating:" + i);
            director.CustomerLeaving += i => _log.Add("leaving:" + i);
            director.CustomerLeft += i => _log.Add("left:" + i);
            return director;
        }

        private static string Snapshot(CustomerDirector director) =>
            director.ActiveCustomers + "|" + string.Join(",", Enumerable.Range(0, director.SeatCount)
                .Select(i => director.PhaseAt(i) + ":" + director.CustomerAt(i).Value + ":" + director.IsOccupied(i)));

        [Test]
        public void Settings_WalkingDefaults_AreFieldInitializers_AndExistingConstructorCallsKeepLegacyMode()
        {
            var created = (CustomerDirectorSettings)Activator.CreateInstance(typeof(CustomerDirectorSettings));
            Assert.That(created.CustomersWalk, Is.False);
            Assert.That(created.EatSeconds, Is.EqualTo(4f));
            Assert.That(created.ArrivalTimeoutSeconds, Is.EqualTo(30f));
            Assert.That(created.DepartureTimeoutSeconds, Is.EqualTo(20f));
            var legacy = new CustomerDirectorSettings(3, 6f, 2f, 1f, 1f, 1f);
            Assert.That(legacy.CustomersWalk, Is.False);
            Assert.That(legacy.EatSeconds, Is.EqualTo(4f));
            var bad = new CustomerDirectorSettings(1, 1f, 1f, 1f, 1f, 1f, true, -1f, float.NaN, -5f);
            Assert.That(bad.CustomersWalk, Is.True);
            Assert.That(new[] { bad.EatSeconds, bad.ArrivalTimeoutSeconds, bad.DepartureTimeoutSeconds }, Is.All.EqualTo(0f));
            foreach (string name in new[] { "_customersWalk", "_eatSeconds", "_arrivalTimeoutSeconds", "_departureTimeoutSeconds" })
            {
                FieldInfo field = typeof(CustomerDirectorSettings).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, name);
                Assert.That(field.GetCustomAttributes(typeof(SerializeFieldAttribute), false), Is.Not.Empty, name);
                var tbd = (TramChanh.Core.Provisional.TbdAttribute)Attribute.GetCustomAttribute(field, typeof(TramChanh.Core.Provisional.TbdAttribute));
                Assert.That(tbd?.DecisionId, Is.EqualTo("DEC-010"), name);
            }
        }

        [Test]
        public void LegacyMode_SeatsAtSpawn_ReportsSeatedPhase_AndNeverRaisesWalkingEvents()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(1);
            CustomerDirector director = Director(new[] { seat }, new CustomerDirectorSettings(1, 100f, 1f));
            Assert.That(director.CustomersWalk, Is.False);
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(seat.Requests.Count, Is.EqualTo(1), "Legacy requests at spawn.");
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated));
            director.NotifyArrived(0);
            Assert.That(seat.Requests.Count, Is.EqualTo(1), "NotifyArrived is ignored in legacy mode.");
            seat.HasLiveOrder = false;
            director.Tick(0.5d);
            director.NotifyDeparted(0);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated), "Clear delay; NotifyDeparted is ignored in legacy mode.");
            director.Tick(1d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Free));
            Assert.That(_log, Is.EqualTo(new[] { "seated:0", "left:0" }));
            Assert.That(director.PhaseAt(-1), Is.EqualTo(CustomerPhase.Free));
            Assert.That(director.PhaseAt(1), Is.EqualTo(CustomerPhase.Free));
        }

        [Test]
        public void Arrival_ReservesTheSeatWithoutARequest_AndRequestsOnNotifyArrived()
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables(3);
            CustomerDirector director = Director(seats, Walking());
            Assert.That(director.CustomersWalk, Is.True);
            int seat = director.SpawnNow();
            Assert.That(seat, Is.InRange(0, 2));
            CustomerArrivingEvent arriving = _arriving.Single();
            Assert.That(arriving.SeatIndex, Is.EqualTo(seat));
            Assert.That(arriving.Customer, Is.EqualTo(new CustomerId(100)));
            Assert.That(arriving.Items, Is.Not.Empty);
            Assert.That(seats.All(s => s.Requests.Count == 0), Is.True, "No order at spawn.");
            Assert.That(director.PhaseAt(seat), Is.EqualTo(CustomerPhase.Arriving));
            Assert.That(director.IsOccupied(seat), Is.True);
            Assert.That(director.CustomerAt(seat), Is.EqualTo(arriving.Customer));
            Assert.That(director.ActiveCustomers, Is.EqualTo(1));

            director.Tick(29d);
            Assert.That(seats[seat].Requests, Is.Empty, "Still walking.");
            director.NotifyArrived(seat);
            Assert.That(seats[seat].Requests.Single().Customer, Is.EqualTo(arriving.Customer));
            Assert.That(seats[seat].Requests.Single().Items, Is.EqualTo(arriving.Items), "The announced menu is ordered.");
            Assert.That(director.PhaseAt(seat), Is.EqualTo(CustomerPhase.Seated));
            Assert.That(_seated.Single().Order, Is.EqualTo(new OrderId(seats[seat].TableNumber * 1000 + 1)));
            Assert.That(_log, Is.EqualTo(new[] { "arriving:" + seat, "seated:" + seat }));
            director.Tick(5d);
            Assert.That(seats[seat].Requests.Count, Is.EqualTo(1), "The arrival timeout no longer applies.");
        }

        [Test]
        public void ArrivalTimeout_RequestsAnyway()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(5);
            CustomerDirector director = Director(new[] { seat }, Walking(arrivalTimeout: 30f));
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            director.Tick(29.5d);
            Assert.That(seat.Requests, Is.Empty);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Arriving));
            director.Tick(0.5d);
            Assert.That(seat.Requests.Count, Is.EqualTo(1));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated));
            director.NotifyArrived(0);
            Assert.That(seat.Requests.Count, Is.EqualTo(1), "A late presenter notify is ignored.");
        }

        [Test]
        public void RequestFailure_RaisesFailedThenLeaving_AndNotifyDepartedFreesTheSeat()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(2) { FailWith = "order.point.not_configured" };
            CustomerDirector director = Director(new[] { seat }, Walking());
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            director.NotifyArrived(0);
            Assert.That(seat.Requests.Count, Is.EqualTo(1));
            Assert.That(_log, Is.EqualTo(new[] { "arriving:0", "failed:0:order.point.not_configured", "leaving:0" }));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            Assert.That(director.IsOccupied(0), Is.True, "Seat stays reserved while the customer walks away.");
            Assert.That(director.ActiveCustomers, Is.EqualTo(1));
            seat.FailWith = null;
            Assert.That(director.SpawnNow(), Is.EqualTo(-1));

            director.NotifyDeparted(0);
            Assert.That(_log.Last(), Is.EqualTo("left:0"));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Free));
            Assert.That(director.IsOccupied(0), Is.False);
            Assert.That(director.ActiveCustomers, Is.EqualTo(0));
            Assert.That(_log, Has.No.Member("eating:0"), "No order was live, so nothing to eat.");
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.CustomerAt(0).Value, Is.EqualTo(101));
        }

        [Test]
        public void FullCycle_EatLeaveDepart_FreesTheSeat_AndANewCustomerWithANewIdReusesIt()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(4);
            CustomerDirector director = Director(new[] { seat }, Walking(1, eat: 4f));
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            director.NotifyArrived(0);
            director.Tick(10d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated), "Live order keeps the customer seated.");

            seat.HasLiveOrder = false;           // order completed
            director.Tick(1d);                   // detection; the eat timer starts now
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Eating));
            director.Tick(3.5d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Eating));
            director.Tick(0.5d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            Assert.That(director.IsOccupied(0), Is.True);
            Assert.That(director.SpawnNow(), Is.EqualTo(-1), "A leaving customer's seat is not reassigned.");
            director.NotifyDeparted(0);
            Assert.That(_log, Is.EqualTo(new[] { "arriving:0", "seated:0", "eating:0", "leaving:0", "left:0" }));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Free));
            Assert.That(director.ActiveCustomers, Is.EqualTo(0));

            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.CustomerAt(0), Is.EqualTo(new CustomerId(101)));
            Assert.That(seat.Requests.Count, Is.EqualTo(1), "The second customer has not ordered yet.");
            director.NotifyArrived(0);
            Assert.That(seat.Requests.Select(r => r.Customer.Value), Is.EqualTo(new[] { 100, 101 }));
        }

        [Test]
        public void ZeroEatSeconds_LeavesOnTheDetectionTick()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(1);
            CustomerDirector director = Director(new[] { seat }, Walking(1, eat: 0f));
            director.SpawnNow();
            director.NotifyArrived(0);
            seat.HasLiveOrder = false;
            director.Tick(0.1d);
            Assert.That(_log, Is.EqualTo(new[] { "arriving:0", "seated:0", "eating:0", "leaving:0" }));
        }

        [Test]
        public void DepartureTimeout_DepartsAnyway()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(1);
            CustomerDirector director = Director(new[] { seat }, Walking(1, eat: 1f, departureTimeout: 20f));
            director.SpawnNow();
            director.NotifyArrived(0);
            seat.HasLiveOrder = false;
            director.Tick(1d);
            director.Tick(1d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            director.Tick(19.5d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            Assert.That(_log, Has.No.Member("left:0"));
            director.Tick(0.5d);
            Assert.That(_log.Last(), Is.EqualTo("left:0"));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Free));
            director.NotifyDeparted(0);
            Assert.That(_log.Count(e => e == "left:0"), Is.EqualTo(1), "A late presenter notify is ignored.");
        }

        [Test]
        public void MaxActive_CountsWalkingCustomers()
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables();
            CustomerDirector director = Director(seats, Walking(2, interval: 1f, arrivalTimeout: 1000f));
            for (int i = 0; i < 20; i++) { director.Tick(1d); }
            Assert.That(director.ActiveCustomers, Is.EqualTo(2));
            Assert.That(_arriving.Count, Is.EqualTo(2));
            Assert.That(seats.All(s => s.Requests.Count == 0), Is.True, "Both are still walking.");
            Assert.That(director.SpawnNow(), Is.EqualTo(-1));

            // A leaving customer still counts.
            int first = _arriving[0].SeatIndex;
            seats[first].FailWith = "x";
            director.NotifyArrived(first);
            Assert.That(director.PhaseAt(first), Is.EqualTo(CustomerPhase.Leaving));
            director.Tick(5d);
            Assert.That(director.ActiveCustomers, Is.EqualTo(2));
            Assert.That(_arriving.Count, Is.EqualTo(2));
            director.NotifyDeparted(first);
            Assert.That(director.ActiveCustomers, Is.EqualTo(1));
            director.Tick(1d);
            Assert.That(_arriving.Count, Is.EqualTo(3), "The freed slot is used by the next timed arrival.");
        }

        [Test]
        public void NoReassignment_WhileArrivingOrLeaving()
        {
            // Three seats and a cap of 10, so -1 below comes from the reservation, not from MaxActive.
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables(3);
            seats[2].HasLiveOrder = true;        // busy with an order placed outside the director
            CustomerDirector director = Director(seats, Walking(10));
            Assert.That(director.MaxActiveCustomers, Is.EqualTo(3));
            int a = director.SpawnNow();
            int b = director.SpawnNow();
            Assert.That(new[] { a, b }, Is.EquivalentTo(new[] { 0, 1 }));
            Assert.That(!seats[0].HasLiveOrder && !seats[1].HasLiveOrder, Is.True, "Arriving seats have no live order yet.");
            Assert.That(director.SpawnNow(), Is.EqualTo(-1), "Both seats are reserved by walking customers.");

            seats[a].FailWith = "x";
            director.NotifyArrived(a);
            Assert.That(director.PhaseAt(a), Is.EqualTo(CustomerPhase.Leaving));
            Assert.That(director.SpawnNow(), Is.EqualTo(-1));
            for (int i = 0; i < 19; i++) { director.Tick(1d); }
            Assert.That(director.SpawnNow(), Is.EqualTo(-1));
            Assert.That(_arriving.Count, Is.EqualTo(2));
            Assert.That(_arriving.Select(e => e.Customer.Value), Is.Unique);
            Assert.That(director.ActiveCustomers, Is.EqualTo(2), "Below the cap, yet nothing was reassigned.");
        }

        [Test]
        public void WrongPhaseNotifies_AreIgnored_WithoutThrowingOrChangingState()
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables(4);
            CustomerDirector director = Director(seats, Walking(10, eat: 100f));
            // Seat phases: one Arriving, one Seated, one Eating, one Leaving.
            int arriving = director.SpawnNow();
            int seated = director.SpawnNow();
            int eating = director.SpawnNow();
            int leaving = director.SpawnNow();
            director.NotifyArrived(seated);
            director.NotifyArrived(eating);
            seats[leaving].FailWith = "x";
            director.NotifyArrived(leaving);
            seats[eating].HasLiveOrder = false;
            director.Tick(0.1d);
            Assert.That(new[] { director.PhaseAt(arriving), director.PhaseAt(seated), director.PhaseAt(eating), director.PhaseAt(leaving) },
                Is.EqualTo(new[] { CustomerPhase.Arriving, CustomerPhase.Seated, CustomerPhase.Eating, CustomerPhase.Leaving }));
            string before = Snapshot(director);
            int events = _log.Count;
            int requests = seats.Sum(s => s.Requests.Count);

            Assert.DoesNotThrow(() =>
            {
                foreach (int i in new[] { seated, eating, leaving, -1, 4, int.MaxValue, int.MinValue }) { director.NotifyArrived(i); }
                foreach (int i in new[] { arriving, seated, eating, -1, 4, int.MaxValue, int.MinValue }) { director.NotifyDeparted(i); }
            });
            Assert.That(Snapshot(director), Is.EqualTo(before));
            Assert.That(_log.Count, Is.EqualTo(events));
            Assert.That(seats.Sum(s => s.Requests.Count), Is.EqualTo(requests));

            // Free seat after a departure.
            director.NotifyDeparted(leaving);
            before = Snapshot(director);
            director.NotifyArrived(leaving);
            director.NotifyDeparted(leaving);
            Assert.That(Snapshot(director), Is.EqualTo(before));
        }

        [Test]
        public void Pause_HoldsArrivalEatAndDepartureTimers()
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables(3);
            CustomerDirector director = Director(seats, Walking(10, interval: 1000f, eat: 2f, arrivalTimeout: 2f, departureTimeout: 2f));
            int walking = director.SpawnNow();
            int dining = director.SpawnNow();
            int going = director.SpawnNow();
            director.NotifyArrived(dining);
            seats[going].FailWith = "x";
            director.NotifyArrived(going);       // Leaving, departure timer 2 s
            seats[dining].HasLiveOrder = false;
            director.Tick(0.01d);                // dining: eat timer 2 s starts
            director.Tick(1d);
            Assert.That(new[] { director.PhaseAt(walking), director.PhaseAt(dining), director.PhaseAt(going) },
                Is.EqualTo(new[] { CustomerPhase.Arriving, CustomerPhase.Eating, CustomerPhase.Leaving }));
            string before = Snapshot(director);
            int events = _log.Count;
            Paused(director);
            director.Tick(double.PositiveInfinity);
            Assert.That(Snapshot(director), Is.EqualTo(before), "Paused ticks change nothing.");
            Assert.That(_log.Count, Is.EqualTo(events));
            Assert.That(seats[walking].Requests, Is.Empty);

            director.Tick(1d);                   // every timer has 1 s or less left
            Assert.That(new[] { director.PhaseAt(walking), director.PhaseAt(dining), director.PhaseAt(going) },
                Is.EqualTo(new[] { CustomerPhase.Seated, CustomerPhase.Leaving, CustomerPhase.Free }));
        }

        [Test]
        public void Pause_HoldsEachTimerMidway()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(1);
            CustomerDirector director = Director(new[] { seat }, Walking(1, eat: 2f, arrivalTimeout: 2f, departureTimeout: 2f));
            director.SpawnNow();
            director.Tick(1.5d);
            Paused(director);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Arriving));
            director.Tick(0.5d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated));
            seat.HasLiveOrder = false;
            director.Tick(1d);
            director.Tick(1.5d);
            Paused(director);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Eating));
            director.Tick(0.5d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            director.Tick(1.5d);
            Paused(director);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            director.Tick(0.5d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Free));
            Assert.That(_log, Is.EqualTo(new[] { "arriving:0", "seated:0", "eating:0", "leaving:0", "left:0" }));
        }

        private static void Paused(CustomerDirector director)
        {
            for (int i = 0; i < 500; i++) { director.Tick(0d); }
            director.Tick(-1d);
            director.Tick(double.NaN);
        }

        [Test]
        public void ThrowingHandlers_DoNotBreakState_OtherHandlersStillRun_AndFaultsReachTheSink()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(1);
            CustomerDirector director = Director(new[] { seat }, Walking(1, eat: 1f));
            int after = 0;
            director.CustomerArriving += e => throw new InvalidOperationException("arriving fault");
            director.CustomerSeated += e => throw new InvalidOperationException("seated fault");
            director.CustomerEating += i => throw new InvalidOperationException("eating fault");
            director.CustomerLeaving += i => throw new InvalidOperationException("leaving fault");
            director.CustomerLeft += i => throw new InvalidOperationException("left fault");
            director.CustomerArriving += e => after++;
            director.CustomerLeft += i => after++;

            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Arriving));
            director.NotifyArrived(0);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated));
            seat.HasLiveOrder = false;
            director.Tick(1d);
            director.Tick(1d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            director.NotifyDeparted(0);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Free));
            Assert.That(director.ActiveCustomers, Is.EqualTo(0));
            Assert.That(after, Is.EqualTo(2), "Subscribers after a throwing one still run.");
            Assert.That(_faults.Select(f => f.Message), Is.EqualTo(new[] { "arriving fault", "seated fault", "eating fault", "leaving fault", "left fault" }));
            Assert.That(_log, Is.EqualTo(new[] { "arriving:0", "seated:0", "eating:0", "leaving:0", "left:0" }));
            Assert.That(director.SpawnNow(), Is.EqualTo(0), "The seat is reusable.");
        }

        [Test]
        public void ThrowingHandler_WithoutASink_IsLoggedThroughDebugLogException()
        {
            var director = new CustomerDirector(new[] { new FakeCustomerSeat(1) }, Walking(1), Drink, Cake, 1);
            director.CustomerArriving += e => throw new InvalidOperationException("unsinked fault");
            LogAssert.Expect(LogType.Exception, new Regex("unsinked fault"));
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Arriving));
            Assert.That(director.FaultSink, Is.Null);
        }

        [Test]
        public void ReentrantNotifies_FromHandlers_AreConsistent()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(1);
            CustomerDirector director = Director(new[] { seat }, Walking(1, eat: 0f));
            director.CustomerArriving += e => director.NotifyArrived(e.SeatIndex);    // presenter teleports
            director.CustomerLeaving += i => director.NotifyDeparted(i);               // presenter despawns immediately
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated));
            seat.HasLiveOrder = false;
            director.Tick(0.1d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Free));
            Assert.That(director.ActiveCustomers, Is.EqualTo(0));
            Assert.That(_log, Is.EqualTo(new[] { "arriving:0", "seated:0", "eating:0", "leaving:0", "left:0" }));
            Assert.That(_faults, Is.Empty);
        }
    }

    /// <summary>PEOPLE-001: walking mode over ten real <see cref="TableOrderPoint"/>s, OrderService and StallTicketQueue.</summary>
    public sealed class CustomerWalkingIntegrationTests
    {
        private GameObject _root;
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private LobbyOrderController _controller;
        private OrderEntryModel _entry;
        private ReadyOrderPickupPoint _pickup;
        private TableOrderPoint[] _tables;
        private OrderPointSeat[] _seats;
        private int _preparation;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Walking customer tables");
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Lobby | ActorRole.Stall, _hands, _clock, _events);
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            { new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink), new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake) }));
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _entry = new OrderEntryModel(_events);
            _controller = Child("Lobby").AddComponent<LobbyOrderController>();
            _controller.Initialize(_orders, _events);
            _pickup = Child("Ready pickup").AddComponent<ReadyOrderPickupPoint>();
            Set(_pickup, "_id", 90);
            Set(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf, _orders, _events);
            _tables = new TableOrderPoint[10];
            _seats = new OrderPointSeat[10];
            for (int i = 0; i < 10; i++)
            {
                _tables[i] = Child("TABLE_" + (i + 1).ToString("00")).AddComponent<TableOrderPoint>();
                _tables[i].Initialize(31 + i, _tables[i].transform, _orders, _controller, new TableId(i + 1));
                _seats[i] = new OrderPointSeat(_tables[i], i + 1);
            }
            _preparation = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(_orders.ObserverFaults, Is.Empty);
            if (_hands.Current is Component held && held != null) { Object.DestroyImmediate(held.gameObject); }
            Object.DestroyImmediate(_root);
            _entry.Dispose();
            _shelf.Dispose();
            _events.Dispose();
        }

        private static CustomerDirectorSettings Walking(int max, float drink = 1f, float cake = 1f, float mixed = 1f) =>
            new CustomerDirectorSettings(max, 1000f, 2f, drink, cake, mixed, true, 2f, 30f, 20f);

        [Test]
        public void OrderIsCreatedOnlyAfterArrival_AtWaitingForLobby_WithTheSeatTableAsOrigin()
        {
            var faults = new List<Exception>();
            var director = new CustomerDirector(_seats, Walking(10), "drink", "cake", 11, 100, faults.Add);
            var arriving = new List<CustomerArrivingEvent>();
            var seated = new List<CustomerSeatedEvent>();
            director.CustomerArriving += arriving.Add;
            director.CustomerSeated += seated.Add;

            for (int n = 0; n < 10; n++) { Assert.That(director.SpawnNow(), Is.Not.EqualTo(-1)); }
            Assert.That(director.SpawnNow(), Is.EqualTo(-1), "All ten tables reserved by walking customers.");
            Assert.That(_orders.Active, Is.Empty, "No order exists while customers walk.");
            Assert.That(_tables.All(t => !t.ActiveOrder.IsValid), Is.True);
            director.Tick(29d);
            Assert.That(_orders.Active, Is.Empty);

            foreach (CustomerArrivingEvent e in arriving.Take(5)) { director.NotifyArrived(e.SeatIndex); }
            Assert.That(seated.Count, Is.EqualTo(5));
            Assert.That(_orders.Active.Count, Is.EqualTo(5));
            director.Tick(1d);                   // arrival timeout seats the other five
            Assert.That(seated.Count, Is.EqualTo(10));

            foreach (CustomerSeatedEvent e in seated)
            {
                IReadOnlyOrder order = _orders.Get(e.Order);
                Assert.That(order.Status, Is.EqualTo(OrderStatus.WaitingForLobby), "Arrivals enter Lobby intake, not the stall.");
                Assert.That(order.Origin, Is.EqualTo(OrderOrigin.ForTable(new TableId(_seats[e.SeatIndex].TableNumber))));
                Assert.That(order.Origin.TableId.Value, Is.EqualTo(e.SeatIndex + 1));
                Assert.That(order.CustomerId, Is.EqualTo(e.Customer));
                Assert.That(order.RequestedItems.Select(r => r.ItemDefinitionId),
                    Is.EqualTo(arriving.Single(a => a.SeatIndex == e.SeatIndex).Items.Select(r => r.ItemDefinitionId)));
                Assert.That(_tables[e.SeatIndex].ActiveOrder, Is.EqualTo(e.Order));
                Assert.That(director.PhaseAt(e.SeatIndex), Is.EqualTo(CustomerPhase.Seated));
            }
            Assert.That(_queue.Tickets, Is.Empty, "Nothing reaches the stall before the Lobby takes the order.");
            Assert.That(faults, Is.Empty);
        }

        [Test]
        public void ArrivalAtATableTakenMeanwhile_FailsThroughTheLobby_AndTheCustomerLeaves()
        {
            var director = new CustomerDirector(new[] { _seats[2] }, Walking(1), "drink", "cake", 3);
            var failed = new List<CustomerRequestFailedEvent>();
            var leaving = new List<int>();
            director.CustomerRequestFailed += failed.Add;
            director.CustomerLeaving += leaving.Add;
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            OrderId external = _tables[2].RequestCustomerService(new CustomerId(1), new[] { new ItemRequest("drink", 1) }).Value;
            director.NotifyArrived(0);
            Assert.That(failed.Single().SeatIndex, Is.EqualTo(0));
            Assert.That(failed.Single().ReasonKey, Is.Not.Null.And.Not.Empty);
            Assert.That(leaving, Is.EqualTo(new[] { 0 }));
            Assert.That(_orders.Active.Single().Id, Is.EqualTo(external), "The external order is untouched.");
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
        }

        [Test]
        public void CompletedOrder_Eats_Leaves_Departs_AndTheTableIsReusedByANewCustomer()
        {
            var director = new CustomerDirector(new[] { _seats[6] }, Walking(1, 0f, 0f, 1f), "drink", "cake", 3, 200);
            var seated = new List<CustomerSeatedEvent>();
            var log = new List<string>();
            director.CustomerSeated += e => { seated.Add(e); log.Add("seated"); };
            director.CustomerArriving += e => log.Add("arriving");
            director.CustomerEating += i => log.Add("eating");
            director.CustomerLeaving += i => log.Add("leaving");
            director.CustomerLeft += i => log.Add("left");

            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            director.NotifyArrived(0);
            OrderId order = seated[0].Order;
            Assert.That(_orders.Get(order).RequestedItems.Select(r => r.ItemDefinitionId), Is.EqualTo(new[] { "drink", "cake" }));

            TakeOrder(_tables[6], order);
            Assert.That(_shelf.PlaceReady(Prepare(ItemKind.Drink, order)).IsSuccess, Is.True);
            Assert.That(_shelf.PlaceReady(Prepare(ItemKind.Cake, order)).IsSuccess, Is.True);
            _pickup.Execute(_context);
            var bundle = _hands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            bundle.transform.SetParent(_root.transform, false);
            director.Tick(1d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Seated), "Order is still live while carried.");
            _tables[6].Execute(_context);
            Assert.That(_orders.Get(order).Status, Is.EqualTo(OrderStatus.Completed));

            director.Tick(1d);                   // detection: eating starts
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Eating));
            Assert.That(director.SpawnNow(), Is.EqualTo(-1), "An eating customer keeps the table although its point is free.");
            director.Tick(2d);
            Assert.That(director.PhaseAt(0), Is.EqualTo(CustomerPhase.Leaving));
            director.NotifyDeparted(0);
            Assert.That(log, Is.EqualTo(new[] { "arriving", "seated", "eating", "leaving", "left" }));

            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.CustomerAt(0).Value, Is.EqualTo(201));
            Assert.That(_orders.Active, Is.Empty, "The new customer has not ordered yet.");
            director.NotifyArrived(0);
            Assert.That(seated[1].Order, Is.Not.EqualTo(order));
            Assert.That(_orders.Get(seated[1].Order).Origin.TableId, Is.EqualTo(new TableId(7)));
            Assert.That(_orders.Get(seated[1].Order).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
        }

        private void TakeOrder(OrderPoint point, OrderId id)
        {
            Assert.That(point.Query(_context).PromptKey, Is.EqualTo(OrderPoint.TakeOrderPromptKey));
            point.Execute(_context);
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.SentToStall));
        }

        private DeliveryAdapterTests.PreparedBag Prepare(ItemKind kind, OrderId expected)
        {
            var item = Child("Prepared " + kind).AddComponent<DeliveryAdapterTests.PreparedBag>();
            item.Kind = kind;
            Result<OrderItemRef> claim = _queue.ClaimNext(kind, new PreparationId(++_preparation));
            Assert.That(claim.IsSuccess, Is.True, claim.ReasonKey);
            Assert.That(claim.Value.OrderId, Is.EqualTo(expected));
            item.Binding = claim.Value;
            return item;
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_root.transform, false);
            return child;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
