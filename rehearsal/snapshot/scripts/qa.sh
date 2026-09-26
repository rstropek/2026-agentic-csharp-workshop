#!/usr/bin/env bash
# Runs the full QA baseline (formatting, linting/analyzers, builds, all tests) for .NET and Angular.
# See tech-docs/testing.md (QA script) and tech-docs/code-quality.md.
#
# Usage: scripts/qa.sh [--skip-e2e]
#
# Output: one section per tool with PASS/FAIL/SKIP. Output of passing sections is only written to
# .qa-logs/, output of failing sections is printed. A summary follows at the end.
# Exit code: 0 if no section failed, 1 otherwise.

set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$ROOT/.qa-logs"
E2E_PORT=5200

SKIP_E2E=false
for arg in "$@"; do
  case "$arg" in
    --skip-e2e) SKIP_E2E=true ;;
    -h | --help)
      sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    *)
      echo "Unknown argument: $arg (see --help)" >&2
      exit 2
      ;;
  esac
done

# Plain output for agents and CI logs.
export NO_COLOR=1 DOTNET_NOLOGO=1

rm -rf "$LOG_DIR"
mkdir -p "$LOG_DIR"

SUMMARY=()
FAILED=0
PASSED=0
SKIPPED=0
SECTION=0

# run_section <name> <directory relative to repo root> <command...>
# Sets LAST_STATUS to PASS or FAIL.
run_section() {
  local name="$1" dir="$2"
  shift 2
  SECTION=$((SECTION + 1))
  local slug log start duration
  slug="$(echo "$name" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed -E 's/^-+|-+$//g')"
  log="$LOG_DIR/$(printf '%02d' "$SECTION")-$slug.log"

  echo "=== [$SECTION] $name ==="
  echo "\$ (cd $dir && $*)"
  start=$SECONDS
  (cd "$ROOT/$dir" && "$@") >"$log" 2>&1
  local status=$?
  duration=$((SECONDS - start))

  if [ "$status" -eq 0 ]; then
    LAST_STATUS=PASS
    PASSED=$((PASSED + 1))
  else
    LAST_STATUS=FAIL
    FAILED=$((FAILED + 1))
    echo "--- output (exit code $status) ---"
    cat "$log"
  fi
  echo "--- $LAST_STATUS: $name (${duration}s, log: ${log#"$ROOT"/})"
  echo
  SUMMARY+=("$(printf '%-5s %-28s %5ss' "$LAST_STATUS" "$name" "$duration")")
}

# skip_section <name> <reason>
skip_section() {
  SECTION=$((SECTION + 1))
  SKIPPED=$((SKIPPED + 1))
  echo "=== [$SECTION] $1 ==="
  echo "--- SKIP: $1 ($2)"
  echo
  SUMMARY+=("$(printf '%-5s %-28s %s' SKIP "$1" "($2)")")
}

# Runs `npm run e2e`, but fails if a server already listens on the E2E port: outside of CI, Playwright
# would reuse it and test an outdated build.
npm_run_e2e_on_free_port() {
  if command -v lsof >/dev/null && lsof -iTCP:"$E2E_PORT" -sTCP:LISTEN >/dev/null 2>&1; then
    echo "Port $E2E_PORT is in use (stale E2E server?). Stop it, then rerun:"
    lsof -iTCP:"$E2E_PORT" -sTCP:LISTEN
    return 1
  fi
  npm run e2e
}

# .NET
run_section "dotnet format" . dotnet format --verify-no-changes
run_section ".NET build" . dotnet build --no-incremental -tl:off -v:q -nologo
DOTNET_BUILD=$LAST_STATUS
if [ "$DOTNET_BUILD" = PASS ]; then
  run_section ".NET unit tests" . dotnet test --no-build --no-ansi --no-progress --filter-not-trait "Category=Integration"
  run_section ".NET integration tests" . dotnet test --no-build --no-ansi --no-progress --filter-trait "Category=Integration"
else
  skip_section ".NET unit tests" ".NET build failed"
  skip_section ".NET integration tests" ".NET build failed"
fi

# Angular
run_section "Biome (lint + format)" GraderWeb npm run check
run_section "Angular build" GraderWeb npm run build
ANGULAR_BUILD=$LAST_STATUS
run_section "Angular component tests" GraderWeb npm test -- --watch=false

# E2E (builds and publishes the app itself, see GraderWeb/playwright.config.ts)
if [ "$SKIP_E2E" = true ]; then
  skip_section "E2E tests (Playwright)" "--skip-e2e"
elif [ "$DOTNET_BUILD" != PASS ] || [ "$ANGULAR_BUILD" != PASS ]; then
  skip_section "E2E tests (Playwright)" ".NET or Angular build failed"
else
  run_section "E2E tests (Playwright)" GraderWeb npm_run_e2e_on_free_port
fi

echo "=== SUMMARY ==="
printf '%s\n' "${SUMMARY[@]}"
echo
if [ "$FAILED" -eq 0 ]; then
  echo "QA RESULT: PASS ($PASSED passed, $SKIPPED skipped)"
  exit 0
fi
echo "QA RESULT: FAIL ($FAILED failed, $PASSED passed, $SKIPPED skipped). Logs: .qa-logs/"
exit 1
