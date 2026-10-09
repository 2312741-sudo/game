# Tram Chanh — Coordination Index

Owner: **Claude Code lead coordinator (CLAUDE-01)**. Only the lead edits this file.
Other agents (Claude subagents, Google Antigravity) report through the coordination
issue **#20** or on their own PRs and issues.

Last refreshed: 2026-10-09

## Protocol

1. Before starting work, read this index and issue #20. Check the ownership tables
   and shared contracts (`Docs/ACCEL-01_CONTRACTS.md`, `Docs/ORDER_SYSTEM.md`).
2. Announce the task on #20 with its id, branch, files/folders and dependencies.
3. Never edit a scene, prefab or source file that another active workstream owns.
4. When the task is done, commit, push, open/update a PR into `develop` and post a
   handoff on #20 with the test results. Report only checks that actually ran.
5. Do not assume a message was received until the GitHub state has been refreshed.

## Ownership

| Area | Owner |
|---|---|
| Gameplay C# scripts, state machines, drink/cake workflows, Lobby/order logic, bootstrap, `SCN_TramChanh_Main`, integration tests | Claude Code |
| 3D meshes, art prefabs, materials, textures, street environment visuals, stall visual reconstruction, equipment models, UI presentation assets | Google Antigravity |
| `PF_Stall_TramChanh` anchors, `PF_AccelRoadsideEnvironment` anchors, mixed gameplay+visual prefabs | Shared: agree on #20 first |

### Stable anchors (gameplay contract for art replacement)

- `PF_AccelRoadsideEnvironment`: `StallRoot`, `PlayerSpawn`, `LobbyPosition`,
  `ReadyHandoff`, `TablePoint`, `CakeTablePoint`, `VehiclePoint`.
- `PF_Stall_TramChanh` `StallAnchorSet`: TeaRack, Topping, IceBin, WipeArea, Grill,
  BatterArea, RollArea, Sauce, Wrap, ReadyCounter.
- Stall dimensions: W 1.8 m × D 0.8 m, counter 1.0 m, total ≈ 2.2 m.

## Active workstreams

| Id | Agent | Branch | Owned files | Depends on | PR | Status |
|---|---|---|---|---|---|---|
| MAIN-001 | CLAUDE-01 lead / CLAUDE-05 QA | `wave/MAIN-001-integration` | `Scripts/Core/Bootstrap/**`, `Scenes/Gameplay/SCN_TramChanh_Main.unity`, `Scripts/Editor/Placeholders/TramChanhMainSceneBuilder.cs`, `Tests/PlayMode/Integration/TramChanhMain*` | #16, #17, #18 (merged into the wave branch for integration) | open (see #20) | In progress |
| MAIN-002 | CLAUDE-02 drink | `feature/MAIN-002-drink-workflow` | `Scripts/Drinks/**`, `Tests/*/Drinks/**` | MAIN-001 base | — | In progress |
| MAIN-003 | CLAUDE-03 cake | `feature/MAIN-003-cake-workflow` | `Scripts/Cakes/**`, `Tests/*/Cakes/**` | MAIN-001 base, PR #16 review | — | In progress |
| MAIN-004 | CLAUDE-04 lobby/orders | `feature/MAIN-004-lobby-orders` | `Scripts/Orders/**`, `Scripts/Lobby/**`, `Scripts/UI/Orders/**`, `Scripts/Customers/**`, matching EditMode tests | MAIN-001 base | — | In progress |
| ART-* | Antigravity | to be announced on #20 | art folders only | anchor contract above | — | Not started |

## Pending source PRs

| PR | Branch | Content | State |
|---|---|---|---|
| #16 | `feature/ACCEL-01-cake` | Recipe-driven cake station | Draft; under review by MAIN-003 |
| #17 | `feature/ACCEL-01-orders` | Order delivery, reusable customer points | Draft; approved in review |
| #18 | `feature/ACCEL-01-scene` | Roadside night environment and anchors | Draft |
| #19 | `chore/UNITY-LOCAL-SYNC` | Local sync guide and scene menu | Open |
| #6 | `review/DRINK-001-pr5-and-wave-plan` | Review records | Open |

## Validation status

This container has no Unity Editor. Every check listed as run here was done with
the .NET harness or static YAML checks. Unity compilation, EditMode/PlayMode tests,
QA-000 and Console checks in Unity 6000.6.0f1 are **not run** until a session on
the user's Mac (canonical project in `Lưu trữ/game`) runs them.
