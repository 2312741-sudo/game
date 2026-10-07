# DRINK-WAVE Lobby/UI — implementation report (CX-023 / CX-024 subset)

Implementation scope record by Claude Code, on request of the root integrator. This is **not** a review: the root
integrator reviews this PR independently, and any Claude review of Codex work lives under `Docs/Reviews/`.

Base: `develop` `01e50b4` (frozen contracts, PR #8). Branch: `feature/DRINK-WAVE-lobby-ui`.

## Scope (owned files only)

| Area | Files |
|---|---|
| Lobby (`TramChanh.Lobby`) | `LobbyOrderController`, `OrderPoint` (shared base), `TableOrderPoint`, `VehicleOrderPoint` |
| UI (`TramChanh.UI.Orders`) | `OrderEntryUI`, `OrderEntryModel` (engine-free, internal) |
| Data | `ScriptableObjects/UI/SO_PromptText_OrderEntry.asset` (English + Vietnamese) |
| Tests (EditMode) | `Tests/EditMode/Lobby/*` (fake `IOrderService`, kit, point and controller tests), `Tests/EditMode/UI/*` |

Unchanged: `IOrderService`, everything under `Scripts/Orders`, `Scripts/Core`, `Scripts/Interaction`, asmdefs, scenes, prefabs, input assets, docs.
Not implemented (out of scope): delivery (T8), completion (T9), `PickUp`, any production `IOrderService`, scene wiring.

## Signatures for the root integration

- `LobbyOrderController.Initialize(IOrderService, IEventBus)`; `OpenEntry(InteractableId, OrderId, ActorRef, OrderOrigin)` (called by points); `CloseEntry()`; `Disconnect()`; `HasEntrySession`, `EntryOrder`.
- `TableOrderPoint.Initialize(int interactableId, Transform interactionPoint, IOrderService, LobbyOrderController, TableId)`; `VehicleOrderPoint.Initialize(..., VehicleId)`.
- Points: `OrderId ActiveOrder`, `OrderOrigin Origin`, `Result<OrderId> RequestCustomerService(CustomerId, IReadOnlyList<ItemRequest>)`, plus `IInteractable` (`Id`, `InteractionPoint`, `Query`, `Execute`).
- `OrderEntryUI.Initialize(IEventBus)`; serialized `_table` (`PromptLocalizationTable`) and `_language`; own `UIDocument`; `Enter()`, `Send()`, `Cancel()` return/act without a Lobby reference; `Shown` / `Hidden` events for the host to pause or capture the cursor.

## Behaviour decisions

- **T2 point query** (ORDER_SYSTEM §7): hidden without a live order or past entry; otherwise blocked in this order by role (`interaction.lobby_role_required`, needs `ActorRole.Lobby`), clock pause (`interaction.paused`), full hands (`hands.full`); prompts `order.point.take` (WaitingForLobby) / `order.point.continue` (TakingOrder, Entered). Execute republishes `ActionBlocked(point id, reason)` when blocked or when the controller refuses.
- **One live order per point.** Terminal orders (`Completed`, `Failed`) free the point; `Delivered` still occupies it until Completed.
- **Authorized session.** `OpenEntry` authorizes one session (point id + order id). `OrderEntryConfirmed` / `OrderSendRequested` for that order are accepted **without any clock check**, so the UI may pause the clock; a new point interaction stays blocked while paused. Events for another order, an invalid id, or with no session are ignored. The session closes on a successful send, on any status past `Entered`, on `CloseEntry`, or on `Disconnect`.
- **Failures** from `Enter`/`SendToStall` are reported as `ActionBlocked(session point id, reasonKey)`; the UI shows the localized reason and stays open.
- **Event rule** (ORDER_SYSTEM §6.4): after the order service has committed, a faulting observer of `OrderEntryRequested` is logged and does not change the result.
- **Entry is pre-filled** from `RequestedItems` (DEC-012); the UI sends the same list back. Enter and Send are separate confirmations (DEC-003).
- **Keys.** `order.entry.title|enter|send|close`, `item.<definitionId>` (unknown ids fall back to the key; no item ids are invented here), plus prompt and reason keys listed in the asset.

## Host notes (root)

- `PlayerInputReader.Update` recaptures the cursor on Interact (E / left mouse) while uncaptured, so clicking a UI button after `SetCaptured(false)` can re-lock the cursor in the same frame. `OrderEntryUI.Shown/Hidden` are provided so the host can decide how to pause; this PR does not touch `PlayerInputReader`.
- `InteractionPromptView` needs `order.point.take` / `order.point.continue` and the reason keys in its table; merge the entries from `SO_PromptText_OrderEntry.asset` or assign that asset.

## Validation

- **Unity 6000.6.0f1: not available in this environment, not run.** Root must compile and run EditMode/PlayMode and open the project before merge. `OrderEntryUITests` (needs `UIDocument`, `SerializedObject`, `AssetDatabase`) and the `LogAssert.Expect` fault test could only be checked by reading.
- Out-of-Unity .NET 8 harness (NUnitLite; managed stand-ins for `UnityEngine.Object/Transform/GameObject`): compiled `Core` + `Orders` + the used `Interaction` types + `Lobby` + `OrderEntryModel` + the Lobby and model tests with zero errors and zero warnings; **39/39 pass**.
- Mutation probes (each made the named tests fail): remove the session check in `OnSendRequested`; remove the pause guard in `Query`; stop treating `Failed` as terminal; make Send also confirm; remove the origin check in `OpenEntry`.
- Assembly boundaries are asserted by tests (`Lobby` and `UI` reference neither each other nor `Stall`); the real asmdefs are unchanged and already enforce this.
- `.meta` files were created with fresh GUIDs for every new file and folder; Unity may rewrite the minimal script metas on first import (same as existing scripts).

## Follow-up (PR #12 N4 and stale-reason refresh)

- `SO_PromptText_OrderEntry.asset` now carries the reason keys the real `OrderService` emits (`order.transition.invalid`, `order.point.wrong`, `order.point.occupied`, `order.origin.invalid`, `order.actor.invalid`, `order.items.*`, `order.too_many_for_shelf`, `order.failure.invalid`; ORDER_SYSTEM section 6.5). The wrong key `order.invalid_transition` was replaced. Controller- and point-owned keys are kept and completed (`order.wrong_point`, `order.entry.not_available`, `order.point.not_configured`, plus `order.not_found`, `order.entry.empty`, `order.lobby.not_configured`, `order.point.*`). A script cross-check found no `order.*` literal in PR #10's `OrderService.cs` or in `Scripts/Lobby` that is missing from the asset; `OrderEntryUITests` asserts the full list in both languages.
- `OrderEntryModel.Enter` / `Send` now raise `Changed` when they clear a stale `BlockedReasonKey`, so the UI refreshes. Regression tests: `SuccessfulEnterAfterAFailedSendRefreshesTheUiWhenTheReasonIsCleared`, `SendClearsAStaleReasonAndNotifiesOnlyWhenThereWasOne`, `FailureReportedWhileSendingIsShownAfterTheClear` (model) and `StaleReasonLabelDisappearsWhenAnEnterRetryClearsIt` (UI, Unity-only).
- Harness: 42/42 pass; removing the new `Changed` call fails the new model tests. Unity not run; root re-validates.

## Independent local Unity validation

Root reviewed the production points, controller, UI/model, localization and regressions against frozen contracts. Final runtime/test head `6da62ce`: Unity6000.6.0f1 compile PASS with zero C# diagnostics; full EditMode158/158, PlayMode25/25 PASS; QA000 PASS on clean detached checkout. Package pins unchanged. Actual service and modal gameplay-input integration are verified separately by the integration PR.
