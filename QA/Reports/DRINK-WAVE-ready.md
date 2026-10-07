# DRINK-WAVE Ready adapters validation

Runtime/test head5c2f4b4, contracts3e56f2c and held-use included.

Unity6000.6.0f1 compilePASS zero C# errors/warnings; Edit122/122 and Play28/28 PASS. QA000clean-checkoutPASS before import.

Tests cover prepared placement, failure preserves hands/presentation, placement anchor alignment, restored colliders/layers, whole-order carrying, pickup refusal/reentrancy guards, slot visibility cleanup, zero-GC queries and reentrant failure during placement. New stale-visual regression failed before the fix and passed after it. Real Ordersservice integration tests remain pending in the integration task.

## Follow-up: Claude review B1–B3 (rebased onto develop `01e50b4`)

Implementation by Claude Code on the root integrator's request; the earlier Unity results above predate this change and must be re-run by the root.

- **B1** `ReadyCounterPoint.Execute`: after a committed `PlaceReady`, the hand release is isolated (`ReleaseFromHand`). A faulting `HeldItemChanged` observer is logged and no longer skips presentation or reconciliation; the slot's actual state is read back (a release already done by a listener is not repeated; a slot that still holds the item is reported with `Debug.LogError` and the committed shelf placement is still presented).
- **B2** The visual's pre-commit parent (the receiving hand anchor) is captured. On a `PickedUpByLobby` reconcile the item is detached only if it is still under that hand anchor; an item adopted by a `ServedOrder` bundle is never re-parented. `Failed` still discards the visual wherever it is.
- **B3** Adapter tests: `B1_*` (3), `B2_*` (2), `B3_PickupRequestsTheOldestReadyOrderAndCarriesItsItemsInSnapshotOrder`. Real-service regressions (`OrderService`, `StallTicketQueue`, `ReadyShelf`; drink+cake bundle in ascending item ids, partial order, capacity-2 oldest first, throwing `HeldItemChanged`, real Lobby pickup inside the placement flush, reentrant failure) live in `Tests/EditMode/Ready/ReadyRealServiceTests.cs` on the stacked branch `feature/DRINK-WAVE-ready-realservice`, because the real services arrive with PR #10.
- Out-of-Unity harness (managed UnityEngine stand-in, real PR #10 services): 25/25 pass; the new real-service B1/B2 tests failed (RED) on the unfixed adapter. Mutations (unconditional unparent, unisolated release) fail the new tests. Unity compile/EditMode/PlayMode not run here.
