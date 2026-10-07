# PR #3 — Claude Code re-review (REV-001 follow-up)

| | |
|---|---|
| PR | [#3 CX-010–CX-012: add the playable first-person interaction slice](https://github.com/2312741-sudo/game/pull/3) |
| Head reviewed | `b0abe50982b6181c7589835e2f345131c65e17d8` |
| Base | `develop` @ `0055cad2d1d8446293e5cd1a57908a7090429fda` (includes merged ENV-001 / PR #4) |
| Previous review | [`PR-3-claude-review.md`](PR-3-claude-review.md) — head `944f940`, CHANGES REQUESTED |
| Reviewer | Claude Code (Technical Lead / Architect) |
| Date | 2026-10-07 |
| Verdict | **APPROVED** |

## What changed since the previous review

I fetched the latest `develop` and the PR head and diffed the current branch against both the
new base and the previously reviewed head (`git range-diff 97d85eb..944f940 0055cad..b0abe50`).

- `develop` gained ENV-001 (PR #4, squash `0055cad`): Unity 6000.6.0f1, the updated packages,
  `packages-lock.json`, and `testables: ["com.unity.inputsystem"]`.
- PR #3 was rebased onto it. Its first commit is unchanged (`fe47996` = `ff82932`).
- Compared with the previously reviewed head `944f940`, the PR changes only three files:
  - `Docs/PROJECT_TASK_PLAN.md`: status-board rows for REV-001 and CX-010/011/012;
  - `QA/Reports/CX-010-player-interaction-slice.md`: retitled; new validation and reproduction notes;
  - `QA/Reports/CX-010-changed-files.txt`: one line added.
- **All C# sources, scenes, prefabs, ScriptableObjects, input actions and asmdefs are
  byte-identical to the previously reviewed head.** The architecture findings from the previous
  review therefore still apply unchanged.
- The PR changes no files under `Packages/` or `ProjectSettings/`. The environment comes only
  from `develop`.

## Previously blocking findings

| # | Previous blocker | Current state | Status |
|---|---|---|---|
| 1 | PlayMode tests did not compile on a fresh checkout (`InputTestFixture` without Input System `testables`) | `develop` manifest now has `"testables": ["com.unity.inputsystem"]`. The PlayMode asmdef references `Unity.InputSystem` + `Unity.InputSystem.TestFramework`. PR #4 and PR #3 both report the TestFramework assembly built in a fresh clone with no `Library` | ✅ Resolved |
| 2 | Validated on Unity 6000.6 / URP 17.6 / Input System 1.20 / Test Framework 1.8 while the repo pinned 6000.0.40f1 / 17.0.3 / 1.11.2 / 1.4.5 | `ProjectVersion.txt` = `6000.6.0f1 (f7f8ed4d1e24)`. `manifest.json` = URP 17.6.0, Input System 1.20.0, Test Framework 1.8.0. Pin and validation environment are now identical. Saved assets were authored in 6000.6 and need no downgrade | ✅ Resolved |
| 3 | Scope/title/status did not describe CX-010–CX-012; REV-001 unrecorded | Title is "CX-010–CX-012: add the playable first-person interaction slice"; the second commit is retitled. The PR body lists the scope per task. The status board has rows for REV-001 ("review completed; re-review pending") and CX-010/011/012 ("delivered in PR #3; awaiting re-review") | ✅ Resolved |

## Verification

**Verified independently** (repository content, no Unity editor):

| Area | Check | Result |
|---|---|---|
| A | Project pinned to Unity 6000.6.0f1 | ✅ `ProjectVersion.txt` on `develop` and PR head |
| A | `manifest.json` ↔ `packages-lock.json` consistent | ✅ All 20 direct entries match at depth 0; all 15 transitive entries resolve and meet their minimum versions (checked in the PR #4 review; PR #3 does not touch either file) |
| A | Input System `testables` configured | ✅ Top-level `testables` array in the manifest |
| B | EditMode count | ✅ The source declares exactly **66** EditMode test cases (`[Test]` plus each `[TestCase]`) |
| B | PlayMode count | ✅ The source declares exactly **15** PlayMode tests (11 interaction, 2 core, 1 blockout, 1 smoke) |
| B | QA-000 | ✅ PASS on a clean checkout of `b0abe50`: docs, ground truths, asmdef graph, remote branches, canonical names |
| C | Tracked scene/prefab content not altered by the environment migration | ✅ All scenes/prefabs/assets are byte-identical to the previously reviewed head. PR #4 changed only the three environment files |
| C | Serialized references | ✅ Unchanged since the previous review, where all 26 scene GUIDs and every prefab/asset GUID resolved |
| D | Title reflects CX-010–CX-012 | ✅ |
| D | Status board accurate | ✅ REV-001 correctly shows "re-review pending"; update it to Done after this approval |
| D | No drink/cake production logic | ✅ No changes under `Scripts/Drinks`, `Cakes`, `Orders`, `Lobby`, `Customers`, `Stall`; no recipe, sauce, doneness or batter identifiers in added code |
| D | Ground truths unchanged | ✅ No changes to `Core/GroundTruth`, stall/tea-rack/grill/station prefabs, the blockout scene, or any doc other than the status board |
| E | Movement/camera separated from interaction | ✅ Unchanged: `FirstPersonController` / `PlayerInputReader` are separate from `PlayerInteractor` |
| E | `PlayerInteractor` follows the approved contracts | ✅ Unchanged: Interactable-layer raycast, data-driven reach, change-only prompt publishing, clock-driven Hold/Continuous, Hidden/Blocked handling |
| E | `IInteractable` workflow-agnostic | ✅ `TramChanh.Interaction` references only Core + Input System |
| E | Tea rack / grill are placeholders | ✅ `InspectionInteractable` only toggles a highlight |
| E | No hard-coded recipe/preparation values | ✅ Only provisional player tuning, as `[Tbd]` data in `SO_Balance_PlayerPreview` |

**Taken from the author's fresh-checkout report** (macOS; logs at
`/private/tmp/CX-010-env-alignment`, not in the repo). This review environment has no Unity
editor, so these were not executed here:

- C# compile with zero errors/warnings;
- EditMode 66/66 and PlayMode 15/15 passing;
- Input System fixture tests executing;
- scenes/prefabs loading without serialization errors;
- tracked assets byte-identical after import.

These are consistent with everything verified above: the test counts match the source exactly,
the environment matches the pin, and the assets match the head that was already reviewed.

## New blocking findings

None.

## Remaining non-blocking technical debt

Carried over from the previous review. None of these blocks merge.

1. **Missing `.meta` files on `develop`.** About 30 files (mostly asmdefs/AssemblyInfo, plus
   `StallDimensions.cs`, `StallPlaceholderBuilder.cs`) and about 20 folders have no `.meta`. The PR
   body says regenerated local metadata had to be kept aside to switch branches, which is this
   problem in practice. Recommended next: a chore PR committing all generated metas from 6000.6.0f1.
2. **Pausing cancels a hold instead of freezing it** (TC-INT-006 intent).
3. **`EnsureInputSystem` changes input handling without a "restart required" message.**
4. **`InspectionInteractable.Awake` assumes a material with `_BaseColor`** (fixture-only).
5. **Validation evidence lives in local `/private/tmp` paths and was run on macOS only;**
   there is no CI. QA-001 (batch automation/CI) would make this reproducible.
6. **Architecture notes to address before production:**
   - the interaction test scene duplicates the blockout layout (guarded by a comparison test);
   - `PlayerInputReader` owns the global pause, which belongs in `GameState` (CX-004);
   - test-only code (`PlayerInteractionTestBootstrap`, `InspectionInteractable`) is in runtime
     assemblies;
   - `PlayerInteractor` depends on the concrete `PlayerInputReader`.
7. **Stale docs from the CX-001 era, outside this PR** (owner: Claude Code):
   - `README.md` still says "any 6000.0 LTS patch";
   - the status-board rows for CX-001 ("awaiting first Unity open") and QA-000 ("main and
     develop do not exist") are out of date.

## Merge readiness

**Safe to squash-merge into `develop`.** GitHub reports the PR as mergeable with no conflicts,
and its base is the current `develop` head.

After merging:

1. Set REV-001 to **Done** and CX-010/011/012 to **Done** on the status board.
2. Have every contributor open the project with Unity 6000.6.0f1.
3. Schedule the `.meta` chore PR before the next feature PR to avoid GUID conflicts.

---
_Generated by [Claude Code](https://claude.ai/code)_
