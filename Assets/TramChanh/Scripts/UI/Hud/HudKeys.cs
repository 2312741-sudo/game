using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.UI.Hud
{
    /// <summary>Every localization key the player HUD composes. Tests assert each one resolves.</summary>
    public static class HudKeys
    {
        // Origins: "Table {0}" / "Vehicle {0}".
        public const string OriginTable = "hud.origin.table";
        public const string OriginVehicle = "hud.origin.vehicle";

        // Order list.
        public const string OrdersTitle = "hud.orders.title";
        public const string OrdersEmpty = "hud.orders.empty";
        public const string OrdersMore = "hud.orders.more";
        public const string OrderHeader = "hud.order.header";
        public const string OrderStageProgress = "hud.order.stage_progress";
        public const string OrderItemQuantity = "hud.order.item_qty";
        public const string OrderItemStatus = "hud.order.item_status";
        public const string NextPrefix = "hud.next.prefix";

        // Next steps (where to go / what to do).
        public const string NextTakeOrder = "hud.next.take_order";
        public const string NextEnterOrder = "hud.next.enter_order";
        public const string NextSendOrder = "hud.next.send_order";
        public const string NextMakeDrink = "hud.next.make_drink";
        public const string NextMakeCake = "hud.next.make_cake";
        public const string NextMakeItems = "hud.next.make_items";
        public const string NextFinishItem = "hud.next.finish_item";
        public const string NextPickUp = "hud.next.pickup";
        public const string NextDeliverTo = "hud.next.deliver_to";
        public const string NextCompleteAt = "hud.next.complete_at";
        public const string NextDrinkSteps = "hud.next.drink_steps";
        public const string NextCakeSteps = "hud.next.cake_steps";
        /// <summary>ServedOrder.NextActionKey (Lobby); kept resolvable even though the HUD prefers <see cref="NextDeliverTo"/>.</summary>
        public const string NextDelivery = "hud.next.delivery";

        // Held item panel.
        public const string HeldTitle = "hud.held.title";
        public const string HeldEmpty = "hud.held.empty";
        public const string HeldEmptyHint = "hud.held.empty_hint";
        public const string HeldUnknown = "hud.held.unknown";
        public const string HeldKindDrink = "hud.held.kind.drink";
        public const string HeldKindCake = "hud.held.kind.cake";
        public const string HeldStateFinished = "hud.held.state.finished";
        public const string HeldForOrder = "hud.held.for_order";
        public const string HeldOrderCancelled = "hud.held.order_cancelled";
        public const string HeldStateLabel = "hud.held.state_label";
        public const string HeldNextLabel = "hud.held.next_label";
        public const string HeldTypePrefix = "held.";

        // Hold / continuous feedback.
        public const string HoldProgress = "hud.hold.progress";
        public const string HoldContinuous = "hud.hold.continuous";

        // Toasts.
        public const string ToastCompleted = "hud.toast.completed";
        public const string ToastFailed = "hud.toast.failed";
        public const string ToastReady = "hud.toast.ready";
        public const string ToastSent = "hud.toast.sent";
        public const string ToastWrongTarget = "hud.toast.wrong_target";

        public const string ItemPrefix = "item.";
        public const string OrderStatusPrefix = "order.status.";
        public const string ItemStatusPrefix = "order.item.status.";

        public static string ItemName(string itemDefinitionId) => ItemPrefix + itemDefinitionId;
        public static string Status(OrderStatus status) => OrderStatusPrefix + status;
        public static string Status(OrderItemStatus status) => ItemStatusPrefix + status;
        public static string HeldType(Type type) => type == null ? HeldUnknown : HeldTypePrefix + type.Name;
        public static string Kind(ItemKind kind) => kind == ItemKind.Cake ? HeldKindCake : HeldKindDrink;

        public static HudText Origin(OrderOrigin origin)
        {
            if (origin.Type == OrderType.TakeawayVehicle && origin.VehicleId.IsValid) { return HudText.Of(OriginVehicle, origin.VehicleId.Value); }
            if (origin.TableId.IsValid) { return HudText.Of(OriginTable, origin.TableId.Value); }
            return HudText.Of(HeldUnknown);
        }

        /// <summary>Constant keys plus every order/item status key.</summary>
        public static IReadOnlyList<string> All
        {
            get
            {
                var keys = new List<string>
                {
                    OriginTable, OriginVehicle, OrdersTitle, OrdersEmpty, OrdersMore, OrderHeader, OrderStageProgress, OrderItemQuantity,
                    OrderItemStatus, NextPrefix, NextTakeOrder, NextEnterOrder, NextSendOrder, NextMakeDrink, NextMakeCake, NextMakeItems,
                    NextFinishItem, NextPickUp, NextDeliverTo, NextCompleteAt, NextDrinkSteps, NextCakeSteps, NextDelivery, HeldTitle,
                    HeldEmpty, HeldEmptyHint, HeldUnknown, HeldKindDrink, HeldKindCake, HeldStateFinished, HeldForOrder, HeldOrderCancelled,
                    HeldStateLabel, HeldNextLabel, HoldProgress, HoldContinuous, ToastCompleted, ToastFailed, ToastReady, ToastSent,
                    ToastWrongTarget
                };
                foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus))) { keys.Add(Status(status)); }
                foreach (OrderItemStatus status in Enum.GetValues(typeof(OrderItemStatus))) { keys.Add(Status(status)); }
                return keys;
            }
        }
    }
}
