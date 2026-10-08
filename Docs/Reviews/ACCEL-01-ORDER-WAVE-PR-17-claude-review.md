# ACCEL-01 ORDER-WAVE — PR #17 Claude Code independent review

| | |
|---|---|
| PR | [#17 ACCEL-01 order delivery and completion](https://github.com/2312741-sudo/game/pull/17) |
| Head inspected | `63f2cdb276e69850fa3d3ab007023f8f9ff9bba7` (`Assets`, `Packages`, `ProjectSettings` identical to runtime/test commit `b4efced`; the last commit adds only QA reports) |
| Base | `develop` `d2303cc` (PR #15 merged); merge-base equals `develop`; `Packages` and `ProjectSettings` unchanged |
| Reviewer | Claude Code (Technical Lead / Architect); review only, no runtime/test edits, no merge, no other branch touched |
| Date | 2026-10-09 (local) |
| Verdict | **CHANGES REQUESTED** — one blocker (B1: a carried bundle can soft-lock the hands when its lifecycle binding is not wired, which is the default of the current API). Everything else in the lane is approved |

Docs used: `ARCHITECTURE.md`, `INTERACTION_SYSTEM.md`, `ACCEL-01_CONTRACTS.md` (as in the PR) and `ORDER_SYSTEM.md` §3, §6.4, §8. **`ORDER_LIFECYCLE.md` does not exist in the repository**; `ORDER_SYSTEM.md` §3 is the lifecycle specification and I added a delivery section (§3.2.1) to it on the PR #6 branch.

## Scope

32 files, runtime: `Orders/{OrderService, Order, StallTicketQueue}` (+ contract files `IOrderDelivery`, `IOrderDeliveryInfo`, `IOrderItemCatalog`, `DeliveryTarget`, `DeliveryRejected`, `OrderDeliveryInfo`, `Core/IPreparationFeedback`), `Lobby/{OrderPoint, ServedOrder, ReadyOrderPickupPoint}`; tests `OrderDeliveryTests` (11), `DeliveryAdapterTests` (15 + helpers), `DeliveryPresentationTests` (3, PlayMode); QA reports. No existing intake, queue, Ready or held-item interface was changed.

## Verified independently (out-of-Unity .NET 8 harness with a managed `UnityEngine` stand-in; no Unity editor here)

- The real `Core`, `Orders` (services), `Lobby` sources and the two EditMode delivery test files compile with 0 errors and **41/41 pass**, matching the author's "delivery-focused EditMode 41/41". PlayMode (3) and the full 325/38 results are the author's.
- Eight mutations of the PR source, each run against those 41 tests:

| Mutation | Result |
|---|---|
| wrong attempts not counted | killed (3 tests) |
| customer check removed from `Deliver` | killed |
| `Complete` frees the point after publication instead of before | killed |
| quality committed after the `Delivered` publication | killed |
| mean truncated instead of rounded | killed |
| `ServedOrder` does not retire on `Failed` | killed |
| `Complete` retry disabled | killed |
| adapter does not retire the committed bundle | **survives** — equivalent: `ServedOrder` already retires itself on the `Delivered` event; belt and braces |

- Two probes of my own against the real services (not in the PR): the soft-lock in B1, and the rounding behaviour in N1.

## Requested checks

| Check | Verdict | Evidence |
|---|---|---|
| Identity = origin + customer | ✅ | `Deliver` compares `OrderOrigin` and `CustomerId`; the point builds the target from its own live order, so a bundle for another order is rejected at its real order; wrong origin and wrong customer each tested; mutation killed |
| Invalid actor / default target | ✅ | `actor.Value <= 0` → `order.actor.invalid`, `!target.IsValid` → `order.delivery.invalid_target`, unknown or not-`PickedUpByLobby` order → `order.transition.invalid`; none mutate or publish (tests); free point blocks with `order.delivery.no_customer` without an attempt |
| Failed carried bundle retirement | ⚠️ ✅ only when bound | `ServedOrder` subscribes to `OrderStatusChanged`, polls in `LateUpdate`, releases only its own held identity and retires visuals; replacement items adopted during release survive. **Only if `BindDelivery` ran** — see B1 |
| Observer / reentrant atomicity | ✅ | all events via the §6.4 outbox; tests for throwing observers, a reentrant `Complete`, a rejection observer that delivers and completes, a fault before the bundle's own listener, a throwing hand observer, a new customer ordering from inside the completion callback; `Delivered` is always observed before `Completed`; occupancy freed before the `Completed` publication (mutation killed) |
| Rounded mean quality | ✅ with N1 | sum of item qualities in `long`, committed before publication; 0 until `Delivered` |
| Snapshot agreement (C4) | ✅ | `Order` implements `IOrderDeliveryInfo` over the same fields `TryGetInfo` reads, so they agree by construction; the test asserts it |
| Delivery then `Complete` | ✅ | the adapter calls `Complete` right after a committed `Deliver`, treats an already `Completed` order (reentrant observer) as success, and decides "committed" from the order status, not only the returned `Result` |
| C3 retry-only `Complete` | ✅ | after a transient failure the order stays `Delivered`, the bundle is already retired, and the next interaction (`order.point.complete`, empty hands) calls `Complete` only; no preparation, shelf or `Deliver` call is touched (test + killed mutation) |

## Blocker

**B1 — an unbound `ServedOrder` soft-locks the hands, and the unbound path is the API default.** `ReadyOrderPickupPoint.Initialize(shelf, orders = null, events = null)` only binds the bundle's lifecycle subscription when `orders`/`events` are supplied. The merged composition (`DrinkWaveBootstrap`, PR #15) calls `pickup.Initialize(Shelf)` with one argument. Reproduced against the real services: with the one-argument call, after a whole-order pickup `OrderService.Fail(order)` leaves the `ServedOrder` in the player's hands; the table's query then says `Blocked / order.delivery.no_customer`, every other point refuses (`order.transition.invalid`), pickup and take-order need empty hands, and nothing retires the bundle (the retire-on-`Failed` branch in `OrderPoint.Deliver` is unreachable because `Query` blocks first). With the three-argument call the hand is released at once. The contract requires a failed carried order to be retired, so the safe path must not be opt-in. Fix inside the lane (no contract change): make `OrderPoint.Execute` retire a held `ServedOrder` whose order is `Failed`/terminal or unknown instead of only publishing `ActionBlocked` (self-healing for unbound bundles), **and** either require `orders`/`events` in `ReadyOrderPickupPoint.Initialize` or throw when they are half-supplied and the bundle cannot be bound; add a regression using the one-argument call. The integration wave must also update the bootstrap to pass the service and bus.

## Non-blocking findings

1. **N1 — rounding mode is unspecified and unpinned.** `Math.Round` rounds half to even: the mean of 97 and 98 gives 98, of 96 and 97 gives 96, of 94 and 95 gives 94 (whereas "round half up" gives 97 and 95 for the last two). The only test, 75 and 80 (77.5 → 78), passes under either mode. Choose `MidpointRounding.AwayFromZero` (the usual meaning of "round the mean") or document half-even, and pin it with a .5 case that differs.
2. **N2 — capability by downcast.** `OrderPoint.Configure` takes an `IOrderService` and sets `_delivery = orders as IOrderDelivery`, so delivery is enabled by the concrete service type rather than by injection (the contract says the root injects `IOrderDelivery`). A fake that lacks it is visible only as `order.delivery.not_configured`. Add an explicit `IOrderDelivery` parameter or setter at integration.
3. **N3 — localization keys to cover (root's N1 pass).** `order.point.deliver`, `order.point.complete`, `order.delivery.no_customer`, `order.delivery.not_configured`, `order.delivery.invalid_target`, `order.delivery.wrong_target`, `order.status.PickedUpByLobby`, `hud.next.delivery`, `ready.no_complete_order`. Listed in `ORDER_SYSTEM.md` §6.5.
4. **N4 — `BindDelivery` can throw before the pickup's `try`.** If the order disappeared between `NextReadyOrder` and binding, the reservation object leaks. Unlikely (the shelf just reported the order); move the call inside the guarded block.
5. **N5 — completion retry needs empty hands and a manual interaction.** Acceptable for a transient failure; note it as the intended behaviour so UI feedback (`order.point.complete`) is visible to the player.
6. **N6 — `DeliveryAttempts` is an unbounded `int`** and wrong attempts have no cooldown; fine for the slice, relevant for the later economy.

## Taken from the author's report (not re-run)

Unity 6000.6.0f1: compile 0 diagnostics, EditMode 325/325, PlayMode 38/38, QA-000 clean checkout PASS (`QA/Reports/ACCEL-01-orders-validation.md`, `QA-000-ACCEL-01-orders.md`). Production input paths, Ready composition and the fresh-checkout run remain integration-wave gates, as the PR states.

## Merge readiness

Not mergeable until B1 is fixed and the regression exists; I will re-check only that delta. B4 contract tests and the C2 UI reflection test gate the root integration wave, not this lane. Docs updated on the PR #6 branch: `ORDER_SYSTEM.md` §3.2 (`DeliveryRejected` field names), new §3.2.1 (delivery and completion rules) and §6.5 (delivery keys).

---
_Generated by [Claude Code](https://claude.ai/code)_
