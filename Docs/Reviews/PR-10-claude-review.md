# PR #10 — Claude Code independent review (Orders services: order service, ticket queue, ready shelf)

| | |
|---|---|
| PR | [#10 DRINK-WAVE Orders services](https://github.com/2312741-sudo/game/pull/10) |
| Head reviewed | `3fd62e8` (runtime/test commit `d44c487`; `3fd62e8` adds only `QA/Reports/DRINK-WAVE-orders.md`) |
| Base | `develop` `01e50b4` (PR #8 frozen contracts). Merge-base equals `develop`; no rebase needed |
| Reviewer | Claude Code (Technical Lead / Architect), independent of the implementation |
| Date | 2026-10-07 |
| Verdict | **APPROVED** — no blocking findings. Merge stays gated on the real-service integration tests and the fresh-checkout run |

## Scope inspected

`Scripts/Orders/{Order,OrderItem,OrderService,StallTicketQueue,ReadyShelf}.cs`, `Scripts/Core/Content/ContentDatabase.cs`, `Tests/EditMode/Orders/{OrderGameplayTests,OrderTransactionTests}.cs`, `QA/Reports/DRINK-WAVE-orders.md`, and the 8 new `.meta` files. No contract, asmdef, scene, prefab or doc file changes (verified by diff): `IOrderService`, `IStallTicketQueue`, `IReadyShelf*` and the event structs are untouched.

## Verified independently

| Check | Result |
|---|---|
| Diff scope vs `develop` | 17 files, all under `Orders`, `Core/Content`, the Orders tests and the QA report; every new file has a `.meta`; no `UnityEngine` use in `Orders` |
| Pure C# domain | `OrderService`, `ReadyShelf`, `StallTicketQueue` are plain C#; collaborators injected (`IGameClock`, `IEventBus`, `IIdGenerator`, `IContentDatabase`, fault sink); no static state |
| Out-of-Unity harness | .NET 8 + NUnitLite, managed stand-ins for the few `UnityEngine` types; compiled `Core` + `Content` + `Orders` + all `Tests/EditMode/Orders` sources: 0 errors. **81 of 82 pass**; the one failure (`ARCH001_…`) is a harness artifact because Core and Orders share one assembly there. The `Is.Not.AllocatingGCMemory` test passes with a real `GC.GetAllocatedBytesForCurrentThread` check |
| Mutation probes (each applied to the PR source, test run, then reverted) | Killed (tests fail): Fail never clears shelf slots; Fail clears slots only after failure observers; observer faults propagate out of the flush; ticket tie-break reversed; Fail keeps the ticket; `MarkReady` skipped; `PlaceReady` publishes before the slot is occupied; pickup publishes before slots are cleared. **Survived:** no re-entrancy queue (`if (_flushing) return;` removed); `Release` accepting a `Ready` item; removal of the `orderId != NextReadyOrder` pickup guard — see N1–N3 |

## Conformance with the amended docs (ORDER_SYSTEM §3–§6.4)

| Rule | Source evidence | Verdict |
|---|---|---|
| T1 guards: valid origin/customer, non-empty known items, point not occupied | `OrderService.RequestService` | ✅ (stricter: also rejects unknown item ids at request time) |
| T2 `BeginTaking`: Lobby actor valid, status `WaitingForLobby`, origin equals the order's point | `BeginTaking` | ✅ |
| T3 `Enter`: known items, per-kind quantity ≤ shelf capacity (`order.too_many_for_shelf`), multiset equals `RequestedItems` (DEC-012), validate before mutate | `Enter`, `ValidateItems`, `MatchesRequest` | ✅ |
| T4 `SendToStall`: FIFO by `SentAt`, ties by ascending `OrderId`; stall sees only sent orders (GT-004) | `SendToStall`, `ComesBefore`, `StallTicketQueue` | ✅ |
| T5 first claim moves order to `InPreparation`; oldest pending item of the kind; failed orders skipped; stale/duplicate preparation rejected | `Claim`, `ClaimNext`, `HasPreparation` | ✅ |
| `Release` → item `Pending`, order stays `InPreparation`; stale ref → `stall.ticket.not_bound` | `Release` | ✅ (Ready-item guard untested: N2) |
| §6.2 placement: validate (`CanPlace` reasons in the documented priority) → `MarkReady` first → order commit (throws on invariant violation) → occupy slot → publish last | `CanPlace`, `PlaceReady`, `CommitReady`, `PublishReady` | ✅ |
| `CanPlace` and `HasPending` / `IsBound` / `NextReadyOrder` pure and allocation-free | source reads + allocation test | ✅ |
| §6.3 pickup: only a `Ready` order, whole order, T7 commit, all slots cleared, snapshot ascending `OrderItemId`, event last; no `PickUp` on `IOrderService` | `ReadyShelf.PickUp`, `OrderService.PickUp` (internal) | ✅ |
| §6.4 (1) participants publish nothing | queue/order/shelf bookkeeping only enqueue into the outbox; no `Events.Publish` outside `FlushOutbox` | ✅ |
| §6.4 (2) one outbox flushed after the last mutation; item events by ascending `OrderItemId`, then the order event | `Enqueue` / `FlushOutbox`, `PublishReady` | ✅ |
| §6.4 (3) per-item Ready is `OrderItemStatusChanged(Ready)`; order Ready only after the last item | `PublishReady` | ✅ |
| §6.4 (4) listener fault is a post-commit fault: committed result returned, remaining events delivered, fault reaches injected sink | `FlushOutbox` catch + sink (and sink faults captured) | ✅ |
| §6.4 (5) re-entrancy: a listener's mutation opens a new transaction whose events follow FIFO | `_flushing` guard + single FIFO queue | ✅ (test pins only the coarse order: N1) |
| §3.3 `Fail`: unbind preparations, return items to `Pending`, remove ticket, clear the order's shelf slots **before** failure observers | `Fail` | ✅ |
| TC-ORDER-003/005/006/007/008/010/011/012, TC-READY-001/003/005/006/007/008/009 | `OrderGameplayTests`, `OrderTransactionTests`, `OrderContractTests` | ✅ present (TC-READY-002 is the shared fixture in `OrderContractTests`; no preparation implementation lives here) |
| Public-API rule | `OrderService` / `ReadyShelf` / `StallTicketQueue` are composition-root classes; `Orders` already references `Content`; the five frozen interfaces are unchanged | ✅ |

## Blocking findings

None.

## Non-blocking findings

- **N1 — the re-entrancy test cannot see an interleaved flush.** Removing `if (_flushing) { return; }` leaves all 82 tests green. `TC_READY_009` registers one listener per event type, so a nested flush produces the same global sequence. Add a second listener on `OrderItemStatusChanged(Ready)` and assert it still sees `Ready` before any `Pending`/`Failed` from the nested `Fail`. Do this before the shelf is relied on by other adapters.
- **N2 — `Release` of a `Ready` item is not tested.** The guard `item.Status != InPreparation` is correct, but removing it passes every test; the effect would be a `Pending` item whose slot is still occupied. Add a negative test.
- **N3 — the `orderId != NextReadyOrder` pickup guard is unreachable in the slice and untested.** With one slot per kind a second Ready order cannot coexist. Either test it with a larger capacity or document that PickUp is FIFO-guarded.
- **N4 — reason keys not in the docs.** `ready.quality.invalid`, `ready.no_complete_order`, `order.transition.invalid`, `order.point.wrong`, `order.point.occupied`, `order.actor.invalid`, `order.origin.invalid`, `order.items.empty|invalid|request_mismatch`, `order.failure.invalid`, `stall.ticket.preparation_invalid|preparation_bound|kind_invalid`. Add them to ORDER_SYSTEM (I will do this in the PR #6 docs), and keep the localization table in step: **PR #12's table uses `order.invalid_transition`, `order.wrong_point`, `order.not_found`, which the real service does not emit.** Align one side before integration.
- **N5 — orders never leave `Active` except through `Fail`.** `Complete` / `Deliver` are deferred (CX-025), so a table or vehicle point stays occupied after pickup. This is the known scope boundary; record it as an open item so the drink-wave integration test does not reuse a point.
- **N6 — observer faults accumulate** in `ObserverFaults` without bound. Fine for tests; cap or drain it once a sink is wired in the game.
- **N7 — live views.** `Active` and `Tickets` return live read-only wrappers; a caller enumerating them while a listener calls `Fail` can hit `InvalidOperationException`. Document it or return snapshots at the UI boundary.
- **N8 — invariant throws after `MarkReady`.** `PlaceReady` can throw from `CommitReady` after the item is already `Ready`. This is exactly what §6.2 step 3 prescribes (an invariant violation, never partial success); the caller must treat it as fatal.

## Taken from the author's report (not re-run: no Unity editor here)

Unity 6000.6.0f1 compile with 0 diagnostics, EditMode 173/173, PlayMode 25/25, QA-000 PASS on a clean checkout (`QA/Reports/DRINK-WAVE-orders.md`).

## Merge readiness and conditions

**Safe to squash-merge after** (a) the real-service integration tests with the drink-preparation (PR #11) and Ready-adapter (PR #13) heads, (b) the full fresh-checkout run, and (c) N1 added or explicitly deferred with an owner. N2–N8 may ride a follow-up. The PR does not touch contracts or docs; the PR #6 docs carry the evidence and the N4 key additions.

---
_Generated by [Claude Code](https://claude.ai/code)_
