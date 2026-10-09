# Unity validation on the Mac (MAIN-105)

This is the procedure that turns "compiles in the cloud container" into a real Unity result for
**PR #22 (`wave/MAIN-001-integration` → `develop`)**. Only a run in **Unity 6000.6.0f1** on the Mac counts as a Unity
result. The cloud agents have no Editor: their harness runs (`QA/Reports/MAIN-105-test-inventory.md`) and static
checks are supporting evidence, never a Unity PASS.

Tools added by MAIN-105 (branch `feature/MAIN-105-qa-validation`, based on the PR #22 head `248eac1`):

| Tool | What it is |
|---|---|
| `Automation/static_preflight.py` | Static GUID/fileID/duplicate-meta check of every scene, prefab and asset. No Unity. |
| **Tram Chanh ▸ QA** menu (`Scripts/Editor/QA/TramChanhValidationMenu.cs`) | Read-only editor checks; writes `Temp/TramChanhQA/report.txt`. |
| `TramChanh.EditorTools.TramChanhValidationMenu.RunAllBatch` | Same checks for `-batchmode -executeMethod`; exit code 0 = no errors, 1 = errors. |
| `Automation/unity_validate.sh` | Runs static pre-flight, the QA batch method, EditMode and PlayMode tests; writes `~/TramChanh-validation/<timestamp>/`. |

None of them saves scenes, prefabs, assets or settings, and none runs a Git command that changes anything.

---

## 0. Before you start

1. Unity Hub shows **6000.6.0f1** installed and your licence is active (open any project once if unsure).
   The batch runs use that activation; no serial, password or token is needed.
2. The project folder is the canonical clone (`Lưu trữ/game`). Always quote it in Terminal:
   `cd "$HOME/…/Lưu trữ/game"`.
3. **Close Unity** (or at least this project). Batch mode refuses while `Temp/UnityLockfile` exists.
4. Back up and update with the existing sync script (read `Docs/UNITY_LOCAL_SYNC.md`):
   ```bash
   bash ~/unity_local_sync.sh backup
   git fetch origin
   bash ~/unity_local_sync.sh update feature/MAIN-105-qa-validation   # PR #22 content + the QA tools
   ```
   To validate the bare PR #22 branch instead, use `update wave/MAIN-001-integration`; then only the Test Runner
   and the manual checklist are available (no QA menu, no scripts).
5. Write down the commit: `git rev-parse --short HEAD`.

## 1. Static pre-flight (30 seconds, no Unity)

```bash
python3 Automation/static_preflight.py
python3 Automation/qa000_repo_check.py
```

Expected: `STATIC PREFLIGHT: PASS` and QA-000 with no FAIL lines. A FAIL names the file and line; stop and report it
(section 6) before opening Unity.

## 2. GUI procedure

### 2.1 Open and import
1. Open the folder in Unity Hub with **6000.6.0f1**. Wait until the import progress bar is gone.
2. **Window ▸ General ▸ Console**. Click **Clear**. Any red entry left after Clear is a compile error: stop and
   report it with the full text.
3. If Unity asks to upgrade or change the project, choose the option that does not modify it and report the dialog.

### 2.2 One-time local settings (not tracked in Git)
`ProjectSettings/` is not committed (only `ProjectVersion.txt`), so each Mac sets these once:
1. **Tram Chanh ▸ Setup ▸ Apply Project Settings** — physics layers (9 = Interactable), colour space, URP asset.
2. **Tram Chanh ▸ Scenes ▸ Set Build Settings To Playable Scenes** — puts `SCN_TramChanh_Main` first.
   (The QA menu only *reports* the scene list; it never changes it.)

### 2.3 QA menu
Run **Tram Chanh ▸ QA ▸ Run All**. It opens each scene additively, inspects it, and closes it without saving.

| Item | Checks |
|---|---|
| Validate Missing Scripts | every GameObject in `Assets/TramChanh/Scenes/**.unity` and `Assets/TramChanh/Prefabs/**.prefab` (`GameObjectUtility.GetMonoBehavioursWithMissingScriptCount`) |
| Validate Prefab and Scene References | every object-reference property that stores an id but resolves to nothing ("Missing"), and prefab instances whose asset is missing |
| Validate Main Scene Required Objects | exactly one `TramChanhMainBootstrap` in `SCN_TramChanh_Main`, enabled; every object field assigned, no empty array; the environment prefab has `StallRoot`, `PlayerSpawn`, `TablePoint`, `CakeTablePoint`, `VehiclePoint`, and a `StallAnchorSet` with 10 anchors (one per `StallAnchorId`) |
| Validate Interaction Anchors | every prefab with an `IInteractable` has an `InteractableRef` pointing to it and a Collider on layer 9 that resolves to that ref; layer 9 is named `Interactable` |
| Validate Build Profile Scenes | `EditorBuildSettings.scenes[0]` is `SCN_TramChanh_Main` and enabled; also the active Unity 6 Build Profile if it overrides the list |
| Collect Console Errors | runs every check with an `Application.logMessageReceived` capture and prints only the captured Console section |
| Run All | all of the above + the Console capture; one report |

Result: one Console entry (`[TramChanhQA] Run All: PASS` or `FAIL`) and the full text in
`Temp/TramChanhQA/report.txt` (open it from Finder or `open Temp/TramChanhQA/report.txt`). The last line is
`SUMMARY: PASS|FAIL - validator errors N, …, console errors M`.

### 2.4 Test Runner
1. **Window ▸ General ▸ Test Runner**.
2. **EditMode** tab → **Run All**. Wait for the green/red tree. Note total, passed, failed, ignored.
   `OrderContractTests.ARCH001_*` must pass here (it only failed in the single-assembly cloud harness).
3. **PlayMode** tab → **Run All**. Unity enters Play Mode for each fixture; do not click in the Game view.
4. For every red test, right-click → copy the full name and the failure message.
5. Console: Clear before each tab, check for red entries after it.

### 2.5 Manual Play Mode checklist (full loop)
Open **Tram Chanh ▸ Scenes ▸ Open Main Game (SCN_TramChanh_Main) - primary**, press **Play**, keep the Console visible.
Tick each line; at the first failure write down what you saw and the Console text.

Start state
- [ ] Night roadside street, green Tram Chanh stall in the middle, you stand on the customer side.
- [ ] Three customers: drink table, cake table, takeaway vehicle. No red Console entries.

Drink order (drink table)
- [ ] **E** / left mouse at the drink table opens order entry → **Enter order** → **Send to stall**.
- [ ] Red tea rack → **F** to open → coconut jelly → lemon jelly → ice → hold **F** to shake → hold at the wipe area.
- [ ] Place the drink on the Ready counter. Prompt texts are readable (no raw keys like `interaction.xxx`).
- [ ] Pick the order up at the counter and deliver it to the same table → order completes.
- [ ] A new customer arrives a few seconds later.

Cake order (cake table)
- [ ] Take the order and send it to the stall.
- [ ] Pick up the 500 ml cup → hold at the batter area to measure → pour on the grill → close the lid → open and flip
      → hold to cut → hold to add sauce → hold to roll vertically → wrap → Ready counter.
- [ ] Pick up and deliver to the cake table → completes → new customer later.

Mixed order (takeaway vehicle: 1 drink + 1 cake)
- [ ] Take the order at the vehicle; both the drink and the cake stations get a ticket.
- [ ] Make both. The Ready shelf holds 1 drink + 1 cake; the second tea rack/cake fill shows `ready.slot_full` until
      pickup (intentional, see the MAIN-001 handoff).
- [ ] The order can be picked up only when both items are Ready; deliver to the vehicle → completes.

End
- [ ] Stop Play. Console has no red entries from the session (yellow warnings: list them).
- [ ] `git status` shows no changed tracked files (Unity may touch `Packages/packages-lock.json`; report it if so).

## 3. Batch procedure (Terminal)

With Unity closed:

```bash
bash Automation/unity_validate.sh            # static + qa + editmode + playmode
bash Automation/unity_validate.sh qa editmode # any subset: static, qa, editmode, playmode
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity" bash Automation/unity_validate.sh
```

What it does:
1. Refuses if Unity is not at the pinned path (override with `UNITY_PATH`) or the project is open (`Temp/UnityLockfile`).
2. `python3 Automation/static_preflight.py` (skipped if `python3` is unavailable).
3. `Unity -batchmode -nographics -quit -projectPath … -executeMethod TramChanh.EditorTools.TramChanhValidationMenu.RunAllBatch -logFile …/qa_runall.log`
4. `Unity -batchmode -nographics -projectPath … -runTests -testPlatform EditMode -testResults …/editmode-results.xml -logFile …/editmode.log`
5. `Unity -batchmode -projectPath … -runTests -testPlatform PlayMode -testResults …/playmode-results.xml -logFile …/playmode.log`
   (graphics on by default; `PLAYMODE_NOGRAPHICS=1` adds `-nographics`).
6. Parses the NUnit XML (`<test-run total= passed= failed= …>` and every failed `fullname`) into `SUMMARY.txt`.
7. Compares `git status` before/after and reports (never reverts) any change.

Output folder `~/TramChanh-validation/<yyyyMMdd-HHmmss>/`: `SUMMARY.txt`, `static_preflight.txt`, `qa_runall.log`,
`qa_report.txt`, `editmode-results.xml`, `editmode.log`, `playmode-results.xml`, `playmode.log`, `git-before.txt`,
`git-after.txt`. Exit code 0 only if every selected stage passed.

Reading failures: `RunAllBatch: exit 1` = validator errors (see `qa_report.txt`); any other exit or a missing
`qa_report.txt` = Unity could not run the method (usually compile errors: `grep "error CS" qa_runall.log`).
Test exit codes: 0 all passed, 2 some failed, 3 run error. A missing results XML means the Editor crashed or never
finished; read the end of the log.

Each Unity launch re-opens the project (1–5 minutes each on first import). The first launch may take much longer
while the Library folder is rebuilt.

## 4. Acceptance gate for PR #22

PR #22 may leave draft only when **all** of these hold on the Mac, on the same commit:

1. Unity 6000.6.0f1 imports the project with **0 compile errors** in the Console.
2. `static_preflight.py` PASS and `qa000_repo_check.py` with no FAIL.
3. **Tram Chanh ▸ QA ▸ Run All** (or `RunAllBatch`) `SUMMARY: PASS` — 0 validator errors, 0 captured console errors.
4. **EditMode**: 0 failed, including `OrderContractTests.ARCH001_*`. Any ignored/skipped test is listed with its reason.
5. **PlayMode**: 0 failed, including `TramChanhMainSceneTests` (both MAIN-001 tests).
6. Manual checklist 2.5 complete for the drink, cake and mixed orders, with no red Console entries during play.
7. `git status` unchanged by the runs (or the changed files are reported and explained).

If any line fails, PR #22 stays draft and the failure goes to issue #20 / PR #22 as below.

## 5. What the cloud already checked (not a substitute)

- Runtime scripts and `TramChanhMainSceneTests.cs` compile against UnityEngine 2021.3.33 reference DLLs; the QA menu
  compiles against 2021.x editor references (`QA/Reports/MAIN-105-static-and-compile.md`).
- Static pre-flight: 49 files, 1856 GUID references, 3806 local fileIDs, 0 unresolved, 0 duplicate GUIDs.
- Harness runs (stand-ins, not Unity): drinks 69/69, cakes 28/28, orders/lobby 181/181 when built one assembly per
  asmdef (180/181 single-assembly, the ARCH001 assembly-name test; see the inventory §5).
- 29 of 48 test files have never run anywhere. They only run in steps 2.4 / 3.

## 6. Reporting the result on GitHub

Post one comment on **PR #22** (and link it from issue #20). From the batch run:

```bash
gh pr comment 22 --body-file ~/TramChanh-validation/<timestamp>/SUMMARY.txt
```

or paste this template:

```markdown
### Unity validation — <PASS|FAIL>
- Commit: `<git rev-parse --short HEAD>` on `<branch>`
- Unity 6000.6.0f1, macOS <version>, <Mac model>
- Static pre-flight: <PASS/FAIL>; QA-000: <PASS/FAIL>
- QA menu Run All: <SUMMARY line from qa_report.txt>
- EditMode: total <n>, passed <n>, failed <n>, skipped <n>
- PlayMode: total <n>, passed <n>, failed <n>, skipped <n>
- Failed tests: <fullname — first line of message> (or "none")
- Manual loop: drink <ok/fail>, cake <ok/fail>, mixed <ok/fail>; Console errors during play: <none/list>
- git status after runs: <clean / files>
<details><summary>qa_report.txt</summary>

(paste)
</details>
```

Attach `editmode-results.xml`, `playmode-results.xml` and the logs (zip the timestamp folder) when anything failed.
Do not paste licence details, serials or tokens; the logs do not contain them, but check before attaching.
