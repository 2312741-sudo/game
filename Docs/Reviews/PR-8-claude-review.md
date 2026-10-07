# PR #8 — Claude Code review of the frozen drink-wave contracts (REV-002)

| | |
|---|---|
| PR | [#8 DRINK-WAVE: freeze order and prepared-item contracts](https://github.com/2312741-sudo/game/pull/8) (draft) |
| Head reviewed | `0802648` (branch `feature/DRINK-WAVE-contracts`) |
| Review history | `e7c1515` (also covers `71a00a2`): CHANGES REQUESTED → `56d8bfb` strengthened the shared fixture (binding drop caught; latch and early publication still not) → `2121c0e` and `0802648` close those gaps: **this review** |
| Base | `develop` `17e21cd` (PR #7, metadata freeze, merged). The branch carries PR #7's pre-squash commit `87c9291`, byte-identical to `develop`'s content (74 files, 0 differences), so a squash-merge is harmless |
| Reviewed against | PR #6 amended docs: `ORDER_SYSTEM.md`, `DRINK_WORKFLOW.md`, `ARCHITECTURE.md`, `CODING_CONVENTIONS.md`, `Reviews/DRINK-WAVE-claude-plan-review.md` |
| Reviewer | Claude Code (Technical Lead / Architect) |
| Date | 2026-10-07 |
| **Verdict** | **APPROVED — interface design frozen at `0802648`.** The PR is **not yet merge-ready**: three comment/test-only conditions (M1–M3) and one before the drink-preparation fork (F1). No re-design, and the orders branch may fork now |

## What matches the docs

| Area | Result |
|---|---|
| `ItemKind` in `TramChanh.Core`, `Drink = 0`, `Cake = 1` | ✅ test pins the values |
| `OrderItemRef` | ✅ typed ids, `default` invalid, equality over all three fields, constructor rejects invalid parts |
| `IStallTicketQueue` | ✅ `Tickets`, `HasPending`, `ClaimNext(ItemKind, PreparationId)`, `Release → Result`, `IsBound` |
| `IPreparedItem` | ✅ `Kind`, `BoundItem`, `IsFinished`, `Quality`, `Result MarkReady()`, now with a comment stating the rule |
| Shelf split | ✅ `IReadyShelfPlacement` (`CanPlace → Availability`, `PlaceReady`, `Occupied`), `IReadyShelfPickup` (`NextReadyOrder`, `PickUp(OrderId, ActorRef)`), union `IReadyShelf` |
| `IOrderService` | ✅ no `PickUp`; `BeginTaking(id, actor, OrderOrigin point)`; `Deliver` / `Complete` not included (CX-025, additive; docs amended) |
| Events | ✅ `OrderEntryRequested`, `OrderEntryConfirmed`, `OrderSendRequested`, `OrderStatusChanged`, `OrderItemStatusChanged`; entry events carry immutable snapshots, with a test. No `From`, no `OrderCreated`: accepted; docs amended |
| Public-API rule | ✅ every public member of the contracts, read-only views and events uses only Core, Orders and System types (read by hand) |
| `IContentDatabase` | ✅ minimal `TryGetKind(string, out ItemKind)`; `ItemDefinition` has no numeric data |
| Isolation | ✅ no asmdef, manifest, `ProjectSettings`, `Docs` or `Automation` change; no production implementation |
| Metadata | ✅ every new file has a `.meta`, no duplicate GUIDs, no asset or folder in `Assets` without one |
| QA-000 | ✅ PASS (checked on `e7c1515`; later commits change only the interface comment and two test files) |

**Evidence.** I compiled the contract assemblies and ran `OrderContractTests` outside Unity (.NET 8, NUnitLite): **17 of 17 pass** at `0802648`. The author reports Unity validation PASS for `e7c1515` (compile, EditMode 84, PlayMode 20); **the Unity result for the final head is still pending**.

## What the later commits fixed (verified with deliberately wrong fakes)

| Violator | `e7c1515` | `56d8bfb` | `0802648` |
|---|---|---|---|
| C — `MarkReady` drops `BoundItem` | undetected | caught | **caught** |
| B — `MarkReady` publishes inside the shelf transaction | undetected | undetected | **caught** (mandatory `readEventCount`; plus a test that publishes through a real `EventBus`) |
| A1 — a rejection latches and a *repeated* rejection returns the wrong reason | undetected | undetected | **caught** (the rejection is now repeated twice) |
| A2 — a rejection latches in private state and only a *later success* is poisoned | undetected | undetected | caught **only** if the rejection and the ready assertion run on the **same instance** (see M1) |

Also confirmed: `readState` and `readEventCount` are now **mandatory** (the assertions throw `ArgumentNullException` when omitted, with a test), which removes the "optional hook can be skipped" loophole.

## Conditions before PR #8 merges (comment- and test-only; no interface change)

- **M1 — Fixture usage rule and the success-only-latch fake.** The fixture's own tests use separate instances for the rejection and ready assertions, which cannot see violator A2. State in the fixture's summary (currently "run this fixture with a finished, not-yet-Ready item") that implementations run `AssertUnfinishedTransition`, then finish the item, then `AssertReadyTransition` on the **same instance**, and add a malicious-fake test that proves it (rule now in `ORDER_SYSTEM.md` §6.4).
- **M2 — Finish the rule on the frozen interfaces.** `IPreparedItem.MarkReady` now carries it. Add the matching comment to `IReadyShelfPlacement.PlaceReady`, `IReadyShelfPickup.PickUp` and `IOrderService.Fail`: validate, commit, then publish after the last mutation; a failing observer never changes the result. (PR #6 is not merged, so the contract must describe itself.)
- **M3 — Rebase onto `develop` and pass Unity validation of the final head** (compile, EditMode, PlayMode, QA-000), including ARCH-001, which matches assembly names, under Unity's runtime.

## Before the drink-preparation branch forks

- **F1 — Shareable test doubles.** `PreparedFixture`, `FaultyPrepared` and `TicketFixture` are still `private` nested classes of `OrderContractTests`. Move them to a shared public location (for example `Tests/EditMode/Orders/Doubles/`), plus a fake `IStallTicketQueue` (claim, release, `IsBound`, FIFO, stale-ref rejection), a stub `IReadyShelfPlacement` with settable results and a `RecordingEventBus`. A follow-up PR that merges first is fine; the orders branch does not need it.

## Non-blocking

1. **ARCH-001 depth.** It checks only five interfaces' direct signatures; recurse into `IReadOnlyOrder`, `IReadOnlyOrderItem` and the event structs. (The compiler already enforces the rule for `Stall` and `Lobby`.)
2. **Test ids.** `TC_ORDER_009` is the stale-binding test here; in the docs it is TC-ORDER-011 (TC-ORDER-009 is "discard ruined cake") and the prepared contract is TC-READY-002.
3. **`default(ItemRequest)`** bypasses its constructor (null id, quantity 0); `Enter` must reject it (implementation test).
4. **Parked with other branches.** `DrinkRecipe` (shake/wipe durations) is frozen by the drink-preparation branch; the `ContentDatabase` implementation and the `order.unknown_item` reason key (new; `Enter` when `TryGetKind` fails) belong to the orders branch.

## Event atomicity — the proposed interpretation is **valid**, with four refinements

Proposal: *`MarkReady` mutates only; the Ready notification is deferred until the shelf's `OrderStatusChanged` after full commit; no method publishes externally while the shelf transaction commits.*

**Valid.** If `MarkReady()` published synchronously, observers would run mid-transaction and see the preparation `Ready` while its `OrderItem` is still `InPreparation`, the slot is free and the order is not Ready, and could call back into `CanPlace` / `Occupied` against that half-state.

Refinements (written into `ORDER_SYSTEM.md` §6.4):
1. **The trigger is `OrderItemStatusChanged`, not `OrderStatusChanged`.** The order-level event fires only when the *last* item is placed; in an order with a drink and a cake the drink's Ready would be announced late. Per-item reactions key off `OrderItemStatusChanged` and the bag's `PreparationId`.
2. **`Drinks` gets no Ready or Delivered step event.** `DrinkStepCompleted` is for steps D2–D7, published by the adapter after the step. Ready and Delivered are order facts announced by Orders events; this avoids two competing "Ready" signals and keeps `Drinks` out of the transaction.
3. **"No method publishes" means the participants.** The transaction owner (shelf / order service) publishes once, after the last mutation, from an outbox: item events by ascending `OrderItemId`, then the order event. While a listener runs, `Get`, `NextReadyOrder`, `Occupied`, `IsBound` and `CanPlace` report the committed state; listener re-entrancy is queued after the current flush.
4. **Observer faults must not alter the result.** `EventBus.Publish` propagates a throwing listener to its caller (a test pins this). With "publish last" alone, `PlaceReady` would throw *after* committing, `ReadyCounterPoint` would skip the hand release, and the item would be Ready on the shelf and still in the player's hand. The owner therefore catches around its flush, keeps delivering, reports the fault to an injected sink and returns the committed result (TC-READY-007). The hand release is a second, post-commit step in the adapter.

## Merge readiness

**Interfaces: frozen and approved at `0802648`.** The PR itself is not merge-ready until M1–M3 are done; I will re-check only the follow-up commit. F1 gates the drink-preparation fork, not the orders fork.

---
_Generated by [Claude Code](https://claude.ai/code)_
