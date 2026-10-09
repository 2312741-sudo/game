# Unity validation — PR #24 (ten tables)

**Run this only after PR #22's main loop has passed the manual playtest** (`Docs/QA/PLAYTEST_VI.md`).
Until then, PR #24 is not merged into `wave/MAIN-001-integration`.

Use a **separate** worktree so that `game-QA-22` keeps its validated state.
Never touch `Lưu trữ/game`: Antigravity works there.

## 1. Worktree

```bash
cd "$HOME/Lưu trữ/game"
git fetch origin feature/TABLES-10-integration
git worktree add "$HOME/Lưu trữ/game-QA-24" FETCH_HEAD   # detached, read-only use
git -C "$HOME/Lưu trữ/game-QA-24" rev-parse --short HEAD
```

## 2. Automated (batch)

```bash
cd "$HOME/Lưu trữ/game-QA-24"
bash Automation/unity_validate.sh
```

Record every result in the table below.

| Check | Expected |
|---|---|
| Compile | 0 errors |
| QA RunAllBatch | PASS. INFO "no TABLE_01..TABLE_10 anchors" is expected until Antigravity's customer area lands |
| EditMode | all pass, including `Customers/*`, `Lobby/TenTableOrderFlowTests`, `UI/TenTableHudTests` |
| PlayMode | all pass, including `TramChanhTenTableTests` TABLES_001–007 and `TramChanhMainSceneTests` |
| static_preflight | PASS (it now scans only Git-tracked files) |

Known risks to look at first if PlayMode fails:
- `TABLES_003` and `TABLES_006` at TABLE_01 and TABLE_02: the stand point clears the environment stools by only 0.04 m and 0.11 m.
- The route to TABLE_08–10 passes the vehicle and snack display.

Report the failing test and the collider it names. Do not loosen the test.

## 3. Manual (keyboard and mouse)

Play `SCN_TramChanh_Main` and check each item below.

- [ ] The scene opens with **one** dine-in customer (capsule) at a free table, plus the vehicle customer. More arrive over time. **No more than 3** dine-in customers are present at once (the default).
- [ ] TABLE_03–10 are visible as placeholder crate tables. TABLE_01 and TABLE_02 use the environment furniture.
- [ ] Walk to **every** occupied table. Each can be aimed at from at least one side, and you can walk to it without getting stuck.
- [ ] Take orders from two different tables. The HUD lists both ("Bàn n"), and the hint names the right table.
- [ ] Deliver one order to the **wrong** table. It is refused, and the tray stays in your hand. Then deliver it to the right table: it completes.
- [ ] After completing an order, the customer disappears about 2 s later. The table is free and can get a new customer.
- [ ] A **mixed** order at a table, and the **vehicle** order, still work as in PR #22.
- [ ] No red Console entries.

## 4. Report

Post on PR #24 and issue #20, using this template:

```
PR #24 Unity validation — <commit> — Unity 6000.6.0f1 — <tester>
Compile / QA / EditMode x/y / PlayMode x/y
Manual: customers ☐ max-active ☐ all tables reachable ☐ wrong-table refused ☐ release+reuse ☐ mixed ☐ vehicle ☐
Console errors: …
Failures (test + message / steps): …
```
