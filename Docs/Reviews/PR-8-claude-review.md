# PR #8 — Claude Code review of the frozen drink-wave contracts (REV-002)

| | |
|---|---|
| PR | [#8 DRINK-WAVE: freeze order and prepared-item contracts](https://github.com/2312741-sudo/game/pull/8) (draft) |
| Head reviewed | `56d8bfbd0770df361039c2341be174d078c9f3bd` |
| Review history | `e7c1515` (also covers `71a00a2`): CHANGES REQUESTED, two blockers → `56d8bfb` strengthens the shared fixture after the mutation finding: **narrowed, not cleared** (below) |
| Base | `develop` `17e21cd` (PR #7, metadata freeze, merged). The branch was not rebased: it still carries PR #7's pre-squash commit `87c9291`, byte-identical to what `develop` has (74 files, 0 differences), so a squash-merge is harmless |
| Reviewed against | PR #6 amended docs: `ORDER_SYSTEM.md`, `DRINK_WORKFLOW.md`, `ARCHITECTURE.md`, `CODING_CONVENTIONS.md`, `Reviews/DRINK-WAVE-claude-plan-review.md` |
| Reviewer | Claude Code (Technical Lead / Architect) |
| Date | 2026-10-07 |
| **Verdict** | **CHANGES REQUESTED** — four small corrections, all in test code and interface comments; **no interface design change is requested** |

## What matches the docs

| Area | Result |
|---|---|
| `ItemKind` in `TramChanh.Core`, `Drink = 0`, `Cake = 1` | ✅ test pins the values |
| `OrderItemRef` | ✅ typed ids, `default` invalid, equality over all three fields, constructor rejects invalid parts |
| `IStallTicketQueue` | ✅ `Tickets`, `HasPending`, `ClaimNext(ItemKind, PreparationId)`, `Release → Result`, `IsBound` |
| `IPreparedItem` | ✅ `Kind`, `BoundItem`, `IsFinished`, `Quality`, `Result MarkReady()` |
| Shelf split | ✅ `IReadyShelfPlacement` (`CanPlace → Availability`, `PlaceReady`, `Occupied`), `IReadyShelfPickup` (`NextReadyOrder`, `PickUp(OrderId, ActorRef)`), union `IReadyShelf` |
| `IOrderService` | ✅ no `PickUp`; `BeginTaking(id, actor, OrderOrigin point)`; `Deliver` / `Complete` not included (CX-025, additive; docs amended) |
| Events | ✅ `OrderEntryRequested`, `OrderEntryConfirmed`, `OrderSendRequested`, `OrderStatusChanged`, `OrderItemStatusChanged`; the entry events carry immutable snapshots, with a test. Differences from the earlier catalogue (no `From`, no `OrderCreated`) accepted; docs amended |
| Public-API rule | ✅ every public member of the contracts, read-only views and events uses only Core, Orders and System types (read by hand) |
| `IContentDatabase` | ✅ minimal `TryGetKind(string, out ItemKind)`; `ItemDefinition` has no numeric data |
| Isolation | ✅ no asmdef, manifest, `ProjectSettings`, `Docs` or `Automation` change; no production implementation |
| Metadata | ✅ every new file has a `.meta`, no duplicate GUIDs repo-wide, no asset or folder in `Assets` without one |
| QA-000 | ✅ PASS (checked on `e7c1515`; the later commit changes only two test files) |

**Evidence.** I compiled the contract assemblies and ran `OrderContractTests` outside Unity (.NET 8, NUnitLite): **12 of 12 pass** at `56d8bfb`. The author reports Unity validation PASS for `e7c1515` (compile, EditMode 84, PlayMode 20) and is re-running the 12-test fixture; **the Unity result for `56d8bfb` is still pending**.

## What `56d8bfb` fixed (verified)

The shared fixture now snapshots `Kind`, `BoundItem`, `Quality` and `IsFinished` across success, repeated success and rejection; adds `AssertUnfinishedDoesNotMutate` (returns `ready.not_finished`, changes nothing); takes an optional source-state probe; and proves it with four malicious fakes (failure changes the kind, failure changes source state, success changes the binding, a repeated failure changes the quality). I re-ran my own three violators against it:

| Violator | `e7c1515` | `56d8bfb` |
|---|---|---|
| C — `MarkReady` drops `BoundItem` | passed (undetected) | **caught** |
| A — a *rejected* `MarkReady` latches, so a later legitimate one fails (nothing observable) | passed (undetected) | **still undetected** |
| B — `MarkReady` publishes inside the shelf transaction | passed (undetected) | **still undetected** |

## Required corrections

### C1 — Detect a rejection that poisons a later call (violator A)

`AssertUnfinishedDoesNotMutate` only notices what is observable through `Kind`, `BoundItem`, `Quality`, `IsFinished`, or the **optional** `readState`. A latch in a private field passes. Add a **required** `Action finishFinalStep` parameter: after the no-mutation checks, call it and assert `MarkReady().IsSuccess` (and then `ready.already_ready` on a repeat). This needs no private state, so an implementation cannot skip it by omitting `readState`. Add a malicious-fake test for it.

### C2 — Enforce "`MarkReady` publishes nothing" (violator B)

Add a **required** `Func<int> publishedEventCount` to both assertions and assert it does not move across every `MarkReady()` call (the implementation test passes a recording-bus counter; the pure fixture passes `() => 0`). Add a malicious-fake test. This is the shared-fixture half of the atomicity rule in `ORDER_SYSTEM.md` §6.4 and TC-DRINK-015.

### C3 — Write the rule into the frozen interfaces

PR #6 is not merged, so the contract must describe itself. XML documentation: `IPreparedItem.MarkReady` ("mutates only this object and only on success; publishes nothing, invokes no callback, has no externally visible side effect"); `IReadyShelfPlacement.PlaceReady`, `IReadyShelfPickup.PickUp`, `IOrderService.Fail` ("validate, commit, then publish after the last mutation; a failing observer never changes the result").

### C4 — Shareable test doubles

`PreparedFixture`, `TicketFixture` and the new `FaultyPrepared` are `private` nested classes of `OrderContractTests`, so the parallel branches cannot reuse them. Move them to a shared public location (for example `Tests/EditMode/Orders/Doubles/`) and add the programmable doubles the adapters need: a fake `IStallTicketQueue` (claim, release, `IsBound`, FIFO, stale-ref rejection as the docs define), a stub `IReadyShelfPlacement` with settable results, and a `RecordingEventBus`. Faithful shelf and order-service behaviour stays with the orders branch. I will accept C4 either in this PR or in a small follow-up that merges before the drink-preparation branch forks.

## Non-blocking

1. **ARCH-001 depth.** It checks only five interfaces' direct signatures; recurse into `IReadOnlyOrder`, `IReadOnlyOrderItem` and the event structs. (The compiler already enforces the rule for `Stall` and `Lobby`.)
2. **Test ids.** `TC_ORDER_009` here is the stale-binding test; in the docs it is TC-ORDER-011 (TC-ORDER-009 is "discard ruined cake") and the prepared contract is TC-READY-002.
3. **`default(ItemRequest)`** bypasses its constructor (null id, quantity 0); `Enter` must reject it (implementation test).
4. **Rebase onto `develop`** before merging so the squash does not carry PR #7's commit.
5. **Unity validation of the head** is the merge gate, including ARCH-001 under Unity's runtime (it matches assembly names).
6. **Parked with other branches.** `DrinkRecipe` (shake/wipe durations) concerns only `Drinks` and integration, so the drink-preparation branch freezes it. The `ContentDatabase` implementation and the `order.unknown_item` reason key (new; used by `Enter` when `TryGetKind` fails) belong to the orders branch.

## Event atomicity — the proposed interpretation is **valid**, with four refinements

Proposal: *`MarkReady` mutates only; the Ready notification is deferred until the shelf's `OrderStatusChanged` after full commit; no method publishes externally while the shelf transaction commits.*

**Valid.** If `MarkReady()` published synchronously, observers would run mid-transaction and see the preparation `Ready` while its `OrderItem` is still `InPreparation`, the slot is free and the order is not Ready, and could call back into `CanPlace` / `Occupied` against that half-state.

Refinements (written into `ORDER_SYSTEM.md` §6.4):
1. **The trigger is `OrderItemStatusChanged`, not `OrderStatusChanged`.** The order-level event fires only when the *last* item is placed; in an order with a drink and a cake the drink's Ready would be announced late. Per-item reactions key off `OrderItemStatusChanged` and the bag's `PreparationId`.
2. **`Drinks` gets no Ready or Delivered step event.** `DrinkStepCompleted` is for steps D2–D7, published by the adapter after the step. Ready and Delivered are order facts announced by Orders events; this avoids two competing "Ready" signals and keeps `Drinks` out of the transaction.
3. **"No method publishes" means the participants.** The transaction owner (shelf / order service) publishes once, after the last mutation, from an outbox: item events by ascending `OrderItemId`, then the order event. While a listener runs, `Get`, `NextReadyOrder`, `Occupied`, `IsBound` and `CanPlace` report the committed state; listener re-entrancy is queued after the current flush.
4. **Observer faults must not alter the result.** `EventBus.Publish` propagates a throwing listener to its caller (a test pins this). With "publish last" alone, `PlaceReady` would throw *after* committing, `ReadyCounterPoint` would skip the hand release, and the item would be Ready on the shelf and still in the player's hand. The owner therefore catches around its flush, keeps delivering, reports the fault to an injected sink and returns the committed result (TC-READY-007). The hand release is a second, post-commit step in the adapter.

## Merge readiness

**Not yet.** Make C1–C3 (and C4 here or in a follow-up), rebase onto `develop`, and pass Unity validation of the head. Then the interfaces are approved as frozen, REV-002 closes, and the four branches may fork.

---
_Generated by [Claude Code](https://claude.ai/code)_
