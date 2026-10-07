using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Lobby
{
    /// <summary>Wires the real EventBus/ManualClock to a fake order service and the Lobby adapters.</summary>
    public sealed class LobbyTestKit : IDisposable
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        public EventBus Events { get; } = new EventBus();
        public ManualClock Clock { get; } = new ManualClock();
        public FakeOrderService Service { get; }
        public LobbyOrderController Controller { get; }
        public List<ActionBlocked> Blocked { get; } = new List<ActionBlocked>();
        public List<OrderEntryRequested> Requested { get; } = new List<OrderEntryRequested>();
        public static readonly ActorRef Player = new ActorRef(7);

        public LobbyTestKit()
        {
            Service = new FakeOrderService(Events);
            Controller = Create("Lobby").AddComponent<LobbyOrderController>();
            Controller.Initialize(Service, Events);
            Events.Subscribe<ActionBlocked>(Blocked.Add);
            Events.Subscribe<OrderEntryRequested>(Requested.Add);
        }

        public TableOrderPoint Table(int interactableId = 11, int tableId = 1)
        {
            GameObject host = Create("Table" + tableId);
            var point = host.AddComponent<TableOrderPoint>();
            point.Initialize(interactableId, host.transform, Service, Controller, new TableId(tableId));
            return point;
        }

        public VehicleOrderPoint Vehicle(int interactableId = 21, int vehicleId = 1)
        {
            GameObject host = Create("Vehicle" + vehicleId);
            var point = host.AddComponent<VehicleOrderPoint>();
            point.Initialize(interactableId, host.transform, Service, Controller, new VehicleId(vehicleId));
            return point;
        }

        public InteractionContext Context(ActorRole role = ActorRole.Lobby, IHeldItemSlot hands = null)
        {
            return new InteractionContext(Player, role, hands ?? new HeldItemSlot(), Clock, Events);
        }

        public static IReadOnlyList<ItemRequest> Items(string id = "drink.test", int quantity = 1)
        {
            return new[] { new ItemRequest(id, quantity) };
        }

        public OrderId RequestAt(OrderPoint point, int customer = 1)
        {
            return point.RequestCustomerService(new CustomerId(customer), Items()).Value;
        }

        private GameObject Create(string name)
        {
            var host = new GameObject(name);
            _objects.Add(host);
            return host;
        }

        public void Dispose()
        {
            foreach (GameObject host in _objects) { UnityEngine.Object.DestroyImmediate(host); }
            Events.Dispose();
        }

        public sealed class FakeHoldable : IHoldable
        {
            public Transform HandGrip => null;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }
    }
}
