# DRINK-WAVE preparation validation

Runtime/test head `3e2d11478e2f9e14e99e62bb6c6062dfd0a0dd5f`, based on merged PR8/PR9.

- Unity6000.6.0f1; package pins unchanged.
- Compile PASS zero C# errors/warnings.
- EditMode136/136 PASS; PlayMode25/25 PASS.
- QA000clean-checkout PASS before Unity import.
- Exact Stored→PickedUp→Opened→CoconutJellyAdded→LemonJellyAdded→IceAdded→Shaken→Wiped→Ready; no measuring/pouring state.
- Domain mutates only; adapters publish six D2-D7 steps. MarkReady neither publishes nor changes Unity presentation; Ready visuals follow committed OrderItemStatusChanged.
- Tests cover sequence matrix, no-ticket guards, physical pickup rollback, state/identity/event-preserving same-instance readiness lifecycle, held actions, cancellation and zero-allocation Query.
- Production bypass removed; original pickup-only scene injects an isolated DevTools queue.

## Presentation follow-up

Head `320531a`: shake animator updates require an active initialized Animator; prevents editor prefab unloading and OnDisable diagnostics. Unity6000.6.0f1 compile zero C# diagnostics, full EditMode136/136 and PlayMode25/25 PASS; QA000 clean checkout PASS. Integration separately binds the replaceable view and recipe to saved assets.
