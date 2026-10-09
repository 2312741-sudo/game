# TABLE ANCHOR CONTRACT (TABLES-10 / TABLES-03)

**Audience:** Google Antigravity (art), the lead, and the Claude lanes. Coordination thread: GitHub issue #20.
**Baseline:** `feature/TABLES-10-integration` @ `c7a5de6` (the bootstrap that composes TABLE_01..TABLE_10). Unity 6000.6.0f1.
**Related:** `Docs/Coordination/ART_INTEGRATION_HANDOFF.md` (general art rules, layers, ownership) and `Docs/ASSET_INTEGRATION.md`.
**Checks:** `python3 Automation/art_contract_audit.py` (no Unity needed) and the Unity menu **Tram Chanh/QA/Validate Main Scene Required Objects**. The PlayMode tests live in `Assets/TramChanh/Tests/PlayMode/Integration/TramChanhTenTableTests.cs`.

The game has 10 dine-in tables plus the takeaway vehicle. Gameplay reads table positions **by name only**. Art can move, restyle or replace the furniture freely, as long as the names and the rules below stay true.

---

## 1. Table ids

| Rule | Detail |
|---|---|
| Names | `TABLE_01`, `TABLE_02`, ..., `TABLE_10`. Upper case, an underscore, and **two digits**. |
| Wrong names | `TABLE_1`, `Table_01`, `table_01`, `TABLE-01`, `TABLE 01`, `TABLE_11`, `TABLE_00`. The audit reports these as FAIL and the QA menu as ERROR. |
| Id mapping | `TABLE_nn` has gameplay `TableId = nn` and interactable id `30 + nn` (TABLE_01 is 31, TABLE_10 is 40). The vehicle keeps interactable id 22. |
| Never renumber | A table keeps its number for its whole life, because order history and tests refer to the TableId. If you remove a table, leave a gap and do not shift the other numbers. If you add one, ask on issue #20 first; the count is fixed at 10 in code (`TramChanhMainBootstrap.TableCount`). |
| Uniqueness | Each name appears **once** in the environment prefab, including inactive objects and nested prefabs. A duplicate is a FAIL or ERROR. |
| Placement | A `TABLE_nn` is a direct child of the environment root (`PF_AccelRoadsideEnvironment`) **or** of a single direct child named `CustomerArea`. The audit FAILs other placements. The QA menu only WARNs, because the bootstrap also falls back to a deep search. |
| Optional | All 10 anchors are optional. Any table without an anchor uses the fallback layout (section 8). The audit reports missing anchors as WARN. |

## 2. Child anchors under `TABLE_nn`

| Child | Required | Meaning |
|---|---|---|
| *(the `TABLE_nn` transform itself)* | yes | It sits at the table centre on the ground (y = 0 of the environment). Its rotation becomes the table's rotation. Use scale 1. |
| `Seat` | recommended (WARN if missing) | It is the point where the customer sits. Put it at the floor under the stool centre (y = 0), with +Z facing the table. The bootstrap copies its world position and rotation. Without it, the bootstrap puts the seat at table-local `(-0.7, 0, 0)` facing the table. |
| `ServicePoint` | optional | Reserved: it marks where the server stands, on the open side 0.8 m from the table centre. Code does not read it yet. Today the tests pick the open side toward `PlayerSpawn` first, then the 8 compass directions of the anchor. If you add it, it names your intended approach side for future use. |

Name the child exactly `Seat`. Stool and table meshes can sit anywhere under `TABLE_nn` (for example `TABLE_03/PF_YellowCrateTable`, `TABLE_03/PF_PlasticStool`).

## 3. Furniture prefab requirements

- **Visual only.** Do not put `InteractableRef`, `TableOrderPoint`, `VehicleOrderPoint` or any other gameplay `MonoBehaviour` under `TABLE_nn`. The bootstrap adds the gameplay point. Having one is a FAIL in the audit and an ERROR in the QA menu.
- **Colliders:** either none, or solid colliders on the **Environment layer (8)** only. The audit and the QA menu WARN about solid colliders on any other layer. Never use layer 9 (Interactable) on furniture: it would capture the focus ray without resolving to a table.
- **Table top height ≤ 0.8 m.** The player aims from 1.6 m eye height at 1 m distance, and higher tops hide the order trigger.
- **Service lanes:** keep a **≥ 0.9 m clear lane** between furniture groups and between any furniture and the stall footprint. The stall footprint is stall-local x ±0.9 m, z ±0.4 m, plus the Ready hand-off at the front (+z).
- **Spacing:** table centres (`TABLE_nn` positions) are **≥ 1.2 m apart** horizontally and ≥ 1.2 m from `VehiclePoint`.
- **Ground:** every `TABLE_nn` and its `Seat` stand on walkable ground. A downward ray (ignoring layers 9 and 12) must find a collider within 0.1 m of y = 0. Today that ground is `Roadside/Sidewalk`, which covers x −10..10 and z −4.5..3.6.
- Renderers are free: any mesh or material, and LODs are fine.

## 4. Collision rules around a table

- Nothing solid (non-trigger) may sit within **0.5 m of the table's open-side stand point**. That point is 0.8 m from the table centre, toward the player approach. The table's own top and crate may extend up to 0.3 m from the centre on that side.
- The tests check the gameplay minimum: a player capsule (radius 0.22 m, height 1.7 m, step offset 0.25 m) fits at the stand point and can reach it from `PlayerSpawn` (−1.5, 0, 2.7). The route is either a straight line or a two-segment detour. A failure names the blocking collider.
- Low items under 0.25 m (curbs, mats) do not block, because the controller steps over them.
- Customers never block movement: the placeholder customer has no collider. An Antigravity customer visual must also be collider-less or use trigger colliders only.

## 5. What gameplay adds at runtime (do not author these)

For each table, `TramChanhMainBootstrap` creates a runtime root `TABLE_nn` under `TramChanhMainRuntime`. It is placed on your anchor, or on the fallback. Under that root it adds:

1. `Seat`, copied from your `Seat` child, or the default offset.
2. `PF_YellowCrateTable_TABLE_nn`, the gameplay point prefab (`Workstations/DrinkWave/PF_YellowCrateTable.prefab`). It brings `TableOrderPoint`, `InteractableRef`, its layer-9 **trigger** (0.6 × 0.43 × 0.4 m) and its `InteractionPoint`. Its renderers are hidden when your anchor already has renderers, so your furniture is what the player sees. Fallback tables show the placeholder crate table.
3. The customer visual `Customer_TABLE_nn` under `Seat`, while a customer is seated. This is `_customerVisualPrefab` if one is assigned, otherwise a collider-less capsule placeholder.

At runtime the environment's static `DineInCustomer` and `CakeDineInCustomer` objects are deactivated. Dynamic customers replace them.

## 6. Scene and prefab ownership

| Owner | Owns |
|---|---|
| **Claude** | `SCN_TramChanh_Main`, `TramChanhMainBootstrap` (including the fallback layout), the gameplay point prefabs, all colliders on gameplay prefabs, layers, tests and QA tools. |
| **Antigravity** | Modular visual prefabs: furniture, the customer-area prefab, customer visuals, and their meshes, materials and textures. Also the `TABLE_nn`/`Seat` names inside those prefabs. |

Antigravity does not edit `SCN_TramChanh_Main` or the bootstrap. Claude does not edit Antigravity's meshes or materials.

## 7. Integrating a customer-area prefab

1. Build the area as one prefab, for example `PF_CustomerArea_TramChanh`. Give it a `TABLE_01..TABLE_10` child for each table you place, with a `Seat` under each one. Furniture goes under its table anchor and must stay visual-only (section 3).
2. Nest it in `PF_AccelRoadsideEnvironment` as the single direct child named **`CustomerArea`**. You can also put the `TABLE_nn` anchors directly under the environment root. Keep the existing composition anchors (`StallRoot`, `PlayerSpawn`, `LobbyPosition`, `ReadyHandoff`, `TablePoint`, `CakeTablePoint`, `VehiclePoint`) where they are. `TABLE_01`/`TABLE_02` take priority over `TablePoint`/`CakeTablePoint`, but the old names are still read by other tests, so do not delete them.
3. Run `python3 Automation/art_contract_audit.py`. You need 0 FAIL. The `no TABLE_01..TABLE_10 anchors` WARN disappears once all 10 exist.
4. In Unity, run **Tram Chanh/QA/Validate Main Scene Required Objects** (and **Run All**). You need no ERROR.
5. Post the audit summary on issue #20. Claude then runs the PlayMode suite `TramChanhTenTableTests` and `TramChanhMainSceneTests` and owns any scene follow-up.

## 8. Default fallback layout (used when the environment has no `TABLE_nn`)

Resolution order per table: (1) env `TABLE_nn` (direct child, else deep search); (2) for 1 and 2 only, env `TablePoint` / `CakeTablePoint`; (3) the fallback below. Coordinates are local to the environment root, in metres, with rotation equal to the environment root's rotation.

> Lead: values are copied from `TramChanhMainBootstrap._fallbackTablePositions` at `c7a5de6`. The field is serialized, so if `SCN_TramChanh_Main` ever overrides it, update this table.

| Table | Source when no `TABLE_nn` | x | y | z |
|---|---|---|---|---|
| TABLE_01 | env `TablePoint` | −2.10 | 0 | 1.70 |
| TABLE_02 | env `CakeTablePoint` | −3.35 | 0 | 0.20 |
| TABLE_03 | fallback | −4.60 | 0 | 1.70 |
| TABLE_04 | fallback | −5.85 | 0 | 0.20 |
| TABLE_05 | fallback | −7.10 | 0 | 1.70 |
| TABLE_06 | fallback | −3.35 | 0 | −1.80 |
| TABLE_07 | fallback | −5.85 | 0 | −1.80 |
| TABLE_08 | fallback | 4.20 | 0 | 0.60 |
| TABLE_09 | fallback | 5.60 | 0 | −0.90 |
| TABLE_10 | fallback | 4.20 | 0 | −2.20 |

Other fixed points: `PlayerSpawn` (−1.5, 0, 2.7), `VehiclePoint` (2.6, 0, 2.5), stall `StallRoot` (0, 0, 0) with footprint x ±0.9, z ±0.4. The default seat is table-local (−0.7, 0, 0).
