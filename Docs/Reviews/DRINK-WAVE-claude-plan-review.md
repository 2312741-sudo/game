# Drink gameplay wave — Claude Code plan and boundary-contract review

| | |
|---|---|
| Subject | Codex's implementation plan for the user-authorized drink gameplay wave (branches: orders, held-use, drink preparation, integration) |
| Plan source | Message from the Product Owner's session, 2026-10-07 (no separate plan document existed) |
| Baseline | `develop` @ `36db3ce` (PR #3 merged); PR #5 head `f83f366` approved, merge pending |
| Reviewer | Claude Code (Technical Lead / Architect) |
| Date | 2026-10-07 |
| Gate | REV-003 (drink spec) / REV-002 (order API) for this wave |
| **Verdict** | **CHANGES REQUESTED** — the architecture and branch split are approved; the contracts need the corrections below *before the branches fork* |

Reviewed against `Docs/ARCHITECTURE.md` (§1 ground truth, §4 module map, §6 events, §7 content),
`Docs/ORDER_SYSTEM.md`, `Docs/INTERACTION_SYSTEM.md`, `Docs/DRINK_WORKFLOW.md`,
`Docs/CODING_CONVENTIONS.md` and `Docs/PROJECT_TASK_PLAN.md`, and against the code on `develop`.

> The corrections are cheap contract-level edits, not a redesign. Once Codex confirms them,
> Claude Code will amend `ORDER_SYSTEM.md` §3/§5/§6 and `DRINK_WORKFLOW.md` §2 in a follow-up docs
> commit so the canonical docs match what is built. Until then this document is the binding
> review outcome for the wave.

## 1. Facts verified on `develop`

| Fact | Consequence for the plan |
|---|---|
| `Orders` and `Lobby` contain only `AssemblyInfo.cs` and an asmdef. | "Existing Orders/Lobby are scaffolds" is correct; the canonical orders work is a true prerequisite. |
| Core already has typed `OrderId`, `OrderItemId`, **`PreparationId`**, `TableId`, `VehicleId`, `CustomerId`, `Result`, `Result<T>`, `Availability`, `IIdGenerator`, `ManualClock`. | Contracts should use these types, not raw `int` (see C2). |
| `ItemKind`, `ItemDefinition`, `DrinkRecipe`, `IContentDatabase` do **not** exist; `Content` holds only `BalanceConfig`. | `ItemKind` in `Content` is right, but the rest of the Content subset has no owner (see C4). |
| asmdef references: `Orders`→Core,Content; `Lobby`→Core,Interaction,Orders; `Stall`→Core,Interaction,Orders; `Drinks`→Core,Content,Interaction,Orders. | The proposed APIs fit the graph with **no new references**. `Orders` cannot see `Interaction`, so `ActorRole` is not visible to the order domain (see C3). |
| `ActorRole` is `TramChanh.Interaction.ActorRole`; `ActorRef` is in Core. | The role check can only be an adapter check. |
| `PlayerInputReader` does not read `UseHeld`, although the action (F / right mouse) already exists in `TramChanh.inputactions`. `InteractionActionDriver.Begin` accepts only `IInteractable`. `IHeldItemAction` exists with `QueryUse` / `ExecuteUse`. | The held-use branch needs no new input asset and no change to the approved interfaces (see C5). |

## 2. The two questions asked

### 2.1 `Result MarkReady()` on `IPreparedItem` — **APPROVED, with the protocol in C1**

The docs already require it: `DRINK_WORKFLOW.md` D8 and the `DrinkPreparation` sketch have
`MarkReady()` "called by ReadyShelf", `CAKE_WORKFLOW.md` C10 has the same for cakes, and
`ORDER_SYSTEM.md` §6 step 4 says "the preparation's own state → Ready". Only the `IPreparedItem`
listing in `ORDER_SYSTEM.md` §6 omitted it, so this is the doc catching up, not a new concept.
It is a valid way for `ReadyShelf` (plain C# in `Orders`) to change a Drinks/Cakes object without
referencing either module. What was missing is the atomicity rule; "atomically" needs a defined order
of operations (C1).

### 2.2 `Held` ↔ documented `PickedUp` — **mapping accepted for PR #5 only; rename in the drink-preparation branch (C6)**

PR #5 is a pickup-only slice and records the mapping, so it can merge as is. For the wave the mapping
should not survive: `TeaBagState` is listed with `PickedUp` in `DRINK_WORKFLOW.md` §2 and in both source
documents, and `Held` is a misleading workflow name — the bag is held through `Opened … Wiped` too, so
`Held` says nothing about "picked up, not yet opened". Renaming costs one enum member plus the pinned
test now, and the state is not serialized anywhere.

## 3. Approved as proposed

- Branch split and order: **orders → (held-use in parallel) → drink preparation → integration**.
- `ItemKind` in `Content` (`Orders` and `Drinks` may reference `Content`).
- `OrderItemRef {OrderId, OrderItemId, PreparationId, IsValid}` — keep `PreparationId` in the ref: it
  lets `IsBound` detect a **stale** ref after a release and re-claim by another preparation (value
  equality over all three fields).
- `IStallTicketQueue` additions (`IsBound`) and `IReadyShelf.Occupied(ItemKind)` as read-only helpers.
- `RequestService → BeginTaking → Enter → SendToStall`, role check in the Lobby adapter, separate
  *Enter* and *Send* confirmations (DEC-003).
- The strict chain `PickedUp → Opened → CoconutJellyAdded → LemonJellyAdded → IceAdded → Shaken →
  Wiped → Ready`; pre-portioned tea; no measuring; Lobby → Order → Stall; Unity 6000.6.0f1 and the
  current package pins.

## 4. Required corrections

Each correction states what must change and how it is verified.

### Before the branches fork (contract freeze)

**C1 — Define `PlaceReady` as validate-then-commit, with `MarkReady` first.**
`PlaceReady(item)` must run in this order:
1. `CanPlace(item)` — **pure and allocation-free** (`ReadyCounterPoint.Query` calls it every frame);
   returns `Availability`. Reason keys, in priority order: `ready.not_finished`, `ready.no_order`
   (unbound or `!queue.IsBound(ref)`), `ready.already_ready`, `ready.slot_full`.
2. `item.MarkReady()` **first**. It mutates only the item itself and only on success, so a failure
   aborts with nothing changed.
3. Order-side commit (`OrderItem` `InPreparation → Ready`; T6 when the last item is Ready).
   Already validated in step 1, so a failure here is an invariant violation: **throw** (never return
   partial success).
4. Occupy the slot, then publish `OrderItemStatusChanged` / `OrderStatusChanged` **after** all state
   is consistent.

`IsFinished` means "final step done" and stays `true` after Ready. Contract: `IsFinished` and not yet
ready ⇒ `MarkReady()` succeeds. Verified by a shared `PreparedItemContractTests` fixture run against
every `IPreparedItem` implementation, plus a failure-injection test (a fake whose `MarkReady` fails ⇒
shelf and order unchanged).

**C2 — Typed ids, `Release` result, claim semantics.**
- `ClaimNext(ItemKind, PreparationId)` — not `int`; the typed id already exists in Core.
- `Release` returns `Result`; releasing a stale or unknown ref fails with `stall.ticket.not_bound`.
- `IsBound(ref)` is true only when **all three** fields match the current claim.
- After `Release`, the item returns to `Pending` and the order **stays** `InPreparation` (the docs have
  no backward order transition). `Fail(order)` releases every binding (TC-ORDER-008).
- FIFO key: `SentToStall` time, **ties broken by ascending `OrderId`** (a `ManualClock` gives equal
  timestamps); `ClaimNext` skips `Failed` orders and takes items in ascending `OrderItemId`.
- `PreparationId` is assigned when the bag object is created (stable, unbound until D1), via
  `IIdGenerator`.
- D1 orchestration (drink branch): run every non-ticket guard first, so a blocked pickup never claims;
  then `ClaimNext`; then the slot pickup; if the slot refuses, call `Release`. A single
  `TryTakeBag`-style use case in the drink domain owns this, not a callback (see PR #5 architecture
  concern 1).

**C3 — `BeginTaking` signature and where the role is enforced.**
- Use `BeginTaking(OrderId id, ActorRef actor, OrderOrigin at)`. The domain checks that `at` equals
  `order.Origin` and the status; reuse `OrderOrigin` instead of a new `point` type.
- `ActorRole` lives in `Interaction` and `Orders` cannot reference it, so `ORDER_SYSTEM.md` T2/T7/T8
  guard text "`actor.Role == Lobby`" must read "the **adapter** checks `ActorRole.Lobby`; the domain
  checks actor, origin and status". GT-004 stays enforced because the stall side has no creation path.
- **Interface segregation** (new acceptance criterion): `Lobby` receives `IOrderService`;
  `Drinks`/`Cakes` receive only `IStallTicketQueue`; `Stall` receives only `IReadyShelf`. No stall-side
  type may hold `IOrderService`. A GT-004 test enforces both the reflection check on `IOrderService`
  and this injection rule.

**C4 — Give the minimal Content subset an owner and a branch before drink preparation.**
`Enter` (T3) must verify "every item exists in content", and the shake/wipe durations need a home
(`DRINK_WORKFLOW.md` §6). Neither `ItemDefinition`, `DrinkRecipe` nor `IContentDatabase` exists. Land
a minimal subset first — `ItemKind`, `ItemDefinition`, `DrinkRecipe` (`ShakeHoldSeconds`,
`WipeHoldSeconds`, jelly/ice portions as visual-only data, all `[Tbd("DEC-007")]`),
`IContentDatabase` lookup — in the orders branch or a tiny branch merged ahead of drink preparation.
Hold durations must never live in `BalanceConfig` or in code. The drink's real name is still pending
(DEC-008): use an explicit placeholder display name; do not invent one.

**C7 — Decide the shelf's pickup side.** With one slot per kind (DEC-014) and no pickup, the second
order is blocked by `ready.slot_full` forever. Either include the pickup side now (recommended): a
domain release/take for one Ready order plus `OrderService.PickUp` (T7), even if the Lobby UI and
delivery (CX-025) follow later; or state explicitly that validation covers one order per session. The
shelf API must not need a breaking change when T7 lands.

**C9 — Step 0 prerequisites.**
1. **Contract-first commit.** Land the frozen interfaces, `ItemKind`, `OrderItemRef`, fakes and the
   contract tests (no implementation) on `develop` first, and have Claude Code review it. The branches
   fork from that commit; "drink preparation after the orders contract" is then a real dependency.
2. **Merge PR #5 first.** Drink preparation rebases on it.
3. **Commit the `.meta` files** for the existing un-metaed asmdefs, `AssemblyInfo.cs` files and
   folders (generated by Unity 6000.6.0f1). Four parallel branches that each open Unity will generate
   *different* GUIDs for the same files, producing add/add conflicts on every merge.

### Inside each branch

**C5 — Held-use design.**
- Do not change the approved `IInteractable` / `IHeldItemAction` signatures.
- Drive held actions with the **same** `InteractionActionDriver` through a small internal adapter (or a
  shared action-target interface). One driver instance makes `Interact` (E) and `UseHeld` (F) mutually
  exclusive.
- A held *Hold* action targets the held item, so it is **not** focus-locked to the object being looked
  at. Cancel on release, held item changed, or availability becoming Blocked.
- **Pause freezes** held progress (TC-INT-006); today pause cancels holds, so close that debt here.
- `PlayerInputReader` exposes `UseHeld` pressed/released from the existing action; the input asset
  stays unchanged.
- Tests: Press (open), Hold (shake), E/F exclusion, held-item-change cancel, pause freeze,
  zero-allocation `QueryUse`.

**C6 — Rename `Held` → `PickedUp`** as the first commit of the drink-preparation branch. Replace the
pinned `GT_003` enum test with a name-based check ("no `Measure*`/`Pour*` member").
`DRINK_WORKFLOW.md` needs no change; code now matches it.

**C8 — A customer source that uses the public path.** Intake needs something to call
`RequestService`. Provide a scripted requester (placeholder `SliceCustomerController`, or DevTools)
that uses **only** `RequestService` and covers both origins (`TableOrderPoint` and
`VehicleOrderPoint` on `PF_Placeholder_VehiclePoint`; TC-ORDER-001/002).

**C10 — Retire the PR #5 bypass.** When `IStallTicketQueue` lands, D1 must call
`HasPending`/`ClaimNext`. Delete `_allowUnboundPickupForTest` from `TeaRackController`; if a
ticket-free test scene is still wanted, implement it as a `TramChanh.DevTools` ticket-gate
implementation. Until then the guard in `CODING_CONVENTIONS.md` §8 applies. TC-DRINK-008 runs against
the real queue.

**C11 — Scenes and shared files.** No more scene copies. The integration branch exclusively owns scenes
and prefabs; the others commit none. Compose a base scene additively. Conflict hotspots and owners:

| Shared file | Owner |
|---|---|
| `Docs/PROJECT_TASK_PLAN.md` status board | integration branch only |
| `BalanceConfig.cs`, `PlayerInteractionTestBootstrap.cs` | integration branch; other branches add data via their own ScriptableObjects |
| `Packages/*`, `ProjectSettings/*` | frozen |
| asmdefs | frozen (C12) |

**C12 — No asmdef reference changes.** Every dependency the plan needs is already allowed
(`ARCHITECTURE.md` §4). QA-000's graph check must stay green; any new reference is a proposal to
Claude Code first.

## 5. Corrected contract sketch (for the contract-first commit)

```csharp
// TramChanh.Content
public enum ItemKind { Drink, Cake }

// TramChanh.Orders
public readonly struct OrderItemRef : IEquatable<OrderItemRef>
{
    public OrderId OrderId { get; }
    public OrderItemId OrderItemId { get; }
    public PreparationId PreparationId { get; }
    public bool IsValid => OrderId.IsValid && OrderItemId.IsValid && PreparationId.IsValid;
    // Equals / GetHashCode / == over all three fields
}

public interface IStallTicketQueue
{
    IReadOnlyList<OrderId> Tickets { get; }                       // FIFO: SentToStall time, tie-break OrderId
    bool HasPending(ItemKind kind);                               // pure
    Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparation);
    Result Release(OrderItemRef item);                            // stale ref => "stall.ticket.not_bound"
    bool IsBound(OrderItemRef item);                              // all three fields match the live claim
}

public interface IPreparedItem
{
    ItemKind Kind { get; }
    OrderItemRef BoundItem { get; }       // default (invalid) when unbound
    bool IsFinished { get; }              // final step done; stays true after Ready
    int Quality { get; }
    Result MarkReady();                   // mutates only on success; "ready.not_finished" / "ready.already_ready"
}

public interface IReadyShelf
{
    Availability CanPlace(IPreparedItem item);   // pure, zero-alloc
    Result PlaceReady(IPreparedItem item);       // validate -> MarkReady() -> order commit -> slot -> events (C1)
    bool Occupied(ItemKind kind);
    // + pickup/release of one Ready order (C7)
}

public interface IOrderService
{
    Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested);
    Result BeginTaking(OrderId id, ActorRef actor, OrderOrigin at);   // C3
    Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered);
    Result SendToStall(OrderId id);
    // PickUp / Deliver / Complete per ORDER_SYSTEM.md (C7)
}
```

## 6. Required tests for the wave

| Area | Tests |
|---|---|
| Orders | TC-ORDER-001…009; FIFO tie-break with two orders at the same `ManualClock` time; `ClaimNext` skips `Failed`; release then re-claim by another preparation makes the old ref stale; GT-004/GT-005/GT-006; `PreparedItemContractTests`; `PlaceReady` failure injection; events published only after commit |
| Held-use | Items listed in C5 |
| Drink | GT-007 state × action matrix (only the eight documented transitions succeed); TC-DRINK-001…009; D1 rollback (slot refuses ⇒ `Release`, queue unchanged); blocked pickup never claims; two tickets ⇒ oldest is claimed; no ticket ⇒ `stall.no_ticket.drink`; no measuring state exists |
| Integration | Scripted dine-in **and** vehicle order through Lobby → Order → Stall → Ready; no `_allowUnboundPickupForTest` anywhere; QA-000 green; EditMode and PlayMode green from a fresh checkout on the pinned editor |

## 7. Deferred (unchanged)

Cake workflow, patience, money, customers beyond the scripted requester, save/load, final art, ticket
selection by the player, `FreeWithScoring` mode.

## 8. Next step

Codex adopts C1–C4, C7 and C9 in the contract-first commit; Claude Code reviews it (REV-002 / REV-003).
On approval the four branches fork. REV-003 stays **open** until then.

---
_Generated by [Claude Code](https://claude.ai/code)_
