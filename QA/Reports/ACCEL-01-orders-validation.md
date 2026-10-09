# ACCEL-01 — order delivery validation

Date: 2026-10-09 (Asia/Ho_Chi_Minh)

Validated runtime/test commit: `813fb5c6c4c97aa47b3e64f97eddeb3c63b92e7a`, including the legacy-scene lifecycle wiring in `c417a50`.
Original pre-rebase implementation remains preserved on `feature/ACCEL-01-orders-pre-develop-rebase-20261009`.
Unity: `6000.6.0f1`; existing package pins retained. Validation label: `accel-orders-recovery-full`.

- Compile: PASS, zero C# errors/warnings.
- Full EditMode: **332/332** passed, zero skips.
- Full PlayMode: **38/38** passed, zero skips.
- QA-000: **91/91 PASS** in a clean detached checkout of the runtime/test commit (`accel-orders-recovery-qa`).
- No unexpected test-console logs; deliberate fault-injection logs are asserted by the tests.
- Production input paths, ACCEL scene composition and fresh-checkout Unity validation remain integration-wave gates.

## Behaviors covered

Real OrderService follows canonical intake, preparation, whole-order pickup, origin/customer-matched delivery and completion for dine-in, takeaway and mixed bundles. Invalid/default delivery actor or target does not mutate. Each wrong valid target counts once and publishes one rejection. Quality/snapshot values commit before observers. Completion frees point occupancy before observers, retaining FIFO outbox fault and reentrancy behavior.

Lobby adapters retire successfully delivered or failed bundles, preserving replacement held identities and prepared children adopted during release. Lifecycle subscriptions precede pickup reservation; a late-frame check recovers when a preceding observer prevents terminal status notification. Pickup rechecks reservation identity and order status after shelf observers flush.

A failed first completion leaves Delivered and retires the carried bundle. The same point then exposes `order.point.complete`, retrying only Complete on the next interaction. Intake-only service fakes retain their previous hidden-prompt behavior.

## Test strength

Removing the origin match from Deliver produced the expected failing wrong-origin regression (1/1 failed). The full origin+customer check was restored before the passing full suite. Completion retry was added with an observed RED test, then passed with the new adapter path. Existing assertions were retained.

## Recovery hardening

Ready pickup now requires all three injected lifecycle dependencies (`IReadyShelfPickup`, `IOrderService`, `IEventBus`), preventing an old one-argument call from silently producing a bundle without terminal cleanup. Binding happens inside the reservation cleanup scope, so an invalid/missing order cannot leak a scene root. Existing real-service tests and shelf fixtures use the full seam.

A legacy or otherwise unbound stale bundle can be cleared through the normal interaction driver, even when its order failed, was delivered/completed, or no longer exists. Query offers that recovery only after Lobby-role and pause guards; Execute retires only the held bundle and does not mutate the point's live order. The regressions use InteractionActionDriver, guarding against a cleanup path that a blocked query would make unreachable.

Delivery quality rounds positive midpoint means upward (`MidpointRounding.AwayFromZero`); the 96/97 pair produces 97, alongside the existing 75/80 -> 78 case. Reusable DeliveryContractAssertions preserve explicit wrong-target identity, attempt counts, exact rejection/status events, immutable detail snapshots and matching-delivery recovery after invalid or rejected inputs.
