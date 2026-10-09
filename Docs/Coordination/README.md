# Tram Chanh — Coordination Index

Owner: **Claude Code lead coordinator (CLAUDE-01)**. Only the lead edits this file.
Other agents (Claude subagents, Google Antigravity) report through the coordination
issue **#20** or on their own PRs and issues.

Last refreshed: 2026-10-09 (polish wave started)

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
| MAIN-001 | CLAUDE-01 lead / CLAUDE-05 QA | `wave/MAIN-001-integration` | `Scripts/Core/Bootstrap/**`, `Scenes/Gameplay/SCN_TramChanh_Main.unity`, `Scripts/Editor/Placeholders/TramChanhMainSceneBuilder.cs`, `Tests/PlayMode/Integration/TramChanhMain*` | #16, #17, #18, #19, MAIN-002/003/004 merged into the wave | #22 (draft) | All agent work merged (head 7dad5e3); blocked only on Unity validation on the Mac |
| MAIN-002 | CLAUDE-02 drink | `feature/MAIN-002-drink-workflow` | `Scripts/Drinks/**`, `Tests/*/Drinks/**` | MAIN-001 base | via #22 | Done (c7fbf68): prompt fix + Ready-slot gate, 69/69 harness |
| MAIN-003 | CLAUDE-03 cake | `feature/MAIN-003-cake-workflow` | `Scripts/Cakes/**`, `Tests/*/Cakes/**` | MAIN-001 base, PR #16 review | via #22 | Done (4c76d50): PR #16 F1–F4 fixed, 28/28 harness |
| MAIN-004 | CLAUDE-04 lobby/orders | `feature/MAIN-004-lobby-orders` | `Scripts/Orders/**`, `Scripts/Lobby/**`, `Scripts/UI/Orders/**`, `Scripts/Customers/**`, matching EditMode tests | MAIN-001 base | via #22 | Done (f950ff3): mixed-order regressions, no prod change |
| MAIN-101 | CLAUDE-01 main scene | `wave/MAIN-001-integration` | bootstrap, `SCN_TramChanh_Main`, `PF_Placeholder_VehiclePoint` collider, `DrinkWaveSceneBuilder` vehicle trigger | — | #22 | Done: vehicle trigger, cake InteractableRefs, HUD wiring, raycast reach tests (head 7dad5e3) |
| MAIN-102 | CLAUDE-02 reliability | `feature/MAIN-102-reliability` | `Scripts/{Drinks,Cakes,Orders,Lobby,Stall/Runtime}/**` + their tests | wave head 248eac1 | via #22 | Done (c04f502): drink discard, rack restock, cup return, deadlock model check |
| MAIN-103 | CLAUDE-03 player UX | `feature/MAIN-103-player-ux` | `Scripts/UI/**` (C# only), `Tests/EditMode/UI/**`, `SO_PromptText_TramChanhMain.asset` | wave head 248eac1 | via #22 | Done (a682746): HUD panels, 202/202 keys vi/en |
| MAIN-104 | CLAUDE-04 art contracts | `feature/MAIN-104-art-contracts` | `Docs/Coordination/ART_INTEGRATION_HANDOFF.md`, `Automation/art_contract_audit.py` | — | via #22 | Done (20788fe): handoff posted on #20; audit 0 FAIL |
| MAIN-105 | CLAUDE-05 QA | `feature/MAIN-105-qa-validation` | `Scripts/Editor/QA/**`, `Automation/unity_validate.sh`, `Automation/static_preflight.py`, `Docs/QA/**` | — | via #22 | Done (bf5731c): QA menu, unity_validate.sh, static preflight PASS |
| ART-* | Antigravity | to be announced on #20 | art folders only | anchor contract above | — | Not started |

## Pending source PRs

| PR | Branch | Content | State |
|---|---|---|---|
| #16 | `feature/ACCEL-01-cake` | Recipe-driven cake station | Changes required (F1–F4), fixed in #22 |
| #17 | `feature/ACCEL-01-orders` | Order delivery, reusable customer points | Draft; approved in review |
| #18 | `feature/ACCEL-01-scene` | Roadside night environment and anchors | Draft |
| #19 | `chore/UNITY-LOCAL-SYNC` | Local sync guide and scene menu | Open |
| #6 | `review/DRINK-001-pr5-and-wave-plan` | Review records | Open |

## Validation status

This container has no Unity Editor. Every check listed as run here was done with
the .NET harness or static YAML checks. Unity compilation, EditMode/PlayMode tests,
QA-000 and Console checks in Unity 6000.6.0f1 are **not run** until a session on
the user's Mac (canonical project in `Lưu trữ/game`) runs them.
