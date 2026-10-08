# ACCEL-01 playable Tram Chanh plan

## Recovery and baseline
- develop d9143c14836b9eb230a9145aa0eecf446e3c7a22; main 6fd330dd48c5103a521767ae90f198fd7c4902a6 remains untouched.
- PR15 integration 395b8f1: 284 EditMode /33 PlayMode and clean fresh checkout. A new real mouse pipeline regression is failing and remains preserved; fix before merging PR15.
- PR6 9e2ad2e: review/documentation, defer until its claims match merged behavior.
- All old branches/worktrees preserved. outputs/recovery-worktrees.json records statuses; no stashes.

## Locked additive contracts
See Docs/ACCEL-01_CONTRACTS.md. Shared contracts commit precedes lane branching. Claude reviews architecture before a major merge.

## Ownership and dependencies
- Root: shared Core/Orders contract files, App composition, UI/feedback, final scene wiring and integrated acceptance tests.
- Recovery agent: only existing DrinkWaveBootstrap.cs and DrinkWaveFlowTests.cs in integration-clean; fix mouse path/teardown and validate PR15.
- Cake agent: Scripts/Cakes/**; new Cakes tests; new Editor/Placeholders/CakeWavePrefabBuilder.cs; new Prefabs/ACCEL01/Cakes/** and Data/ACCEL01/Cakes/**. Never modify shared contracts, App bootstrap, UI, package pins.
- Orders agent: existing Orders/OrderService.cs and Order.cs; Lobby/OrderPoint.cs, ServedOrder.cs and new delivery adapters; own new Orders/Lobby delivery tests. No shared interface modifications.
- Scene agent: new Editor/Placeholders/AccelRoadsideSceneBuilder.cs; new Prefabs/ACCEL01/Environment/**, Materials/ACCEL01/**, Scenes/ACCEL01/** and own visual-contract tests. Never modify existing canonical prefabs/anchors or App composition.
- Only three child slots: recovery first; cake and scene start after locked commit; orders takes recovery's slot. UI/root runs concurrently.

Dependency graph: recovery -> PR15 merge; locked contracts -> cake / orders / visual; cake+orders -> root composition+HUD; visual+composition -> integration scene -> full/fresh QA -> Claude major wave review -> develop.

## Integration order
1. Recovery fixes and validates PR15; merge approved safe PR15 and matching docs.
2. Shared additive ACCEL contracts reviewed with Claude.
3. Cake and order lane PRs into develop, merged only after dependencies/tests/QA pass.
4. Scene and UI lane changes combined into milestone PRs.
5. Integration PR and clean remote checkout verification using Unity 6000.6.0f1 and unchanged manifest/lock.

## Acceptance and provisional data
One saved scene demonstrates movement/look/interact, generic held use, table and vehicle Lobby intake, Drink/Cake/mixed orders, exact drink and cake sequences, Ready pickup, matching delivery and Completed. Night roadside scene uses approved stall dimensions/new sign only, stable root/anchors. No tea measurement. All unsupplied amounts/timing configurable TBD; dev recipe clearly labelled. Physical FlipStep replaceable.

Every lane requires compile, relevant Edit/Play tests, QA000, clean Console and a PR into develop. Root serializes Unity runs across worktrees. Final checkout audits all scenes/prefabs, missing scripts/references and package pins; captured playable visual evidence.
