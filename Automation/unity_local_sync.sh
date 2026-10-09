#!/usr/bin/env bash
# Tram Chanh - safe local Unity project check, backup and update (macOS bash 3.2 compatible).
#
#   Automation/unity_local_sync.sh [diagnose]        read-only report of this clone (default)
#   Automation/unity_local_sync.sh scan              list every Unity project found under $HOME
#   Automation/unity_local_sync.sh backup            copy local work to ~/TramChanh-backups/<time>/
#   Automation/unity_local_sync.sh update [branch]   backup, then fast-forward to origin/<branch> (default: develop)
#   Automation/unity_local_sync.sh open-info         print the folder and scene to open
#
# Guarantees: it never runs reset, clean, stash drop, checkout -f, branch -D, worktree remove or rm.
# `update` refuses while Unity has the project open, and refuses when tracked files are modified.
# Untracked files are kept: git itself aborts a switch that would overwrite one, and a backup is taken first.
set -u
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PINNED_UNITY="6000.6.0f1"
PRIMARY_SCENE="Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity"
PREVIEW_SCENE="Assets/TramChanh/Scenes/ACCEL01/SCN_AccelRoadsideEnvironment.unity"
TEST_SCENE="Assets/TramChanh/Scenes/Test/SCN_TeaRackPickupTest.unity"
BACKUP_ROOT="${TRAMCHANH_BACKUP_DIR:-$HOME/TramChanh-backups}"

say() { printf '%s\n' "$*"; }
die() { printf 'ERROR: %s\n' "$*" >&2; exit 2; }

git_in() { git -C "$ROOT" "$@"; }

require_project() {
  [ -f "$ROOT/ProjectSettings/ProjectVersion.txt" ] || die "$ROOT is not a Unity project (ProjectSettings/ProjectVersion.txt is missing)."
  git_in rev-parse --is-inside-work-tree >/dev/null 2>&1 || die "$ROOT is not a Git working tree."
}

unity_version() { sed -n 's/^m_EditorVersion: *//p' "$ROOT/ProjectSettings/ProjectVersion.txt" | head -n 1; }

unity_open() {
  # True when Unity appears to have this project open.
  if [ -f "$ROOT/Temp/UnityLockfile" ]; then return 0; fi
  if ps -axo command 2>/dev/null | grep -i "unity" | grep -F -- "$ROOT" | grep -v grep >/dev/null 2>&1; then return 0; fi
  return 1
}

last_scenes() {
  local f="$ROOT/Library/LastSceneManagerSetup.txt"
  if [ -f "$f" ]; then sed -n 's/^ *path: *//p' "$f"; else say "(Unity has not recorded a scene here yet)"; fi
}

diagnose() {
  require_project
  local branch head tracked untracked
  branch="$(git_in rev-parse --abbrev-ref HEAD 2>/dev/null)"
  head="$(git_in log -1 --format='%h %s' 2>/dev/null)"
  tracked="$(git_in status --porcelain 2>/dev/null | grep -v '^??' | wc -l | tr -d ' ')"
  untracked="$(git_in ls-files --others --exclude-standard 2>/dev/null | wc -l | tr -d ' ')"
  say "== Tram Chanh Unity project report =="
  say "Unity project folder : $ROOT   (open THIS folder in Unity Hub)"
  say "Git root             : $(git_in rev-parse --show-toplevel)"
  say "Unity version        : $(unity_version)   (pinned: $PINNED_UNITY)"
  [ "$(unity_version)" = "$PINNED_UNITY" ] || say "  WARNING: this is not the pinned Unity version."
  say "Branch               : $branch"
  say "Commit               : $head"
  say "Modified tracked     : $tracked file(s)"
  say "Untracked (not ignored): $untracked file(s)  (kept safe; never deleted by this script)"
  if unity_open; then say "Unity                : appears to have this project OPEN (close it before update)"; else say "Unity                : not open on this folder"; fi
  say "Scene(s) Unity last opened here:"
  last_scenes | sed 's/^/  - /'
  say "Scenes present on this branch:"
  for s in "$PRIMARY_SCENE" "$PREVIEW_SCENE" "$TEST_SCENE"; do
    if [ -f "$ROOT/$s" ]; then say "  [present] $s"; else say "  [absent ] $s"; fi
  done
  say "Remote refs (after the last fetch):"
  git_in for-each-ref --format='  %(refname:short) %(objectname:short)' refs/remotes/origin/develop refs/remotes/origin/main refs/remotes/origin/preview 2>/dev/null
  say "Ahead/behind origin/develop:"
  git_in rev-list --left-right --count "origin/develop...HEAD" 2>/dev/null | awk '{print "  behind " $1 ", ahead " $2}'
  say "Worktrees:"
  git_in worktree list | sed 's/^/  /'
  say "Stashes: $(git_in stash list | wc -l | tr -d ' ')"
  say "Files that mark this as the OLD prototype scene if it is the one open: $TEST_SCENE"
}

scan() {
  say "Unity projects under $HOME (depth <= 7, this can take a minute):"
  find "$HOME" -maxdepth 7 \( -name Library -o -name node_modules -o -name .Trash -o -name Applications -o -name .git \) -prune -o \
       -path '*/ProjectSettings/ProjectVersion.txt' -print 2>/dev/null | while IFS= read -r v; do
    proj="$(dirname "$(dirname "$v")")"
    br="$(git -C "$proj" rev-parse --abbrev-ref HEAD 2>/dev/null || echo '-')"
    hd="$(git -C "$proj" log -1 --format=%h 2>/dev/null || echo '-')"
    ver="$(sed -n 's/^m_EditorVersion: *//p' "$v" | head -n 1)"
    say "  $proj   [branch $br @ $hd]   [Unity $ver]"
  done
}

backup_one() {
  # backup_one <git-dir> <destination>
  local dir="$1" dest="$2"
  mkdir -p "$dest"
  git -C "$dir" status --porcelain=v1 -uall > "$dest/status.txt" 2>&1
  git -C "$dir" branch -a -vv > "$dest/branches.txt" 2>&1
  git -C "$dir" log --all --oneline --decorate -60 > "$dest/log.txt" 2>&1
  git -C "$dir" stash list > "$dest/stashes.txt" 2>&1
  git -C "$dir" diff HEAD --binary > "$dest/tracked-changes.patch" 2>/dev/null
  git -C "$dir" ls-files --others --exclude-standard -z | (cd "$dir" && tar -czf "$dest/untracked.tgz" --null -T - 2>/dev/null)
  git -C "$dir" bundle create "$dest/all-refs.bundle" --all >/dev/null 2>&1 || say "  (bundle skipped for $dir)"
}

backup() {
  require_project
  local stamp dest n
  stamp="$(date +%Y%m%d-%H%M%S)"
  dest="$BACKUP_ROOT/$stamp"
  mkdir -p "$dest" || die "cannot create $dest"
  git_in worktree list --porcelain > "$dest/worktrees.txt"
  backup_one "$ROOT" "$dest/main-clone"
  n=0
  git_in worktree list --porcelain | sed -n 's/^worktree //p' | while IFS= read -r wt; do
    [ "$wt" = "$ROOT" ] && continue
    n=$((n + 1))
    backup_one "$wt" "$dest/worktree-$(basename "$wt")"
  done
  say "Backup written to: $dest"
  say "  untracked files archive : $dest/main-clone/untracked.tgz"
  say "  tracked changes patch   : $dest/main-clone/tracked-changes.patch"
  say "  every git ref           : $dest/main-clone/all-refs.bundle"
}

update() {
  require_project
  local target="${1:-develop}" cur
  case "$target" in
    main|master) die "Refusing to update to '$target'. Use develop (or a preview branch).";;
  esac
  if unity_open; then die "Unity has this project open. Close Unity, then run this again."; fi
  backup
  if [ "$(git_in status --porcelain | grep -v '^??' | wc -l | tr -d ' ')" != "0" ]; then
    die "Tracked files are modified. Commit them first (git switch -c wip/<name>; git add -A; git commit) - nothing was changed. The backup above is safe."
  fi
  git_in fetch origin --prune || die "git fetch failed (network?)."
  git_in rev-parse --verify --quiet "origin/$target" >/dev/null || die "origin/$target does not exist."
  cur="$(git_in rev-parse --abbrev-ref HEAD)"
  if [ "$cur" != "$target" ]; then
    if git_in rev-parse --verify --quiet "refs/heads/$target" >/dev/null; then
      git_in switch "$target" || die "git refused to switch (it would overwrite local files). Nothing was changed; see the backup above."
    else
      git_in switch -c "$target" --track "origin/$target" || die "git refused to create $target. Nothing was changed."
    fi
  fi
  git_in merge --ff-only "origin/$target" || die "$target has local commits that are not on origin/$target (diverged). Nothing was overwritten; push or rename your branch first."
  say ""
  say "Updated: $(git_in rev-parse --abbrev-ref HEAD) @ $(git_in log -1 --format='%h %s')"
  open_info
}

open_info() {
  require_project
  say "Open in Unity Hub (Add > Add project from disk): $ROOT"
  say "Unity editor: $(unity_version)"
  if [ -f "$ROOT/$PREVIEW_SCENE" ]; then say "Visual Shell preview scene : $PREVIEW_SCENE   (Tram Chanh > Scenes menu)"; fi
  say "Playable scene           : $PRIMARY_SCENE   (Tram Chanh > Scenes menu)"
  say "Old test scene (do not judge the project by it): $TEST_SCENE"
}

case "${1:-diagnose}" in
  diagnose) diagnose;;
  scan) scan;;
  backup) backup;;
  update) shift; update "${1:-develop}";;
  open-info) open_info;;
  -h|--help|help) sed -n '2,12p' "${BASH_SOURCE[0]}";;
  *) die "unknown command '$1' (try: diagnose, scan, backup, update, open-info)";;
esac
