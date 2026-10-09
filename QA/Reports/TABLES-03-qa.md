# TABLES-03 — Ten-table QA: PlayMode tests, QA menu, art audit, anchor contract

Branch `feature/TABLES-03-qa`, based on `feature/TABLES-10-integration` @ `390f9e8`. The tests target the lead's bootstrap @ `c7a5de6`.
Container without Unity. **Nothing here is a Unity PASS.** These results come from compile checks and static checks only.

## Files

| File | Change |
|---|---|
| `Assets/TramChanh/Tests/PlayMode/Integration/TramChanhTenTableTests.cs` (+ `.meta`) | New PlayMode fixture `TramChanhTenTableTests` (TABLES_001..007) |
| `Assets/TramChanh/Scripts/Editor/QA/TramChanhValidationMenu.cs` | `ValidateTableAnchors`, called from "Validate Main Scene Required Objects" |
| `Automation/art_contract_audit.py` | `table_anchor_checks` on `PF_AccelRoadsideEnvironment` |
| `Docs/Coordination/TABLE_ANCHOR_CONTRACT.md` | Contract for Antigravity |

## Tests (`TramChanhTenTableTests`, loads `SCN_TramChanh_Main` additively like `TramChanhMainSceneTests`)

| Test | Asserts |
|---|---|
| TABLES_001 | 10 `Tables` and 10 `TableAnchors`. Anchor i is named `TABLE_{i+1:00}` and has a `Seat`. `TableId` is i+1 and the interactable id is 31+i, all unique and distinct from the vehicle's id. The point is within 0.5 m of its anchor and keeps its `InteractionPoint`. `DrinkTable`/`CakeTable` are `Tables[0]`/`Tables[1]`. `CustomerPoints` holds the 10 tables + vehicle. |
| TABLES_002 | When the environment provides `TABLE_nn` (or `TablePoint`/`CakeTablePoint` for 1/2), the runtime anchor sits on it (< 1 cm). |
| TABLES_003 | Each table's trigger is on layer 9 and its top is above 0.4 m. The open side is picked deterministically: toward PlayerSpawn first, then the 8 compass directions, taking the first whose 0.8 m stand point fits the player capsule. A ray from eye height 1.6 m, 1 m out on that side, hits within 1.8 m (layer 9, triggers collide). The first hit resolves to **this** table's `InteractableRef`. A second ray (non-gameplay layers, triggers ignored) finds no occluder before the table, apart from the table's own furniture. |
| TABLES_004 | `RaycastAll` down from 2 m above every anchor and Seat (all layers except 9/12, triggers ignored) finds ground within 0.1 m of the environment root y. |
| TABLES_005 | Anchors are pairwise ≥ 1.2 m apart (horizontal), outside the stall footprint (stall-local x ±0.9, z ±0.4) and ≥ 1.2 m from `VehiclePoint`. |
| TABLES_006 | From env `PlayerSpawn`, a CharacterController-sized `CapsuleCast` (radius/height/stepOffset read from the live controller, defaults 0.22/1.7/0.25; lower sphere lifted by stepOffset; layers except Player/HeldItem; triggers ignored) reaches each table's stand point in a straight line. Otherwise a two-segment detour via fixed waypoints (2 L-corners, midpoint ±1/2/3 m sideways) must be clear. A failure names the blocking collider (hierarchy path + layer). |
| TABLES_007 | The vehicle trigger is aimable from 1 m at eye height and resolves to the vehicle. Its mixed order goes Execute → Enter → Send and creates both a Drink and a Cake ticket, and the tea rack is available. |

## Compile results (reference DLLs: UnityEngine 2021.3.33 + NUnit stubs; not the Unity 6 compiler)

- Runtime + `TramChanhTenTableTests.cs` + `TramChanhMainSceneTests.cs`: **Build succeeded, 0 warnings, 0 errors**. This build used the lead's `c7a5de6` `TramChanhMainBootstrap.cs`, copied into the scratch build, plus a scratch-only stub of `TramChanh.Customers` (`CustomerDirector`, `CustomerDirectorSettings`, `ICustomerSeat`, `OrderPointSeat`, `CustomerSeatedEvent`), because those types are not on any pushed branch yet. Neither file is committed.
- Editor QA menu (`ed.csproj`): **Build succeeded, 0 warnings, 0 errors**. The menu uses its own constants and does not depend on the new bootstrap members.
- On this branch alone (bootstrap @ `390f9e8`), `TramChanhTenTableTests.cs` **does not compile** until c7a5de6 and the Customers assembly are merged. This is expected.

## Static checks

- `python3 Automation/art_contract_audit.py`: `SUMMARY: 323 checks ok, 23 WARN (known gaps), 0 FAIL -> PASS`. The one new WARN is `PF_AccelRoadsideEnvironment: no TABLE_01..TABLE_10 anchors (optional): bootstrap uses TablePoint/CakeTablePoint + fallback layout`. Before this change the audit gave 22 WARN and 0 FAIL.
- A synthetic tree self-test of `table_anchor_checks` passed: it detects misnamed (`Table_05`, `TABLE_1`), duplicated, misplaced and Seat-less anchors as expected. CustomerArea placement is accepted.
- `python3 Automation/static_preflight.py`: PASS (0 FAIL / 0 WARN).

## Not verified (needs Unity 6000.6.0f1)

- None of TABLES_001..007 has run. Geometry was only checked on paper:
  - The fallback layout gives a minimum spacing of 1.91 m (TABLE_09/10) and ≥ 2.48 m to VehiclePoint.
  - Every fallback lies on `Roadside/Sidewalk` (x −10..10, z −4.5..3.6).
  - The stand points toward PlayerSpawn clear the env stools by about 0.04 m (TABLE_01) and 0.11 m (TABLE_02). These margins are tight.
- Vehicle/stall/snack-display collider shapes along the routes to TABLE_08..10 were not modelled. The detour logic should cover them, but this is unconfirmed.
- QA menu output in the Editor (wording, the INFO line on the current env).
