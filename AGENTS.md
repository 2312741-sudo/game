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
- Menu `Tram Chanh > QA > Capture Standard Views` (Play Mode): takes the standard screenshots and refuses blank images.
- Menu `Tram Chanh > QA > Check Assembled Stall Overlaps`: stall overlap check inside Unity.
- Playtest steps for the owner: `Docs/QA/PLAYTEST_CHECKLIST.md`.

## 6. Where to read before drawing or placing anything
- `Docs/Design/ART_PIPELINE.md`: pivots, scale, OBJ mirroring, triangle budgets, import settings.
- `Docs/Design/ROADMAP.md`: what is in scope now (look first, functions second, gameplay depth last).
- `Docs/Design/STREET_500M_SPEC.md`: street geometry and graph rules.
- Reference photos from the owner (real stall layout) take priority over your own idea of how it should look.
