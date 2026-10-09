#!/usr/bin/env bash
# Tram Chanh - MAIN-105 Unity validation runner for macOS (bash 3.2 compatible).
#
#   Automation/unity_validate.sh [all|static|qa|editmode|playmode ...]   (default: all)
#
# Stages (in this order, each only when selected):
#   static    python3 Automation/static_preflight.py (skipped when python3 is unavailable)
#   qa        Unity -batchmode -nographics -quit -executeMethod TramChanh.EditorTools.TramChanhValidationMenu.RunAllBatch
#   editmode  Unity -batchmode -nographics -runTests -testPlatform EditMode -testResults ...
#   playmode  Unity -batchmode -runTests -testPlatform PlayMode -testResults ...  (graphics on; PLAYMODE_NOGRAPHICS=1 adds -nographics)
#
# Output: ~/TramChanh-validation/<timestamp>/ (override with TRAMCHANH_VALIDATION_DIR):
#   SUMMARY.txt, static_preflight.txt, qa_runall.log, qa_report.txt,
#   editmode-results.xml, editmode.log, playmode-results.xml, playmode.log, git-before.txt, git-after.txt
#
# Environment:
#   UNITY_PATH          Unity executable (default: Unity Hub install of the pinned version below)
#   TRAMCHANH_PROJECT   project folder (default: the folder above this script)
#
# Guarantees: it never runs a git command that changes anything (only rev-parse/status/log), never
# deletes files, and needs no licence secrets: it uses the Editor you already activated in Unity Hub.
# It refuses to run while the project is open in the Editor (Temp/UnityLockfile).
set -u
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="${TRAMCHANH_PROJECT:-$(cd "$SCRIPT_DIR/.." && pwd)}"
PINNED_UNITY="6000.6.0f1"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$PINNED_UNITY/Unity.app/Contents/MacOS/Unity}"
QA_METHOD="TramChanh.EditorTools.TramChanhValidationMenu.RunAllBatch"
STAMP="$(date +%Y%m%d-%H%M%S)"
OUT="${TRAMCHANH_VALIDATION_DIR:-$HOME/TramChanh-validation}/$STAMP"

say() { printf '%s\n' "$*"; }
die() { printf 'ERROR: %s\n' "$*" >&2; exit 2; }

# ---------------------------------------------------------------- arguments
RUN_STATIC=0; RUN_QA=0; RUN_EDIT=0; RUN_PLAY=0
if [ "$#" -eq 0 ]; then set -- all; fi
for arg in "$@"; do
  case "$arg" in
    all) RUN_STATIC=1; RUN_QA=1; RUN_EDIT=1; RUN_PLAY=1 ;;
    static) RUN_STATIC=1 ;;
    qa) RUN_QA=1 ;;
    editmode) RUN_EDIT=1 ;;
    playmode) RUN_PLAY=1 ;;
    -h|--help) sed -n '2,24p' "$0"; exit 0 ;;
    *) die "unknown stage '$arg' (use all, static, qa, editmode, playmode)" ;;
  esac
done

# ---------------------------------------------------------------- pre-checks
[ -f "$ROOT/ProjectSettings/ProjectVersion.txt" ] || die "$ROOT is not a Unity project (ProjectSettings/ProjectVersion.txt is missing)."
PROJECT_VERSION="$(sed -n 's/^m_EditorVersion: *//p' "$ROOT/ProjectSettings/ProjectVersion.txt" | head -n 1)"
[ "$PROJECT_VERSION" = "$PINNED_UNITY" ] || say "WARNING: project pins Unity $PROJECT_VERSION, this script expects $PINNED_UNITY."

NEED_UNITY=$((RUN_QA + RUN_EDIT + RUN_PLAY))
if [ "$NEED_UNITY" -gt 0 ]; then
  [ -x "$UNITY" ] || die "Unity not found at $UNITY. Install $PINNED_UNITY with Unity Hub or set UNITY_PATH=/path/to/Unity.app/Contents/MacOS/Unity."
  case "$UNITY" in *"$PINNED_UNITY"*) ;; *) say "WARNING: UNITY_PATH does not mention $PINNED_UNITY: $UNITY" ;; esac
  if [ -f "$ROOT/Temp/UnityLockfile" ]; then
    die "The project is open in Unity (Temp/UnityLockfile exists). Close the Editor first; batch mode cannot share the project."
  fi
  if ps -axo command 2>/dev/null | grep -i "Unity.app/Contents/MacOS/Unity" | grep -F -- "$ROOT" | grep -v grep >/dev/null 2>&1; then
    die "A Unity process is using $ROOT. Close it first."
  fi
fi

mkdir -p "$OUT" || die "cannot create $OUT"
SUMMARY="$OUT/SUMMARY.txt"
: > "$SUMMARY"
note() { say "$*"; printf '%s\n' "$*" >> "$SUMMARY"; }

git_snapshot() {
  # Read-only: records the commit and working-tree state; never changes Git.
  {
    printf 'branch: %s\n' "$(git -C "$ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null)"
    printf 'commit: %s\n' "$(git -C "$ROOT" rev-parse HEAD 2>/dev/null)"
    printf 'status --porcelain:\n'
    git -C "$ROOT" status --porcelain 2>/dev/null
  } > "$1"
}
git_snapshot "$OUT/git-before.txt"

note "Tram Chanh validation $STAMP"
note "project: $ROOT"
note "commit:  $(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null) ($(git -C "$ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null))"
note "unity:   $UNITY"
note "output:  $OUT"
note ""

OVERALL=0
fail_overall() { OVERALL=1; }

# ---------------------------------------------------------------- NUnit XML summary (no python needed)
nunit_summary() {
  # $1 = results xml, $2 = label
  local xml="$1" label="$2" line total passed failed skipped inconclusive result
  if [ ! -s "$xml" ]; then note "$label: NO RESULTS FILE ($xml) - the run did not finish; see the log."; return 1; fi
  line="$(grep -m 1 '<test-run ' "$xml")"
  attr() { printf '%s' "$line" | sed -n "s/.* $1=\"\([^\"]*\)\".*/\1/p"; }
  total="$(attr total)"; passed="$(attr passed)"; failed="$(attr failed)"
  skipped="$(attr skipped)"; inconclusive="$(attr inconclusive)"; result="$(attr result)"
  note "$label: result=$result total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"
  # Failed test cases (fullname), one per line.
  grep '<test-case ' "$xml" | grep 'result="Failed"' | sed -n 's/.* fullname="\([^"]*\)".*/    FAILED \1/p' | tee -a "$SUMMARY"
  case "$result" in Passed*) return 0 ;; *) return 1 ;; esac
}

# ---------------------------------------------------------------- stage: static
if [ "$RUN_STATIC" -eq 1 ]; then
  if command -v python3 >/dev/null 2>&1 && python3 -c 'import sys' >/dev/null 2>&1; then
    python3 "$ROOT/Automation/static_preflight.py" --root "$ROOT" > "$OUT/static_preflight.txt" 2>&1
    rc=$?
    note "static pre-flight (not Unity): exit $rc - $(grep '^STATIC PREFLIGHT:' "$OUT/static_preflight.txt" | tail -n 1)"
    [ "$rc" -eq 0 ] || fail_overall
  else
    note "static pre-flight: SKIPPED (python3 not available)"
  fi
fi

# ---------------------------------------------------------------- stage: qa
if [ "$RUN_QA" -eq 1 ]; then
  say "Running $QA_METHOD (batch mode)..."
  "$UNITY" -batchmode -nographics -quit -projectPath "$ROOT" -executeMethod "$QA_METHOD" -logFile "$OUT/qa_runall.log"
  rc=$?
  if [ -f "$ROOT/Temp/TramChanhQA/report.txt" ]; then
    cp "$ROOT/Temp/TramChanhQA/report.txt" "$OUT/qa_report.txt"
  else
    # Unity can delete Temp/ when a batch run quits; the same report is printed to the log.
    sed -n '/^Tram Chanh QA - /,/^SUMMARY:/p' "$OUT/qa_runall.log" > "$OUT/qa_report.txt"
    [ -s "$OUT/qa_report.txt" ] || rm -f "$OUT/qa_report.txt"
  fi
  if [ "$rc" -eq 0 ]; then
    note "QA menu RunAllBatch: exit 0 (no validator errors)"
  else
    note "QA menu RunAllBatch: exit $rc (1 = validator errors; other = Unity failed, e.g. compile errors or method not found)"
    fail_overall
  fi
  if [ -f "$OUT/qa_report.txt" ]; then
    grep '^SUMMARY:' "$OUT/qa_report.txt" | sed 's/^/    /' | tee -a "$SUMMARY"
    grep -E '^  ERROR:' "$OUT/qa_report.txt" | head -n 40 | sed 's/^/  /' | tee -a "$SUMMARY"
  else
    note "    no Temp/TramChanhQA/report.txt was written; see $OUT/qa_runall.log"
    grep -E 'error CS[0-9]+' "$OUT/qa_runall.log" 2>/dev/null | head -n 20 | sed 's/^/    /' | tee -a "$SUMMARY"
  fi
fi

# ---------------------------------------------------------------- stage: tests
run_tests() {
  # $1 = EditMode|PlayMode, $2 = file prefix, $3 = extra flag (may be empty)
  local platform="$1" prefix="$2" extra="$3" rc
  say "Running $platform tests (batch mode)..."
  # -runTests quits on its own; -quit must not be passed with it.
  if [ -n "$extra" ]; then
    "$UNITY" -batchmode "$extra" -projectPath "$ROOT" -runTests -testPlatform "$platform" \
      -testResults "$OUT/$prefix-results.xml" -logFile "$OUT/$prefix.log"
  else
    "$UNITY" -batchmode -projectPath "$ROOT" -runTests -testPlatform "$platform" \
      -testResults "$OUT/$prefix-results.xml" -logFile "$OUT/$prefix.log"
  fi
  rc=$?
  # Unity Test Framework exit codes: 0 all passed, 2 some failed, 3 run error; anything else = Editor failure.
  note "$platform tests: Unity exit $rc"
  nunit_summary "$OUT/$prefix-results.xml" "$platform" || fail_overall
  [ "$rc" -eq 0 ] || fail_overall
}

if [ "$RUN_EDIT" -eq 1 ]; then run_tests EditMode editmode -nographics; fi
if [ "$RUN_PLAY" -eq 1 ]; then
  if [ "${PLAYMODE_NOGRAPHICS:-0}" = "1" ]; then run_tests PlayMode playmode -nographics; else run_tests PlayMode playmode ""; fi
fi

# ---------------------------------------------------------------- git state check (read-only)
git_snapshot "$OUT/git-after.txt"
if ! diff -q "$OUT/git-before.txt" "$OUT/git-after.txt" >/dev/null 2>&1; then
  note ""
  note "NOTE: the Git working tree changed while Unity ran (Unity itself can rewrite files such as"
  note "      Packages/packages-lock.json). Nothing was reverted. Compare:"
  note "      diff $OUT/git-before.txt $OUT/git-after.txt"
fi

note ""
if [ "$OVERALL" -eq 0 ]; then
  note "OVERALL: PASS (Unity $PINNED_UNITY batch run on this Mac)"
else
  note "OVERALL: FAIL - read $SUMMARY and the logs in $OUT"
fi
exit "$OVERALL"
