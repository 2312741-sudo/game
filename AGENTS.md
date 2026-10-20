# Rules for every agent working in this repository

Read this first, every session. It exists because each rule below was broken at least once and cost a review round.
Project owner: the user (Vietnamese speaker). Lead (gameplay code, review, merging): Claude. Art owner: Antigravity ("Anti").

## 1. Git: what you may and may not do
- Work only on the branch you were given (`fix/ART-FIX-nnn`). Create it from the newest `origin/main`.
- Never force-push. Never rewrite history on a branch someone else pushed to.
- Never merge into `main`. The lead reviews and merges.
- Never run `rm -rf ProjectSettings/`, `git restore`, `git checkout -- <file>` or `git reset --hard` on files you did not change yourself without asking first.
- One commit per group of work. Commit messages describe the change; they never contain an AI or model name.
- Do not commit one-off helper scripts (capture scripts, `FixArtPriority*`-style scripts). Use the shared tools listed in section 5.
- Large binaries (png, fbx, glb, obj, wav is the exception for tiny generated sounds) go through Git LFS. Run `git lfs pull` before you read a model.

## 2. Reporting: when you may say "done"
- A task is done only when there is evidence: a real screenshot taken in `SCN_TramChanh_Main` (not a blank or single-colour image), and the exact text of test output pasted, not paraphrased.
- Say plainly what is not done, what failed and what you did not check. "100% complete" is never acceptable without evidence for every item.
- If a test is red, paste its full failure message. Do not edit, skip or delete a test to make it pass.
- Run before every push: `python3 Automation/art_contract_audit.py` (must report 0 FAIL), `python3 Automation/static_preflight.py` (must PASS), EditMode and PlayMode tests in the Unity Test Runner.
- Check footprints before claiming a layout is tidy: `python3 Automation/prefab_bounds.py <prefab> --overlaps --floating`.

## 3. Assets and licences
- Every model, texture or sound that did not come from this project must be listed in `Docs/Reference/ASSET_LICENSES.md` with author, source URL, licence and the project paths.
- Only CC0 and CC-BY are accepted. Never label a model you downloaded as "Project Original".
- A CC-BY asset must also be added to `Assets/TramChanh/Scripts/Core/CreditsText.cs`; the test `CreditsTextTests` fails when a row is missing.
- Unknown source or unclear licence means the asset does not go into the game.
- Keep models light: no model above 50,000 triangles unless the lead approves.

## 4. Do not break the contracts
- Do not rename anything listed in `ART_INTEGRATION_HANDOFF.md` (anchor names, prefab names).
- Do not hand-edit generated prefabs when a builder script exists; change the builder (`Scripts/Editor/*Builder.cs`) and rebuild.
- Do not edit a file in `Scripts/Core`, `Scripts/UI`, `Scripts/Interaction` without telling the lead; those are gameplay code.
- Unity pitfalls that already caused compile errors: no `?.` or `??` on `UnityEngine.Object`; NUnit 3.5 has no `Is.AnyOf`; use `FindObjectsByType<T>()` with its overloads, not a missing no-argument form.

## 5. Shared tools (use these, do not rewrite them)
- `Automation/art_contract_audit.py`, `Automation/static_preflight.py`: repo checks.
- `Automation/prefab_bounds.py`: footprints, overlaps, floating objects of a prefab without opening Unity.
- `Automation/building_cube_check.py`: every building mesh must be made of solid cubes (no recess, no opening, no hollow inside). Must report 0 failed for the building meshes before you push.
- Menu `Tram Chanh > QA > Capture Standard Views` (Play Mode): takes the standard screenshots and refuses blank images.
- Menu `Tram Chanh > QA > Check Assembled Stall Overlaps`: stall overlap check inside Unity.
- Playtest steps for the owner: `Docs/QA/PLAYTEST_CHECKLIST.md`.

## 6. Where to read before drawing or placing anything
- `Docs/Design/ART_PIPELINE.md`: pivots, scale, OBJ mirroring, triangle budgets, import settings.
- `Docs/Design/ROADMAP.md`: what is in scope now (look first, functions second, gameplay depth last).
- `Docs/Design/STREET_500M_SPEC.md`: street geometry and graph rules.
- Reference photos from the owner (real stall layout) take priority over your own idea of how it should look.

## 7. Running several agents at once (art owner)
A single serial task has taken over three hours. Split every prompt that has two or more independent items across parallel sub-agents.
- One sub-agent per item, each in its own git worktree and branch made from the newest `origin/main`: `git worktree add ../game-wt-<item> -b fix/ART-FIX-nnn-<item> origin/main`. Never two agents in the same working copy.
- Give each agent a list of files it owns. Two agents never edit the same file. Items that must edit the same prefab (for example `PF_TramChanh_StreetExtended.prefab`) go to one agent.
- Sub-agents do file work only (generator scripts in `ArtSource/Scripts/`, prefab and material edits). Only ONE agent owns the open Unity instance, because the same project cannot be opened twice; that agent runs the Capture Standard Views, EditMode and PlayMode, and takes the evidence screenshots.
- One integration step at the end: merge the item branches into `fix/ART-FIX-nnn` (never into `main`), run `python3 Automation/art_contract_audit.py`, `python3 Automation/static_preflight.py`, EditMode and PlayMode on the merged result, take the screenshots from the merged result, then push.
- Every rule above (scope, evidence, no force-push, no merge into `main`) applies to every sub-agent. Report one table at the end: item, agent, branch, commit, status, evidence.
- Remove the worktrees when done: `git worktree remove ../game-wt-<item>`.

## 8. Products, brand and menu belong to the owner
- Never invent a product, a recipe, a menu item, a logo, a slogan, a brand colour scheme, a sign text or a price. The menu and the brand are what the owner has shown in his photos and told us. If a task seems to need a new one, stop and ask the lead; the lead asks the owner and writes the answer in the prompt.
- Examples in a prompt ("for example honey lemon tea") are only examples of the kind of work, never a list to build. If a prompt names no concrete item, do nothing for it.
- Current real menu (owner): lemon tea with jellies and ice, and the roll cake (bánh lăn hương). Stall name on the sign: "Trạm Chanh". Anything else needs the owner's written approval.
- Do not add Unity packages or edit `Packages/` or `ProjectSettings/` without the lead's approval.
- New assets that are not placed in the game need the owner's approval before the lead merges them.
