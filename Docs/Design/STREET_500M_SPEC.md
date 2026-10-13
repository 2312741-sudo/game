# Walkable street, 500 m: specification

Owner of the spec: Claude (lead). Owner of the geometry: Antigravity (`ExtendedStreetBuilder.cs`, `ArtSource/Scripts/generate_extended_map_meshes.py`).
Status: spec only. Nothing in this document is built yet. The street in the game today is 80 m (x from -40 to 40).

## 1. Goal

The player can walk a 500 m street. The stall stays at the origin, so the street runs from x = -250 to x = +250.
The 1-2 km2 backdrop (distant city, not walkable) is a later phase and is out of scope here.

## 2. What exists today (from `ExtendedStreetBuilder.cs`)

Coordinates are environment-root local, metres. The stall root is at (0, 0, 0).

| Part | Extent (x) | z range | Notes |
|---|---|---|---|
| `Sidewalk` (service zone) | -8 to 8 | -3.6 to 3.6 | collider, top at y = 0 |
| `SouthSidewalk_80m` | -40 to 40 | about -10 to 3.6 | collider, 3 boxes around the alley |
| `Roadway_80m` | -40 to 40 | 3.6 to 12.6 | collider top at y = -0.15 |
| `NorthSidewalk_80m` | -40 to 40 | 12.6 to 22 | collider |
| `Alley_Pavement` | 16.5 to 20.5 | -15 to -3.8 | side alley |
| `Boundary_RoadCurb` | -40 to 40 | 3.65 | 0.12 m curb (was a 2.5 m wall; the player now walks onto the road) |
| `Boundary_MapEnd_West/East` | -40 / 40 | -3.9 to 3.6 | 3 m walls closing the street ends |
| `Boundary_NorthSidewalk_Back` | -40 to 40 | 16.65 | 3 m wall |
| `Boundary_SouthBuildings_*` | -40 to 40 | -3.85 | 3 m wall in front of the shopfronts |

Waypoint lanes (`BuildWalkwaysGraph`): `SidewalkNorth` z = 14.5 (spacing 2.5 m), `SidewalkSouth` z = 3.1 near the stall (spacing 2.0 m),
`Alley`, `RoadEast`, `RoadWest`. Entries `StreetEntryWest` (-16, 0, 2) and `StreetEntryEast` (12, 0, 2). Ten `TableApproach_01..10` and `BikeParking_nn`.
Anchor names are `WP_<Lane>_<nn>`; the graph builder sorts them by the number, so three digits are fine (`WP_SidewalkNorth_126`).

## 3. Target layout

1. A single constant `StreetHalfLength = 250f` replaces every hard-coded `40f` / `80f` in the builder and in the mesh generator.
2. Keep the service zone (|x| <= 8) exactly as it is. Do not move the stall, the 10 tables, `PlayerSpawn`, `LobbyPosition`, `ReadyHandoff`, `VehiclePoint`.
3. Outside the service zone, build the street from repeated 40 m modules. A module is: shopfront block (A, B or C in a fixed rotation that does not repeat twice in a row),
   street trees every 12 m, a street lamp every 24 m, a utility pole every 36 m, night point lights every 12 m.
   12 whole modules plus two 10 m end pieces cover 500 m.
4. One side alley every 120 m (the existing alley at x = 18.5 is the first). Alleys are decoration plus a dead-end boundary; they do not need lanes beyond the first.
5. Ends: replace the 3 m end walls with a believable closure (a building across the street, or a gate). The walls must still block the player at x = +/-250.
6. Ground colliders must be continuous: no gap between sidewalk, curb, road and the north sidewalk, so the player never falls. The road top stays at y = -0.15 and the sidewalk top at y = 0.
7. Building feet reach the ground (same rule as `BuildingFoundationGapTests`): lowest vertex <= ground top + 0.01 m.

## 4. Waypoint graph at 500 m

- Lane anchors continue along the whole street with the same spacing and the same names (`WP_SidewalkNorth_01` ... `_201`, and so on).
- Graph errors must stay at 0 (`WaypointGraphBuilder` validation). Every `TableApproach_nn` must be reachable from `StreetEntryWest` and `StreetEntryEast`.
- Customers must enter near the stall. Keep `StreetEntryWest/East` near the stall (within 30 m of x = 0). Do not move them to the street ends,
  or customers would walk 250 m (about 190 s) and the walk timeouts would trip.
- Add `AmbientEntryWest` (-250, 0, 2) and `AmbientEntryEast` (250, 0, 2) as the far lane ends for ambient pedestrians and bikes. Gameplay side (Claude) will use them with a spawn window, see section 6.

## 5. Performance budget

| Item | Budget |
|---|---|
| Triangles in view, whole street | 400,000 or fewer at the player's eye |
| Draw calls | batch repeated modules: static batching or GPU instancing |
| Textures | reuse the existing shopfront atlas; no new 2048 textures per module |
| Night point lights | at most 8 real-time lights active near the player; the rest baked or emissive only |
| Colliders | boxes only (no mesh colliders), as today |

## 6. Gameplay side (Claude, after the geometry lands)

- Ambient spawn window: pedestrians and bikes spawn only within about 60 m of the stall and are released beyond 80 m, so crowd density near the stall does not drop 6x.
  Config: `PeopleSettings` gets a window length; 0 keeps today's whole-lane behaviour.
- Customers keep using the near entries.
- Clock and opening hours are unchanged.

## 7. Acceptance for the geometry

- Player can walk the whole 500 m on the sidewalk and across the road, and never falls through the ground.
- `python3 Automation/art_contract_audit.py` reports 0 FAIL.
- Waypoint graph: 0 errors, table routes <= 22 m from the nearest customer entry.
- EditMode and PlayMode: no new failures against `main`.
- Before/after screenshots at x = -240, -120, 0, 120, 240.
- Do not rename any anchor listed in `ART_INTEGRATION_HANDOFF.md`.
