# DRINK-WAVE integration validation

Date2026-10-08 (Asia/Ho_Chi_Minh). Validated remote head `8a2ab063610d566f40f089d9eb62038ad79961ff`.

- Fresh remote clone with no Library: Unity6000.6.0f1 compile PASS, zero C# errors/warnings; full EditMode284/284 and PlayMode33/33 PASS, zero skipped/inconclusive.
- QA000 PASS on the clean fresh clone before Unity import.
- Original SCN_Gameplay_Blockout, stall prefab and package pins unchanged.
- Real-service tests cover Ready release faults, callback failure/pickup, mixed drink+cake snapshots and FIFO.
- Saved-scene tests exercise dine-in and vehicle intake, separate Enter/Send, blocked ticketless rack, one held bag, exact D2-D7, early Ready rejection, placement/whole-order pickup, observer fault, modal pause/cursor/input and saved SO/localization/animation assets.
- Regression evidence: missing composition first RED, missing scene SO references reproduced in3 PlayMode cases then fixed; empty shake controller failed saved-animation test then fixed. Orders reentrant FIFO mutant fails and restored source passes.

## Playable Editor scene

Open Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity with Unity6000.6.0f1 and Play. The overlay loads the unchanged blockout additively. WASD moves; E/left mouse interacts with aimed stations; F/right mouse opens/shakes the held bag; hold E at the wipe area. Request intake at the table/vehicle precedes Enter then Send.

Tea is pre-portioned in bags in the red rack. Sequence: PickedUp → Opened → CoconutJellyAdded → LemonJellyAdded → IceAdded → Shaken → Wiped → Ready. Shake/wipe durations are configurable provisional editor seeds (1s); recipe portions remain TBD at0, not asserted real-world quantities. Item/menu/vehicle identity and final art remain placeholders.

The wave ends at Ready with generic Lobby whole-order pickup. Delivery/completion, post-pickup failure handling, production customer spawning, stock refill and a standalone player build remain outside this wave. Ready visual layout uses the canonical capacity1 per kind; larger capacities are tested at service level. For a player build, both overlay/base scenes must be included; no build is claimed.

Independent Claude review and final dependency merge/rebase verification are recorded below when complete.
