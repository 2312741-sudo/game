using System;
using TramChanh.Core;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Lobby
{
    /// <summary>Takeaway order point for a waiting vehicle (GT-006). Knows nothing about the vehicle model (DEC-010).</summary>
    public sealed class VehicleOrderPoint : OrderPoint
    {
        [SerializeField] private int _vehicleId;
        public VehicleId VehicleId => _vehicleId > 0 ? new VehicleId(_vehicleId) : default;
        public override OrderOrigin Origin => VehicleId.IsValid ? OrderOrigin.ForVehicle(VehicleId) : default;

        public void Initialize(int interactableId, Transform interactionPoint, IOrderService orders, LobbyOrderController controller, VehicleId vehicleId)
        {
            if (!vehicleId.IsValid) { throw new ArgumentException("Vehicle identity is required.", nameof(vehicleId)); }
            Configure(interactableId, interactionPoint, orders, controller);
            _vehicleId = vehicleId.Value;
        }
    }
}
