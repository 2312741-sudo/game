# ACCEL-01 — order delivery validation

Date: 2026-10-09 (Asia/Ho_Chi_Minh)

Validated rebased runtime/test commit: `b4efced` (full suite run at reporting head `90eb301ca73a210b51e72d4794679e6b5683607c`).
Original runtime/test commit `41193b56635d710010ee8c09b0e6297651d06656` is preserved on the backup branch. Owned runtime/test files are byte-identical after rebasing onto current develop; the additional two PlayMode cases come from the merged PR #15 mouse and modal-lifecycle regressions.
Unity: `6000.6.0f1`; existing package pins retained.

- Compile: PASS, zero C# errors/warnings.
- Full EditMode: **325/325** passed, zero skips.
- Full PlayMode: **38/38** passed, zero skips.
- Delivery-focused EditMode: **41/41** passed.
- Delivery presentation PlayMode: **3/3** passed.
- QA-000: PASS in a clean detached checkout of the runtime/test commit.
- Production input paths, Ready composition and fresh-checkout Unity validation remain integration-wave gates.

## Behaviors covered

Real OrderService follows canonical intake, preparation, whole-order pickup, origin/customer-matched delivery and completion for dine-in, takeaway and mixed bundles. Invalid/default delivery actor or target does not mutate. Each wrong valid target counts once and publishes one rejection. Quality/snapshot values commit before observers. Completion frees point occupancy before observers, retaining FIFO outbox fault and reentrancy behavior.

Lobby adapters retire successfully delivered or failed bundles, preserving replacement held identities and prepared children adopted during release. Lifecycle subscriptions precede pickup reservation; a late-frame check recovers when a preceding observer prevents terminal status notification. Pickup rechecks reservation identity and order status after shelf observers flush.

A failed first completion leaves Delivered and retires the carried bundle. The same point then exposes `order.point.complete`, retrying only Complete on the next interaction. Intake-only service fakes retain their previous hidden-prompt behavior.

## Test strength

Removing the origin match from Deliver produced the expected failing wrong-origin regression (1/1 failed). The full origin+customer check was restored before the passing full suite. Completion retry was added with an observed RED test, then passed with the new adapter path. Existing assertions were retained.
