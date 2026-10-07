using System;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Lobby
{
    public sealed class OrderPointTests
    {
        private LobbyTestKit _kit;

        [SetUp] public void SetUp() => _kit = new LobbyTestKit();
        [TearDown] public void TearDown() => _kit.Dispose();

        [Test]
        public void TableOrderPoint_RequestServiceUsesDineInOriginAndOccupiesThePoint()
        {
            TableOrderPoint table = _kit.Table(tableId: 3);
            Assert.That(table.ActiveOrder.IsValid, Is.False);
            OrderId id = _kit.RequestAt(table);
            Assert.That(table.ActiveOrder, Is.EqualTo(id));
            OrderOrigin origin = _kit.Service.Get(id).Origin;
            Assert.That(origin.Type, Is.EqualTo(OrderType.DineIn));
            Assert.That(origin.TableId, Is.EqualTo(new TableId(3)));
            Assert.That(origin.VehicleId.IsValid, Is.False, "GT-005: dine-in orders have no vehicle.");
            Assert.That(table.Origin, Is.EqualTo(origin));
        }

        [Test]
        public void VehicleOrderPoint_RequestServiceUsesTakeawayOriginAndOccupiesThePoint()
        {
            VehicleOrderPoint vehicle = _kit.Vehicle(vehicleId: 4);
            OrderId id = _kit.RequestAt(vehicle);
            Assert.That(vehicle.ActiveOrder, Is.EqualTo(id));
            OrderOrigin origin = _kit.Service.Get(id).Origin;
            Assert.That(origin.Type, Is.EqualTo(OrderType.TakeawayVehicle));
            Assert.That(origin.VehicleId, Is.EqualTo(new VehicleId(4)));
            Assert.That(origin.TableId.IsValid, Is.False, "GT-006: takeaway orders have no table.");
        }

        [Test]
        public void RequestCustomerService_RejectsASecondLiveOrderWithoutCallingTheService()
        {
            TableOrderPoint table = _kit.Table();
            _kit.RequestAt(table);
            _kit.Service.Calls.Clear();
            Result<OrderId> second = table.RequestCustomerService(new CustomerId(2), LobbyTestKit.Items());
            Assert.That(second.ReasonKey, Is.EqualTo("order.point.occupied"));
            Assert.That(_kit.Service.Calls, Is.Empty);
        }

        [Test]
        public void RequestCustomerService_FreesThePointWhenTheOrderIsTerminal()
        {
            TableOrderPoint table = _kit.Table();
            OrderId first = _kit.RequestAt(table);
            _kit.Service.SetStatus(first, OrderStatus.Completed);
            Assert.That(table.ActiveOrder.IsValid, Is.False);
            OrderId second = _kit.RequestAt(table, 2);
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(table.ActiveOrder, Is.EqualTo(second));
            _kit.Service.SetStatus(second, OrderStatus.Failed);
            Assert.That(table.ActiveOrder.IsValid, Is.False);
        }

        [Test]
        public void RequestCustomerService_ServiceFailureLeavesThePointFree()
        {
            TableOrderPoint table = _kit.Table();
            _kit.Service.RequestFailure = "order.request.rejected";
            Result<OrderId> result = table.RequestCustomerService(new CustomerId(1), LobbyTestKit.Items());
            Assert.That(result.ReasonKey, Is.EqualTo("order.request.rejected"));
            Assert.That(table.ActiveOrder.IsValid, Is.False);
        }

        [Test]
        public void RequestCustomerService_RejectsInvalidArgumentsBeforeTheService()
        {
            TableOrderPoint table = _kit.Table();
            Assert.That(table.RequestCustomerService(default, LobbyTestKit.Items()).ReasonKey, Is.EqualTo("order.point.invalid_customer"));
            Assert.That(table.RequestCustomerService(new CustomerId(1), null).ReasonKey, Is.EqualTo("order.point.empty_request"));
            Assert.That(table.RequestCustomerService(new CustomerId(1), Array.Empty<ItemRequest>()).ReasonKey, Is.EqualTo("order.point.empty_request"));
            Assert.That(_kit.Service.Calls, Is.Empty);
        }

        [Test]
        public void Point_QueryIsHiddenWithoutALiveOrderAndForOrdersPastEntry()
        {
            TableOrderPoint table = _kit.Table();
            Assert.That(table.Query(_kit.Context()).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            OrderId id = _kit.RequestAt(table);
            foreach (OrderStatus status in new[] { OrderStatus.SentToStall, OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.PickedUpByLobby, OrderStatus.Delivered })
            {
                _kit.Service.SetStatus(id, status);
                Assert.That(table.Query(_kit.Context()).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden), status.ToString());
            }
        }

        [Test]
        public void Point_QueryPromptsFollowTheOrderStatusTable()
        {
            VehicleOrderPoint vehicle = _kit.Vehicle();
            OrderId id = _kit.RequestAt(vehicle);
            InteractionQuery take = vehicle.Query(_kit.Context());
            Assert.That(take.Availability.IsAvailable, Is.True);
            Assert.That(take.PromptKey, Is.EqualTo("order.point.take"));
            Assert.That(take.Kind, Is.EqualTo(InteractionKind.Press));
            foreach (OrderStatus status in new[] { OrderStatus.TakingOrder, OrderStatus.Entered })
            {
                _kit.Service.SetStatus(id, status);
                InteractionQuery again = vehicle.Query(_kit.Context());
                Assert.That(again.Availability.IsAvailable, Is.True, status.ToString());
                Assert.That(again.PromptKey, Is.EqualTo("order.point.continue"));
            }
        }

        [Test]
        public void Point_QueryBlocksWithoutLobbyRolePausedClockOrFullHands()
        {
            TableOrderPoint table = _kit.Table();
            _kit.RequestAt(table);
            Assert.That(table.Query(_kit.Context(ActorRole.Stall)).BlockedReasonKey, Is.EqualTo("interaction.lobby_role_required"));
            Assert.That(table.Query(_kit.Context(ActorRole.None)).BlockedReasonKey, Is.EqualTo("interaction.lobby_role_required"));
            Assert.That(table.Query(_kit.Context(ActorRole.Lobby | ActorRole.Stall)).Availability.IsAvailable, Is.True, "DEC-001: one player performs both roles.");
            var hands = new HeldItemSlot();
            hands.TryPickUp(new LobbyTestKit.FakeHoldable());
            Assert.That(table.Query(_kit.Context(hands: hands)).BlockedReasonKey, Is.EqualTo("hands.full"));
            _kit.Clock.Pause();
            Assert.That(table.Query(_kit.Context()).BlockedReasonKey, Is.EqualTo("interaction.paused"));
        }

        [Test]
        public void Point_QueryBlocksUnconfiguredPointsAndRejectsNullContext()
        {
            var host = new UnityEngine.GameObject("Unconfigured");
            try
            {
                var table = host.AddComponent<TableOrderPoint>();
                Assert.That(table.Query(_kit.Context()).BlockedReasonKey, Is.EqualTo("order.point.not_configured"));
                Assert.That(table.Origin.IsValid, Is.False);
                Assert.That(table.Id.IsValid, Is.False);
                Assert.That(table.RequestCustomerService(new CustomerId(1), LobbyTestKit.Items()).ReasonKey, Is.EqualTo("order.point.not_configured"));
                Assert.Throws<ArgumentNullException>(() => table.Query(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Point_ExecuteTakesTheOrderOnceThenReopensWithoutAnotherBeginTaking()
        {
            TableOrderPoint table = _kit.Table(tableId: 2);
            OrderId id = _kit.RequestAt(table);
            table.Execute(_kit.Context());
            Assert.That(_kit.Service.Calls, Is.EqualTo(new[] { "RequestService", "BeginTaking" }));
            Assert.That(_kit.Service.BeginActors, Is.EqualTo(new[] { LobbyTestKit.Player }));
            Assert.That(_kit.Service.BeginPoints, Is.EqualTo(new[] { table.Origin }));
            Assert.That(_kit.Service.Get(id).Status, Is.EqualTo(OrderStatus.TakingOrder));
            Assert.That(_kit.Requested.Count, Is.EqualTo(1));
            Assert.That(_kit.Requested[0].OrderId, Is.EqualTo(id));
            Assert.That(_kit.Requested[0].RequestedItems[0].ItemDefinitionId, Is.EqualTo("drink.test"));
            Assert.That(_kit.Controller.HasEntrySession, Is.True);
            Assert.That(_kit.Controller.EntryOrder, Is.EqualTo(id));
            Assert.That(table.Query(_kit.Context()).PromptKey, Is.EqualTo("order.point.continue"));
            table.Execute(_kit.Context());
            Assert.That(_kit.Service.Calls, Is.EqualTo(new[] { "RequestService", "BeginTaking" }));
            Assert.That(_kit.Requested.Count, Is.EqualTo(2), "Continue reopens the entry UI.");
            Assert.That(_kit.Blocked, Is.Empty);
        }

        [Test]
        public void Point_ExecuteBlockedPublishesActionBlockedAndNeverTouchesTheOrder()
        {
            TableOrderPoint table = _kit.Table(interactableId: 15);
            OrderId id = _kit.RequestAt(table);
            _kit.Service.Calls.Clear();
            table.Execute(_kit.Context(ActorRole.Stall));
            _kit.Clock.Pause();
            table.Execute(_kit.Context());
            Assert.That(_kit.Service.Calls, Is.Empty);
            Assert.That(_kit.Service.Get(id).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_kit.Blocked.Count, Is.EqualTo(2));
            Assert.That(_kit.Blocked[0].ReasonKey, Is.EqualTo("interaction.lobby_role_required"));
            Assert.That(_kit.Blocked[1].ReasonKey, Is.EqualTo("interaction.paused"));
            Assert.That(_kit.Blocked[0].InteractableId, Is.EqualTo(new InteractableId(15)));
            Assert.That(_kit.Requested, Is.Empty);
            Assert.That(_kit.Controller.HasEntrySession, Is.False);
        }

        [Test]
        public void Point_ExecuteWithoutAnOrderDoesNothing()
        {
            TableOrderPoint table = _kit.Table();
            table.Execute(_kit.Context());
            Assert.That(_kit.Service.Calls, Is.Empty);
            Assert.That(_kit.Blocked, Is.Empty);
            Assert.That(_kit.Requested, Is.Empty);
        }

        [Test]
        public void Point_ExecuteReportsBeginTakingFailureWithoutOpeningTheEntry()
        {
            TableOrderPoint table = _kit.Table(interactableId: 12);
            OrderId id = _kit.RequestAt(table);
            _kit.Service.BeginFailure = "order.actor_not_at_point";
            table.Execute(_kit.Context());
            Assert.That(_kit.Service.Get(id).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_kit.Requested, Is.Empty);
            Assert.That(_kit.Controller.HasEntrySession, Is.False);
            Assert.That(_kit.Blocked.Count, Is.EqualTo(1));
            Assert.That(_kit.Blocked[0].ReasonKey, Is.EqualTo("order.actor_not_at_point"));
            Assert.That(_kit.Blocked[0].InteractableId, Is.EqualTo(new InteractableId(12)));
        }

        [Test]
        public void Point_InitializeValidatesArguments()
        {
            var host = new UnityEngine.GameObject("Init");
            try
            {
                var table = host.AddComponent<TableOrderPoint>();
                Transform point = host.transform;
                Assert.Throws<ArgumentOutOfRangeException>(() => table.Initialize(0, point, _kit.Service, _kit.Controller, new TableId(1)));
                Assert.Throws<ArgumentNullException>(() => table.Initialize(1, null, _kit.Service, _kit.Controller, new TableId(1)));
                Assert.Throws<ArgumentNullException>(() => table.Initialize(1, point, null, _kit.Controller, new TableId(1)));
                Assert.Throws<ArgumentNullException>(() => table.Initialize(1, point, _kit.Service, null, new TableId(1)));
                Assert.Throws<ArgumentException>(() => table.Initialize(1, point, _kit.Service, _kit.Controller, default));
                var vehicle = host.AddComponent<VehicleOrderPoint>();
                Assert.Throws<ArgumentException>(() => vehicle.Initialize(1, point, _kit.Service, _kit.Controller, default));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
