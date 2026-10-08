# ACCEL-01 locked minimum shared contracts

The existing intake, queue, held-item actions and atomic Ready shelf remain authoritative. Additive capability interfaces avoid forcing existing intake fakes or preparation modules to implement delivery. No package or Unity version changes.

## Delivery
`IOrderDelivery.Deliver(OrderId, ActorRef, DeliveryTarget)` accepts only a positive actor and PickedUpByLobby order. `DeliveryTarget` contains `OrderOrigin` and `CustomerId`; both must match. Wrong valid target increments attempts once and emits DeliveryRejected, keeps order/bundle in hands. Invalid/default target/actor rejects without mutation. Matching delivery commits Delivered and mean item quality before status publication. `Complete` accepts only Delivered, commits Completed, removes the active order/point occupancy before publication; terminal repetition rejects. Existing FIFO observer outbox isolates faults/reentrancy. `IOrderDeliveryInfo` exposes attempts/quality without changing IReadOnlyOrder fakes.

Lobby's OrderPoint handles delivery only while holding the matching ServedOrder capability; wrong-point attempts route to that held order for explicit rejection. After a committed successful handoff, release hands and retire visuals even when an observer faults. Failed carried orders must not remain deliverable. Root injects service implementing IOrderDelivery; no production intake bypass. Table/vehicle customer identities persist through handoff. Slice completion follows delivery immediately through explicit Complete, preserving both canonical events/statuses.

## Cake and feedback
Cakes own explicit documented states Waiting, BatterMeasured, BatterPoured, Cooking, Cooked, Flipped, Cut, Sauced, Rolled, Wrapped, Ready, Delivered, Ruined. Measurement uses per-recipe TargetBatterMl/BatterToleranceMl, cup capacity 500 only; configurable fill/topup/empty before pour. First measurement claims Cake ticket, recipe resolves from claimed item definition. No hardcoded universal recipe. Out-of-tolerance policy configurable per existing docs. Cooking/burning uses injected game clock, pause-safe. IFlipAction lives within Cakes and remains replaceable; placeholder moves cake to RollArea. Root calls a Cake station Initialize(queue, ids, events, clock, recipe catalog) seam defined within Cakes. Cake MarkReady remains silent/failure atomic under existing IPreparedItem contract.

IPreparationFeedback lives in Core and exposes only localized PreparationStateKey and NextActionKey. Cake/cup and tea bag may implement it; UI consumes generic held IHoldable plus this interface, never references Cakes/Drinks. Root uses Orders events/read-only order service to populate UI. No real prices/quantities inferred by the UI.

## Composition/scene ownership
Root owns AccelGameplayBootstrap and final wiring. Scene lane saves environment prefab/scene with discoverable `StallRoot`, `TablePoint`, `VehiclePoint`, `PlayerSpawn`, `LobbyPosition` and `ReadyHandoff` anchors. Stall roots retain 1.8 x0.8m, 1m counter, approx2.2m roof; only new sign. Cake builder outputs self-contained station prefab with named BatterArea/BatterSource/Grill/RollArea/WrappingArea anchors and stable canonical tool roots. Scene layout is environment only until root installs validated station adapters.

Tests must guard production ticket entry, wrong delivery identity, observer faults/reentrancy, mixed readiness and exact transitions. Real data remains TBD; devseed assets names and inspector notes explicitly provisional.
