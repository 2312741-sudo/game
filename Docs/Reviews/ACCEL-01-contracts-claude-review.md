# ACCEL-01 shared contracts — Claude Code architecture review

| | |
|---|---|
| Reviewed | `wave/ACCEL-01-playable-tram-chanh` commit `f4eb584` (additive): `Core/IPreparationFeedback.cs`, `Orders/{IOrderDelivery, DeliveryTarget, DeliveryRejected, IOrderDeliveryInfo}.cs`, `Docs/ACCEL-01_CONTRACTS.md`, `tasks/plan.md`, `tasks/todo.md` (+ `.meta` files). Parent `395b8f1` (PR #15 head, on `develop` `d9143c1`) |
| Reviewer | Claude Code (Technical Lead / Architect). Review only; no runtime edits, rebases or merges |
| Date | 2026-10-08 (local) |
| Verdict | **CHANGES REQUESTED** — the direction is right and the commit is safely additive, but three small shared-contract gaps (B1–B3) must be closed **before the cake and orders lanes fork**, otherwise two lanes will invent incompatible solutions. B4 must close before any lane PR merges |

## Verified

- **Additive only.** The commit adds 13 files and changes none. `IOrderService`, `IStallTicketQueue`, `IPreparedItem`, `IReadyShelf*`, the intake events and the held-item interfaces are untouched.
- **Compiles.** The new Core and Orders files compile with the existing sources in my out-of-Unity .NET 8 harness (0 errors); the 84 existing Orders tests give 83 passes, the one failure being the known harness artifact (`ARCH001`, Core and Orders share an assembly there). No tests cover the new types yet.
- **Capability segregation is right.** Delivery in its own interface follows the interface-segregation rule of ORDER_SYSTEM §6.1; `Complete` having no actor is acceptable because only the composition root injects `IOrderDelivery` into the adapters that may complete. `IPreparationFeedback` is a Core, string-only, read-only interface, so UI never references Cakes or Drinks (public-API rule respected). `DeliveryTarget` uses only Core and Orders types.
- **Rules consistent with the amended docs.** Wrong valid target: order stays `PickedUpByLobby`, attempts +1, `DeliveryRejected` (ORDER_SYSTEM §3.2, TC-ORDER-004); mean item quality committed before status publication (§8); `Complete` frees point occupancy before publication; the FIFO outbox isolates observer faults (§6.4); hands are released after the committed handoff even if an observer faults (§6.4 items 4 and 6); Failed carried orders must not stay deliverable (known CX-025 item).
- **Cake state list and rules match `CAKE_WORKFLOW.md`** (Waiting … Ready, Delivered, Ruined; recipe-driven `TargetBatterMl` / `BatterToleranceMl`; 500 ml capacity only; `IFlipAction` inside Cakes; pause-safe clock; `MarkReady` silent and failure-atomic).

## Blockers (shared-contract changes before the lanes fork)

**B1 — the cake lane cannot resolve its recipe from the claim.** The contract says the recipe "resolves from the claimed item definition", but `IStallTicketQueue.ClaimNext(ItemKind, PreparationId)` returns only an `OrderItemRef` (`OrderId`, `OrderItemId`, `PreparationId`), and the Cakes assembly deliberately has no `IOrderService` (it would expose intake). Nothing in the frozen contracts yields the item's `ItemDefinitionId`. Four cake menu items with different `CakeRecipe`s (and sauce mapping, DEC-008) cannot be served by guessing. Required, additive (suggested shape, in `Orders`, implemented by `StallTicketQueue` / `OrderService`):

```csharp
public interface IOrderItemCatalog          // read-only, stall side
{
    // false for a stale, unbound or failed-order ref
    bool TryGetItemDefinition(OrderItemRef item, out string itemDefinitionId);
}
```

and a Cakes-owned `ICakeRecipeCatalog` (definition id → `CakeRecipe`) that the root fills from content. Document that Drinks keep their single `DrinkRecipe` until a second drink exists.

**B2 — `IOrderDeliveryInfo` has no access path or semantics.** It has two properties and no order id, so it only works as an interface that the order object also implements and callers reach by `order as IOrderDeliveryInfo`. That silently yields `null` for any `IReadOnlyOrder` fake and is undiscoverable. Since nothing implements it yet, decide now: preferably a member of the delivery capability, `bool TryGetInfo(OrderId id, out OrderDeliveryInfo info)` with a `readonly struct OrderDeliveryInfo { int Attempts; int QualityScore; }`; otherwise state the cast contract in the doc and add a test that `OrderService.Get` always returns an object implementing it. Specify: `QualityScore` is 0 until `Delivered`, then `round(mean(item.Quality))` (0..100); `DeliveryAttempts` counts only wrong-target attempts on a `PickedUpByLobby` order. Also give `DeliveryTarget` an `IsValid` (a `default` struct is constructible and the constructor throws for invalid input, so `Deliver` must reject `default`) and value equality, so tests and `DeliveryRejected` can compare targets.

**B3 — ownership gap for `CakeRecipe`.** `ARCHITECTURE.md` places `CakeRecipe`, `SauceDefinition` and the measuring-cup definition in `TramChanh.Content`, but the plan forbids the cake agent from touching shared files and limits it to `Scripts/Cakes/**`, new Cakes tests, the cake prefab builder and `Data/ACCEL01/Cakes/**`. Content files therefore belong to nobody. Either the root adds those ScriptableObject classes (plus their `[Tbd("DEC-007")]` fields) to `Content` in the contract commit, or the documents are amended to place them in `TramChanh.Cakes`. Decide before the fork; the first option keeps the documented architecture.

**B4 — no contract tests yet (blocks lane merges, not the fork).** Add before any lane PR merges: `ARCH-001` coverage for `IOrderDelivery`, `IOrderDeliveryInfo` and `IPreparationFeedback`; `DeliveryTarget` default/invalid cases; and a shared `DeliveryContractAssertions` fixture with a fake in the style of `PreparedItemContractAssertions` (wrong target changes only attempts and publishes exactly one `DeliveryRejected`; repeated wrong attempts each count; invalid actor or default target mutate nothing and publish nothing; a rejected state transition never increments attempts; a rejection that has already committed never latches and breaks a later success; events are silent until the owner's flush). The `tasks/todo.md` item "contract tests" is still open.

## Non-blocking findings

1. **Delivery operating rules to write down (Orders lane).** (a) Who calls `Complete` and what happens if it fails after `Deliver` committed (suggest: the point adapter, right after the hand release; a failure is an invariant violation, logged, and the order stays `Delivered`, which `Fail` rejects, so the point would stay occupied — add a test). (b) A free point has no customer, so it cannot build a `DeliveryTarget`: hide or block the deliver prompt without counting an attempt. (c) `ServedOrder` already receives the hand slot in `OnPickedUp(IHeldItemSlot)`; it can release the hand and retire its visuals when its order fails. (d) A bundle held for order A at point B must route the attempt to A explicitly (the plan says so) and never complete B.
2. **`DeliveryRejected` naming.** ORDER_SYSTEM §3.2 calls the fields `OrderId` / `AttemptedTarget` / `Reason`; the struct uses `Order` / `AttemptedTarget` / `ReasonKey`. Align the doc (I will on the PR #6 branch) or the struct. List the delivery reason keys (`order.delivery.wrong_target`, invalid actor/target, `order.transition.invalid`) so the root's localization pass (N1) covers them.
3. **`IPreparationFeedback`.** Name the properties as the plan says (`StateKey`) or fix the plan; define the key namespaces (for example `feedback.state.*`, `feedback.next.*`), empty/null meaning "hide"; require constant strings (no per-frame allocation) and pure getters; require each implementer to expose its finite key set so a test can assert every key exists in the localization table (the drift found in PR #15 N1). Because the cup is read from its graduation marks (CAKE_WORKFLOW §2.2), no numeric gauge is required now; if a HUD gauge is wanted later, add a separate optional interface rather than changing this one.
4. **Cake claim and discard rules.** CAKE_WORKFLOW C1 claims the ticket when a measurement is recorded on release, the contract says "first measurement"; use the document's wording. Specify what happens to the claim on `EmptyBatter` (back to `Waiting`) and on `Ruined` → discard (C13, TC-ORDER-009: `Release`, the item returns to `Pending`, the next measurement re-claims). The binding (`PreparationId`) must follow the batter from cup to cake object.
5. **Anchor names.** The contract's cake anchors (`BatterSource`, `Grill`, `RollArea`, `WrappingArea`) and the environment anchors (`StallRoot`, `TablePoint`, `VehiclePoint`, `PlayerSpawn`, `LobbyPosition`, `ReadyHandoff`) do not match `StallAnchorId` (`Grill`, `BatterArea`, `RollArea`, `Sauce`, `Wrap`, `ReadyCounter`) or ASSET_INTEGRATION §5. Reconcile and record the canonical names there and in QA-000. The cake station must be placed from the saved `StallAnchorSet` poses exactly as the drink station is.
6. **Scene lane must reuse, not copy, the stall.** Reference `PF_Stall_TramChanh` and its `StallAnchorSet` so GT-001 stays defined once (`StallDimensions`) and the blockout comparison tests keep their meaning; the new sign only.
7. **Folder and plan sprawl.** `Data/ACCEL01`, `Prefabs/ACCEL01`, `Materials/ACCEL01` and `Scenes/ACCEL01` are outside the layout in ASSET_INTEGRATION §3 (`ScriptableObjects/…`, `Prefabs/…`), and `tasks/plan.md` / `tasks/todo.md` duplicate `Docs/PROJECT_TASK_PLAN.md`. Use the canonical folders or amend the layout, and keep one task board.
8. **Mixed readiness** is covered by the existing atomic shelf (capacity one per kind); make the acceptance tests name drink-only, cake-only and mixed orders, plus a delivery attempt at the wrong point with a mixed bundle.

## Merge readiness

Do not branch the cake and orders lanes from `f4eb584` as it stands. After B1–B3 are folded into the contract commit (one small follow-up commit is enough) I will re-check only that delta. B4 and the items in N1 are tracked per lane. Nothing here asks for runtime changes in Orders, Drinks, Lobby or the UI by me; the root owns the shared files.

---
_Generated by [Claude Code](https://claude.ai/code)_
