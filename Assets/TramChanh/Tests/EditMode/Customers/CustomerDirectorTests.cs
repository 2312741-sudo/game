using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Customers;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Customers
{
    /// <summary>TABLES-01: arrival, occupancy and release logic against scriptable seats.</summary>
    public sealed class CustomerDirectorTests
    {
        private const string Drink = "drink.test";
        private const string Cake = "cake.test";
        private readonly List<CustomerSeatedEvent> _seated = new List<CustomerSeatedEvent>();
        private readonly List<int> _left = new List<int>();
        private readonly List<CustomerRequestFailedEvent> _failed = new List<CustomerRequestFailedEvent>();

        [SetUp]
        public void SetUp()
        {
            _seated.Clear();
            _left.Clear();
            _failed.Clear();
        }

        private CustomerDirector Director(IReadOnlyList<ICustomerSeat> seats, CustomerDirectorSettings settings = null, int seed = 7, int firstId = 100)
        {
            var director = new CustomerDirector(seats, settings ?? new CustomerDirectorSettings(), Drink, Cake, seed, firstId);
            director.CustomerSeated += _seated.Add;
            director.CustomerLeft += _left.Add;
            director.CustomerRequestFailed += _failed.Add;
            return director;
        }

        [Test]
        public void Settings_DefaultsMatchProvisionalValues()
        {
            var settings = new CustomerDirectorSettings();
            Assert.That(settings.MaxActiveCustomers, Is.EqualTo(3));
            Assert.That(settings.ArrivalIntervalSeconds, Is.EqualTo(6f));
            Assert.That(settings.TableClearSeconds, Is.EqualTo(2f));
            Assert.That(new[] { settings.DrinkWeight, settings.CakeWeight, settings.MixedWeight }, Is.EqualTo(new[] { 1f, 1f, 1f }));
            var bad = new CustomerDirectorSettings(-4, -1f, float.NaN, -1f, -2f, -3f);
            Assert.That(bad.MaxActiveCustomers, Is.EqualTo(0));
            Assert.That(bad.ArrivalIntervalSeconds, Is.EqualTo(0f));
            Assert.That(bad.TableClearSeconds, Is.EqualTo(0f));
            Assert.That(bad.DrinkWeight + bad.CakeWeight + bad.MixedWeight, Is.EqualTo(0f));
        }

        [Test]
        public void Settings_DefaultsAreFieldInitializers_SoADeserializedInstanceKeepsThem()
        {
            // The bootstrap serializes `new CustomerDirectorSettings()` with no scene values: defaults must come
            // from field initializers (run by the parameterless constructor Unity's serializer uses).
            foreach (var field in typeof(CustomerDirectorSettings).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            {
                Assert.That(field.GetCustomAttributes(typeof(UnityEngine.SerializeFieldAttribute), false), Is.Not.Empty, field.Name);
                var tbd = (TramChanh.Core.Provisional.TbdAttribute)Attribute.GetCustomAttribute(field, typeof(TramChanh.Core.Provisional.TbdAttribute));
                Assert.That(tbd?.DecisionId, Is.EqualTo("DEC-010"), field.Name);
            }
            var created = (CustomerDirectorSettings)Activator.CreateInstance(typeof(CustomerDirectorSettings));
            Assert.That(created.MaxActiveCustomers, Is.EqualTo(3));
            Assert.That(created.ArrivalIntervalSeconds, Is.EqualTo(6f));
            Assert.That(created.TableClearSeconds, Is.EqualTo(2f));
            Assert.That(new[] { created.DrinkWeight, created.CakeWeight, created.MixedWeight }, Is.EqualTo(new[] { 1f, 1f, 1f }));
        }

        [Test]
        public void SpawnNow_WorksBeforeTheFirstTick_AndSeatedFiresOnlyAfterASuccessfulRequest()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(3);
            CustomerDirector director = Director(new[] { seat });
            int requestsWhenSeated = -1;
            director.CustomerSeated += e => requestsWhenSeated = seat.Requests.Count;
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(requestsWhenSeated, Is.EqualTo(1), "Seated is raised after Request returned.");
            Assert.That(_seated.Single().Order, Is.EqualTo(new OrderId(3001)));
            Assert.That(_seated.Single().Customer, Is.EqualTo(seat.Requests[0].Customer));
        }

        [Test]
        public void Constructor_RejectsDuplicateTableNumbersAndNullSeats()
        {
            Assert.Throws<ArgumentException>(() => Director(new[] { new FakeCustomerSeat(1), new FakeCustomerSeat(1) }));
            Assert.Throws<ArgumentException>(() => Director(new ICustomerSeat[] { new FakeCustomerSeat(1), null }));
            Assert.Throws<ArgumentException>(() => new CustomerDirector(FakeCustomerSeat.Tables(), null, "", Cake, 1));
            Assert.That(Director(FakeCustomerSeat.Tables()).SeatCount, Is.EqualTo(10));
        }

        [Test]
        public void SpawnNow_AssignsOnlyFreeSeats_AndNeverAnOccupiedOne()
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables();
            CustomerDirector director = Director(seats, new CustomerDirectorSettings(10));
            var taken = new HashSet<int>();
            for (int i = 0; i < 10; i++)
            {
                int seat = director.SpawnNow();
                Assert.That(seat, Is.InRange(0, 9));
                Assert.That(taken.Add(seat), Is.True, "Seat " + seat + " was assigned twice.");
                Assert.That(director.IsOccupied(seat), Is.True);
                Assert.That(seats[seat].Requests.Count, Is.EqualTo(1));
            }
            Assert.That(director.SpawnNow(), Is.EqualTo(-1), "All tables occupied.");
            Assert.That(director.ActiveCustomers, Is.EqualTo(10));
            Assert.That(seats.All(s => s.Requests.Count == 1), Is.True);
        }

        [Test]
        public void SpawnNow_SkipsSeatsWhosePointStillHasALiveOrder()
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables();
            for (int i = 0; i < 10; i++) { seats[i].HasLiveOrder = i != 6; }
            CustomerDirector director = Director(seats, new CustomerDirectorSettings(10));
            Assert.That(director.SpawnNow(), Is.EqualTo(6));
            Assert.That(director.SpawnNow(), Is.EqualTo(-1));
            Assert.That(seats.Where((s, i) => i != 6).All(s => s.Requests.Count == 0), Is.True);
            Assert.That(director.ActiveCustomers, Is.EqualTo(1));
        }

        [Test]
        public void MaxActive_DefaultsToThree_IsConfigurable_AndClampedToSeatCount()
        {
            Assert.That(Spawned(Director(FakeCustomerSeat.Tables())), Is.EqualTo(3));
            Assert.That(Spawned(Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(5))), Is.EqualTo(5));
            Assert.That(Spawned(Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(0))), Is.EqualTo(0));
            CustomerDirector clamped = Director(FakeCustomerSeat.Tables(4), new CustomerDirectorSettings(50));
            Assert.That(clamped.MaxActiveCustomers, Is.EqualTo(4));
            Assert.That(Spawned(clamped), Is.EqualTo(4));
        }

        [Test]
        public void MaxActive_AlsoLimitsTimedArrivals()
        {
            CustomerDirector director = Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(2, 1f));
            for (int i = 0; i < 20; i++) { director.Tick(1d); }
            Assert.That(director.ActiveCustomers, Is.EqualTo(2));
            Assert.That(_seated.Count, Is.EqualTo(2));
        }

        [Test]
        public void Tick_ArrivesOnInterval_AndZeroDeltaIsAPause()
        {
            CustomerDirector director = Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(10, 6f));
            director.Tick(3d);
            for (int i = 0; i < 1000; i++) { director.Tick(0d); }
            director.Tick(-5d);
            director.Tick(double.NaN);
            director.Tick(2d);
            Assert.That(_seated, Is.Empty, "5 game seconds elapsed; paused ticks add nothing.");
            director.Tick(1d);
            Assert.That(_seated.Count, Is.EqualTo(1));
            director.Tick(5d);
            Assert.That(_seated.Count, Is.EqualTo(1));
            director.Tick(1d);
            Assert.That(_seated.Count, Is.EqualTo(2));
        }

        [Test]
        public void Tick_LongFrameSeatsAtMostOneCustomer()
        {
            CustomerDirector director = Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(10, 6f));
            director.Tick(60d);
            Assert.That(_seated.Count, Is.EqualTo(1));
            director.Tick(6d);
            Assert.That(_seated.Count, Is.EqualTo(2));
        }

        [Test]
        public void Menu_ProducesDrinkCakeAndMixedOrders_WithMatchingItems()
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables(60);
            CustomerDirector director = Director(seats, new CustomerDirectorSettings(60));
            Assert.That(Spawned(director), Is.EqualTo(60));
            var kinds = new HashSet<string>();
            foreach (FakeCustomerSeat seat in seats)
            {
                IReadOnlyList<ItemRequest> items = seat.Requests.Single().Items;
                string key = string.Join("+", items.Select(r => r.ItemDefinitionId + "x" + r.Quantity));
                Assert.That(new[] { "drink.testx1", "cake.testx1", "drink.testx1+cake.testx1" }, Has.Member(key));
                kinds.Add(key);
            }
            Assert.That(kinds.Count, Is.EqualTo(3), "Equal weights over 60 customers produce all three menus.");
        }

        [TestCase(1f, 0f, 0f, "drink.test")]
        [TestCase(0f, 1f, 0f, "cake.test")]
        [TestCase(0f, 0f, 1f, "drink.test+cake.test")]
        public void Menu_FollowsWeights(float drink, float cake, float mixed, string expected)
        {
            FakeCustomerSeat[] seats = FakeCustomerSeat.Tables();
            CustomerDirector director = Director(seats, new CustomerDirectorSettings(10, 6f, 2f, drink, cake, mixed));
            Spawned(director);
            Assert.That(seats.Select(s => string.Join("+", s.Requests.Single().Items.Select(r => r.ItemDefinitionId))), Is.All.EqualTo(expected));
        }

        [Test]
        public void TableReleases_AfterOrderEndsPlusClearDelay_AndIsReusedByANewCustomer()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(4);
            CustomerDirector director = Director(new[] { seat }, new CustomerDirectorSettings(1, 100f, 2f));
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            CustomerId first = director.CustomerAt(0);
            Assert.That(first, Is.EqualTo(new CustomerId(100)));
            director.Tick(10d);
            Assert.That(director.IsOccupied(0), Is.True, "Live order keeps the table.");

            seat.HasLiveOrder = false;           // order completed
            director.Tick(0.5d);                 // detection: clear delay starts
            Assert.That(director.IsOccupied(0), Is.True);
            Assert.That(director.SpawnNow(), Is.EqualTo(-1), "A clearing table is not free.");
            director.Tick(0d);                   // paused: clear timer does not run
            director.Tick(1.5d);
            Assert.That(director.IsOccupied(0), Is.True);
            Assert.That(_left, Is.Empty);
            director.Tick(0.5d);
            Assert.That(_left, Is.EqualTo(new[] { 0 }));
            Assert.That(director.IsOccupied(0), Is.False);
            Assert.That(director.CustomerAt(0), Is.EqualTo(default(CustomerId)));
            Assert.That(director.ActiveCustomers, Is.EqualTo(0));

            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.CustomerAt(0).Value, Is.GreaterThan(first.Value));
            Assert.That(seat.Requests.Select(r => r.Customer.Value), Is.EqualTo(new[] { 100, 101 }));
        }

        [Test]
        public void ArrivalTimerHoldsWhileFull_AndSeatsAsSoonAsATableClears()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(1);
            CustomerDirector director = Director(new[] { seat }, new CustomerDirectorSettings(1, 6f, 0f));
            director.Tick(6d);
            Assert.That(_seated.Count, Is.EqualTo(1));
            director.Tick(30d);
            Assert.That(_seated.Count, Is.EqualTo(1));
            seat.HasLiveOrder = false;
            director.Tick(0.1d);                 // zero clear delay: freed and immediately reseated
            Assert.That(_left, Is.EqualTo(new[] { 0 }));
            Assert.That(_seated.Count, Is.EqualTo(2));
            Assert.That(_seated[1].Customer.Value, Is.EqualTo(101));
        }

        [Test]
        public void FailedRequest_LeavesSeatFree_ReportsReason_AndDoesNotThrow()
        {
            FakeCustomerSeat seat = new FakeCustomerSeat(2) { FailWith = "order.point.not_configured" };
            CustomerDirector director = Director(new[] { seat }, new CustomerDirectorSettings(1, 1f));
            Assert.That(director.SpawnNow(), Is.EqualTo(-1));
            Assert.DoesNotThrow(() => director.Tick(5d));
            Assert.That(director.IsOccupied(0), Is.False);
            Assert.That(director.ActiveCustomers, Is.EqualTo(0));
            Assert.That(_seated, Is.Empty);
            Assert.That(_failed.Count, Is.EqualTo(2));
            Assert.That(_failed[0].SeatIndex, Is.EqualTo(0));
            Assert.That(_failed[0].ReasonKey, Is.EqualTo("order.point.not_configured"));

            seat.FailWith = null;
            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(director.IsOccupied(0), Is.True);
        }

        [Test]
        public void CustomerIds_AreUniqueAndIncreasing()
        {
            CustomerDirector director = Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(10), firstId: 500);
            Spawned(director);
            int[] ids = _seated.Select(e => e.Customer.Value).ToArray();
            Assert.That(ids, Is.EqualTo(Enumerable.Range(500, 10).ToArray()));
            for (int i = 0; i < 10; i++) { Assert.That(director.CustomerAt(_seated[i].SeatIndex).Value, Is.EqualTo(ids[i])); }
        }

        [Test]
        public void SeatChoice_IsDeterministicForASeed_AndVariesAcrossSeeds()
        {
            int[] a = Order(Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(10), seed: 42));
            int[] b = Order(Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(10), seed: 42));
            int[] c = Order(Director(FakeCustomerSeat.Tables(), new CustomerDirectorSettings(10), seed: 43));
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a, Is.Not.EqualTo(Enumerable.Range(0, 10).ToArray()), "Seats are not filled in index order.");
        }

        private static int Spawned(CustomerDirector director)
        {
            int count = 0;
            while (director.SpawnNow() >= 0) { count++; }
            return count;
        }

        private static int[] Order(CustomerDirector director)
        {
            var order = new List<int>();
            int seat;
            while ((seat = director.SpawnNow()) >= 0) { order.Add(seat); }
            return order.ToArray();
        }
    }
}
