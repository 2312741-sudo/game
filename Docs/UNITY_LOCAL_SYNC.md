# Unity local sync guide

How to keep the Unity Editor on your Mac showing the same project that Codex, Claude Code and GitHub contain.

## The facts that matter

| Item | Value |
|---|---|
| Unity version (pinned) | **6000.6.0f1** |
| Unity project folder | the **root of the Git clone** (the folder that contains `Assets/`, `Packages/`, `ProjectSettings/`). Add exactly this folder in Unity Hub |
| Integration branch | `develop` (never `main`) |
| Playable scene (primary) | `Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity` |
| Visual Shell (roadside night, view only) | `Assets/TramChanh/Scenes/ACCEL01/SCN_AccelRoadsideEnvironment.unity` — only on `feature/ACCEL-01-scene` (PR #18, draft) and on `preview/ACCEL-01-visual-shell` |
| Old test scene | `Assets/TramChanh/Scenes/Test/SCN_TeaRackPickupTest.unity` — gray prototype by design; never judge the project by it |

**Opening the wrong scene looks like "the project did not update".** `SCN_TeaRackPickupTest` is a gray test scene on every branch. Unity re-opens the last scene it had, so it keeps showing that one until you open another. In the Editor use the menu **Tram Chanh ▸ Scenes** (added by this guide's PR) to open the right one.

`ProjectSettings/` (except `ProjectVersion.txt`) is not tracked. On open, `TramChanhProjectSetup` (an `[InitializeOnLoad]` script) applies layers, Linear colour space, the Input System setting and creates and assigns the URP asset. On the first open it logs "[TramChanh] Project setup applied"; wait for the import to finish before judging the lighting.

## First time: get the script without touching your clone

Your clone does not have the script yet. Copy it to a folder **outside** the repo and point it at your clone:

```bash
cd /path/to/your/clone
git fetch origin +chore/UNITY-LOCAL-SYNC:refs/remotes/origin/chore/UNITY-LOCAL-SYNC
git show origin/chore/UNITY-LOCAL-SYNC:Automation/unity_local_sync.sh > ~/unity_local_sync.sh
export TRAMCHANH_PROJECT=/path/to/your/clone
bash ~/unity_local_sync.sh diagnose
```

Once your clone is on a branch that contains it, `bash Automation/unity_local_sync.sh ...` works without the variable.

## Check what you have (read-only)

```bash
cd /path/to/your/clone
bash Automation/unity_local_sync.sh diagnose   # folder, branch, commit, dirty files, last scene Unity opened, worktrees
bash Automation/unity_local_sync.sh scan       # every Unity project under your home folder (finds copies, e.g. under "Lưu trữ")
bash Automation/unity_local_sync.sh open-info  # which folder and scene to open
```

If `scan` lists more than one copy of the game, only the copy whose `diagnose` output matches the Unity Hub entry is the project Unity is showing. Hub shows each project's path.

## Update safely

1. **Quit Unity** (the script refuses while the project is open).
2. Run:
   ```bash
   bash Automation/unity_local_sync.sh update develop          # the integrated game
   bash Automation/unity_local_sync.sh update preview/ACCEL-01-visual-shell   # the Visual Shell preview (see below)
   ```
   It always takes a backup first (`~/TramChanh-backups/<time>/`: untracked files archive, tracked-changes patch, all-refs bundle, status, worktree list), then `git fetch`, `git switch`, and `git merge --ff-only`.
3. It never resets, cleans, force-checks-out, drops stashes, deletes branches or removes worktrees. If tracked files are modified it stops and tells you to commit them to a `wip/` branch first. If an untracked file would be overwritten, Git itself refuses and nothing changes; your backup is safe.
4. Reopen the project in Unity Hub, wait for the import, then **Tram Chanh ▸ Scenes ▸ Open …**.

Never run `git reset --hard`, `git clean -fd`, `git checkout -- .` or `git stash drop` on a clone that has Codex work in it. Unfinished agent work lives in untracked files and in **worktrees**; list them with `git worktree list` and leave them alone (a worktree is an independent folder; deleting a branch does not delete its work, but `git worktree remove --force` does).

## Verify the commit

```bash
git log -1 --format='%h %s'      # expected on develop: the newest merged DRINK-WAVE / ACCEL-01 commit
git fetch origin && git rev-list --left-right --count origin/develop...HEAD   # "0 0" = identical to GitHub
```

## What each branch contains

| Branch | Content |
|---|---|
| `develop` | Playable drink flow over the gray blockout (`SCN_DrinkWave`): Lobby intake → drink steps → Ready → pickup; HUD prompts, order-entry modal |
| `feature/ACCEL-01-orders` (PR #17, draft, approved) | adds delivery and completion |
| `feature/ACCEL-01-cake` (PR #16, draft) | adds the cake station |
| `feature/ACCEL-01-scene` (PR #18, draft) | adds the **roadside night environment** scene (view only: a camera, no player) |
| `preview/ACCEL-01-visual-shell` | `develop` + only the environment files from PR #18, so you can see the Visual Shell now without merging anything |

The integrated scene that combines the environment with the drink and cake stations and a full HUD is the integration wave's job and is **not on GitHub yet**; if Codex produced it locally, it exists only in that agent's workspace until pushed.

## Keeping agents and the Editor in step

- One clone is "the Unity clone". Agents work in their own worktrees/branches (`feature/*`, `wave/*`); only `develop` is merged into your Unity clone.
- Before an agent pushes, ask for the branch name and head commit; before you open Unity, run `update`.
- After any `update`, check the Console is empty of errors, then open the scene from the menu.
