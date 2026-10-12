# Map expansion, characters and simulated people — plan

Status: **PLAN (lead decision, defaults chosen). No gameplay code yet.**
Gate: gameplay work below is built on its own branches and is **not merged into the main scene** until the manual playtest of
PR #22 (`Docs/QA/PLAYTEST_VI.md`) passes in Unity 6000.6.0f1. Art work (Antigravity) can proceed in parallel.

## 1. Goals

1. A larger, believable Vietnamese street around the stall, still playable and readable.
2. People who look and move like people: customers who walk to a table, sit, order, eat and leave; pedestrians and motorbikes that
   make the street feel alive.
3. No change to the existing order/Lobby/drink/cake rules. People are a presentation and flow layer on top of them.

## 2. Decisions (defaults; change them by saying so on Issue #20)

| Topic | Decision |
|---|---|
| Map size | Extend the street along X (a longer block, sidewalks on both sides, one road, side alley). The **service zone** stays exactly as today (x ∈ [-8, 6.5], z ∈ [-3, 4]). New area is walkable sidewalk/visual street; no new gameplay stations in it yet. |
| Walkable bounds | The player can walk the extended sidewalk but not onto the road or into shop fronts. Invisible boundary colliders on layer 8 at the edges. |
| Navigation | **Waypoint graph**, not NavMesh. Deterministic, testable without Unity, no bake step, no extra package. |
| Customers | Walk in from the street edge to their table's `Seat`, sit, **then** the existing Lobby order flow starts, wait, eat/drink after delivery, stand and leave to the street edge. One customer per table, at most `MaxActiveCustomers` (default 3, configurable). |
| Ambient pedestrians | Non-interactive, pooled, walk along sidewalk waypoint lanes. Default max 12 visible. They never enter the service zone tables. |
| Traffic | Non-interactive motorbikes on a road lane (looped path, pooled). Default max 6. Purely visual, no physics, no collision with the player. |
| Takeaway vehicle | Keeps the current always-present customer at `VehiclePoint`. Later: a motorbike arrives, stops, leaves (phase 3). |
| Characters | 6 base humanoid bodies (varied age/gender/height) × modular clothing/hair via material/mesh swaps, one shared humanoid rig. Stylization consistent with the stall art. |
| Player body | First person. No full body needed now. Optional hands/forearms mesh for held items (phase 3). |
| Performance | Pooling, animator culling, GPU instancing, LOD for characters beyond ~15 m, no per-agent physics. Budget: ≤ 25 animated characters on screen. |

## 3. Architecture (gameplay side, owned by Claude)

New assembly **`TramChanh.People`** (references Core, Orders, Customers). Pure C# where possible so it is testable without Unity.

- `WaypointGraph` (nodes, edges, lanes, named endpoints `StreetEntryWest/East`, per-table `TableApproach_nn`).
  Authored as anchors in the environment prefab (see §5). Built at startup by `PeopleBootstrap`.
- `PathFollower` (plain C#): speed, arrival radius, deterministic stepping with game-clock delta (pause-safe).
- `PersonState` machine (plain C#): `Arriving → Seated → Ordering(Lobby) → Waiting → Eating → Leaving → Gone`.
  Ordering and delivery stay in `OrderPoint` / `OrderService`; the person only *listens* (events) and animates.
- `CustomerPresenter` (MonoBehaviour, thin): moves a character along the path, plays animator parameters, sits on `Seat`.
  Extends `CustomerDirector`'s `CustomerSeated`/`CustomerLeft` events (currently they only spawn/destroy a capsule).
  Change: seat is *reserved* when the customer starts walking; the order is requested when it **arrives**.
- `AmbientCrowd` (pool + lane scheduler): spawns pedestrians/motorbikes with a seeded RNG; configurable counts.
- All spawn/despawn on a fixed budget; all timing from the game clock so pausing the game pauses people.

Contracts to keep: orders never bypass Lobby; the director's public API stays compatible; tables keep their TableId and ids 31–40.

## 4. Phases

| Phase | Owner | Content | Gate |
|---|---|---|---|
| 0 | user + Mac Claude | Manual playtest of PR #22 passes | Unblocks all gameplay phases |
| A | Antigravity | Hygiene branch + `PF_TramChanh_Street` (prompt ART-INTEGRATION-001) | Draft PR, audit 0 FAIL |
| B | Antigravity | Extended street module + waypoint anchors (prompt ART-MAP-002) | Audit 0 FAIL |
| C | Antigravity | Character kit: 6 bodies, clothing, rig, animation set (prompt ART-CHAR-001) | Import check in Unity |
| D | Claude | `TramChanh.People`: graph, follower, state machine, tests (.NET) | After phase 0 |
| E | Claude | Customers walk and sit; `CustomerPresenter`; scene wiring | PlayMode in Unity |
| F | Claude | `AmbientCrowd` (pedestrians, motorbikes), budgets | PlayMode + profiler |
| G | Both | Polish: LOD, sounds, takeaway motorbike arrival, hands | Playtest |

## 5. Contract for Antigravity (anchors and assets)

Environment prefab (extended) additionally contains, as direct children of the root unless noted:

- `Walkways` (empty parent) with child anchors forming lanes. Names are exact:
  `WP_<lane>_<nn>` for lane points (two digits, ordered along the lane), lanes: `SidewalkNorth`, `SidewalkSouth`, `Alley`,
  `RoadEast` (motorbikes heading +X), `RoadWest` (heading −X).
- `StreetEntryWest`, `StreetEntryEast` (where people enter/exit; hidden behind a corner or building edge).
- `TableApproach_01`…`TableApproach_10` next to each table's open side, on the ground, facing the table.
- `BikeParking_01…` optional, non-gameplay.
- Everything visual-only, no gameplay components, no colliders on layer 9; boundary colliders on layer 8.

Characters: humanoid rig (Unity Humanoid avatar), ~12–20k tris LOD0 with LOD1/LOD2, one shared skeleton, root at the feet,
height 1.50–1.80 m. Animation clips (humanoid): `Idle`, `Walk`, `Sit_Down`, `Sit_Idle`, `Eat_Drink_Loop`, `Stand_Up`, `Wave`,
`Idle_Phone`; riders: `Motorbike_Ride_Idle`. Animator Controller `AC_Person` with parameters `Speed` (float), `Sitting` (bool),
`Eating` (bool), `Wave` (trigger). No scripts on character prefabs.

## 6. Tests (Claude)

.NET (no Unity): graph connectivity (every `TableApproach` reachable from both entries), path determinism and pause-safety,
state machine transitions, no assignment to occupied tables, order requested only after arrival, seat released after leave,
budgets respected, 5,000-step soak. PlayMode: persons never block the player's ray or capsule; reach and ids unchanged.

## 7. Risks

- Walking customers change when orders appear (after arrival, not at spawn): HUD/active-order tests must be updated.
- Seat reservation vs. occupancy: a table must not be reassigned while a customer is walking to or leaving it.
- Content size/perf of many animated characters: enforce budgets and LOD from the first slice.
- Art/gameplay drift: anchor names above are part of the contract; renaming needs agreement on Issue #20.
