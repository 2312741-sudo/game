# ART-COMPARE-001: Environment Comparison Metrics

| Metric | Option A: Roadside ACCEL-01 | Option B: Phố mở rộng ART-MAP-002 |
| :--- | :--- | :--- |
| **Source Prefab** | `Assets/TramChanh/Prefabs/ACCEL01/Environment/PF_AccelRoadsideEnvironment.prefab` | `Assets/TramChanh/Prefabs/Environment/PF_TramChanh_StreetExtended.prefab` |
| **GameObjects** | 539 | 559 |
| **MeshRenderers** | 292 | 160 |
| **SkinnedMeshRenderers** | 0 | 0 |
| **Total Vertices** | 43,305 | 48,259 |
| **Total Triangles** | 35,578 | 34,400 |
| **Active Lights** | 10 | 15 |
| **Colliders** | 91 | 148 |
| **Unique Materials** | 41 | 22 |
| **Renderer Bounds (X × Y × Z)** | 37.2m × 15.5m × 22.7m | 80.0m × 15.6m × 36.6m |
| **Renderer Bounding Box (X Range)** | [-24.2, 13.0] | [-40.0, 40.0] |
| **Renderer Bounding Box (Z Range)** | [-9.5, 13.2] | [-15.1, 21.5] |
| **Collider Bounds (X × Y × Z)** | 26.0m × 14.2m × 22.7m | 80.2m × 14.2m × 36.6m |

## Camera Positions (Identical for Both Options)

| Shot ID | Position (X, Y, Z) | Target (X, Y, Z) | FOV | Description |
| :--- | :--- | :--- | :--- | :--- |
| `01_TopDown` | `(0.0, 32.0, -12.0)` | `(0.0, 0.0, 5.0)` | 70° | High bird's-eye overview framing the whole block/street and surroundings |
| `02_EyeLevel` | `(-0.6, 1.6, 2.8)` | `(-0.1, 1.2, 0.0)` | 65° | Customer eye-level (~1.6m) looking south towards Trạm Chanh counter and signage |
| `03_SidewalkEast` | `(16.5, 1.6, 1.8)` | `(-16.0, 1.4, 1.8)` | 65° | Looking west along the sidewalk corridor from the East |
| `04_SidewalkWest` | `(-17.0, 1.6, 1.8)` | `(16.0, 1.4, 1.8)` | 65° | Looking east along the sidewalk corridor from the West |
| `05_RoadMotorbikes` | `(-2.0, 1.6, 5.5)` | `(-11.0, 0.9, 2.8)` | 65° | Roadside view along curb towards parked motorbikes and street profile |
