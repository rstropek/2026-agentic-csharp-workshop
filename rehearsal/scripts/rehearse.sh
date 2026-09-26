#!/usr/bin/env bash
# Rehearse the storyboard (run from rehearsal/). Usage: rehearse.sh <run-suffix> [first-step] [last-step]
# Steps: 1 2 3 4 5 6 7a 7b 8a 8b 8c 8d 9a 9b 9c 10 11a 11b 11c
# Step 1 starts from scratch; later steps start from the tag of the last committing step before them.
# After each step: tag step<id> in grader/ (steps that commit), snapshot of that state in snapshot/, gzipped logs,
# and a commit of both in the basta2026 repo (SNAPSHOT_COMMIT=0 disables the commit).
set -euo pipefail
R=$(pwd)
ROOT=$(cd .. && pwd)
RUN=${1:-run}
FIRST=${2:-1}
LAST=${3:-11c}
STEPS=(1 2 3 4 5 6 7a 7b 8a 8b 8c 8d 9a 9b 9c 10 11a 11b 11c)
TAGGED=" 1 2 3 4 5 6 7a 7b 8a 8b 9a 10 11a 11b 11c "  # steps whose result is a commit on main
TRAILER="Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01EGiZYXpx4ZuG7Vxwyuw7f6"
is_tagged() { [[ "$TAGGED" == *" $1 "* ]]; }

check() {  # verify step result: on main, clean tree, then tag
  cd "$R/grader"
  echo "--- after $1: branch=$(git branch --show-current) head=$(git log --oneline -1)"
  git status --short | head
  [ "$(git branch --show-current)" = main ] && [ -z "$(git status --porcelain)" ] && git tag -f "$1" >/dev/null && echo "OK $1" || { echo "CHECK FAILED $1"; exit 1; }
  cd "$R"
}
agent() { ./scripts/run-agent.sh "prompts/step$1.md" "step$1-$RUN"; }

# Development API with demo data (attendees: second terminal + Settings -> Reset demo data)
stop_api() { lsof -ti tcp:5100 -sTCP:LISTEN | xargs kill 2>/dev/null || true; }
start_api() {  # (re)start the API from the current code
  stop_api
  (cd "$R/grader" && dotnet run --project GraderApi >> "$R/logs/api-$RUN.log" 2>&1 &)
  for _ in $(seq 1 90); do api_up && return; sleep 1; done
  echo "API did not start"; exit 1
}
api_up() { [ "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:5100/openapi/v1.json)" = 200 ]; }
reset_demo_data() {  # POST to the demo data endpoint (path taken from the OpenAPI spec, the agent names it)
  local path; path=$(curl -fs http://localhost:5100/openapi/v1.json | jq -r '.paths | to_entries[] | select((.key | test("demo"; "i")) and .value.post) | .key' | head -1)
  curl -fsS -o /dev/null -X POST "http://localhost:5100$path" && echo "demo data reset ($path)"
}
trap stop_api EXIT

MCP_ARGS=""
mcp_config() {  # MCP config with `grader mcp` for claude -p. Start the built dll, not `dotnet run`
  # (its build output would pollute stdout = the JSON-RPC channel)
  (cd "$R/grader" && dotnet build GraderCli -v q -nologo > "$R/logs/build-cli-$RUN.log" 2>&1)
  local dll; dll=$(cd "$R/grader" && dotnet msbuild GraderCli -nologo -getProperty:TargetPath)
  printf '{ "mcpServers": { "grader": { "type": "stdio", "command": "dotnet", "args": ["%s", "mcp"] } } }\n' "$dll" > "$R/logs/mcp-$RUN.json"
  echo "MCP config: $R/logs/mcp-$RUN.json ($dll)"
  MCP_ARGS="--mcp-config $R/logs/mcp-$RUN.json --strict-mcp-config --disallowedTools Bash"
}
openrouter_key() { . "$R/openrouter.env"; }  # export OpenRouter__ApiKey=...; never committed

run_step() {
  case $1 in
    1) # Step 1: deterministic skeleton script
      rm -rf grader && ./scripts/step1-setup.sh > "logs/step1-$RUN.log" 2>&1 && check step1 ;;
    2) # Step 2: skills script, then the AGENTS.md prompt
      (cd grader && "$R/scripts/step2-skills.sh") > "logs/step2-skills-$RUN.log" 2>&1
      agent 2 && check step2
      [ ! -e grader/CLAUDE.md ] && echo "OK no CLAUDE.md" || echo "WARN CLAUDE.md created" ;;
    3|4) agent "$1" && check "step$1" ;;
    5) # attendees copy the spec into ./spec before running the prompt
      mkdir -p grader/spec && cp ../2026-09-storyboard_erdiagram.md grader/spec/
      agent 5 && check step5 ;;
    6) # attendees copy the architecture article into ./tech-docs before running the prompt
      cp ../web-api.md grader/tech-docs/web-api.md
      agent 6 && check step6 ;;
    7a) # attendees copy the design into ./spec/design; the agent checks the UI in Chrome (Claude in Chrome)
      mkdir -p grader/spec && rm -rf grader/spec/design && cp -r ../design grader/spec/design
      CLAUDE_EXTRA_ARGS=--chrome agent 7a && check step7a ;;
    7b) CLAUDE_EXTRA_ARGS=--chrome agent 7b && check step7b ;;
    8a) stop_api; agent 8a && check step8a ;;  # the agent needs no running API
    8b) start_api && reset_demo_data; agent 8b && check step8b ;;
    8c) # teacher prompts with CLI + skill: no code changes, no commit
      start_api && reset_demo_data; agent 8c; echo "--- after 8c (expected: no changes)"; git -C grader status --short ;;
    8d) api_up || start_api
      agent 8d; echo "--- after 8d (expected: no changes)"; git -C grader status --short ;;
    9a) stop_api; agent 9a && check step9a ;;
    9b) # teacher prompts of 8c/8d via the MCP tools only (no shell)
      start_api && reset_demo_data; mcp_config
      CLAUDE_EXTRA_ARGS="$MCP_ARGS" agent 9b; echo "--- after 9b (expected: no changes)"; git -C grader status --short ;;
    9c) api_up || start_api
      [ -n "$MCP_ARGS" ] || mcp_config
      CLAUDE_EXTRA_ARGS="$MCP_ARGS" agent 9c; echo "--- after 9c (expected: no changes)"; git -C grader status --short ;;
    10) stop_api; agent 10 && check step10 ;;
    11a) stop_api; agent 11a && check step11a ;;
    11b) # attendees copy the assistant design into ./spec/design, commit it and provide the OpenRouter key
      # (environment variable OpenRouter__ApiKey or `dotnet user-secrets set OpenRouter:ApiKey ...` in GraderApi)
      cp ../design-assistant/*.png grader/spec/design/ && cp ../design-assistant/readme.md grader/spec/design/assistant.md
      (cd grader && git add spec/design && git commit -qm "Add assistant design")
      openrouter_key; agent 11b && check step11b ;;
    11c) # development API with demo data and the assistant (attendees: second terminal)
      openrouter_key; start_api && reset_demo_data
      CLAUDE_EXTRA_ARGS=--chrome agent 11c && check step11c ;;
  esac
}

leak_check() (  # the OpenRouter key must never end up in the snapshot or the logs we commit
  . "$R/openrouter.env"
  [ -n "${OpenRouter__ApiKey:-}" ] || exit 0
  ! grep -rqF -- "$OpenRouter__ApiKey" "$R/snapshot" 2>/dev/null || exit 1
  for f in "$R"/logs/*.gz "$R"/logs/*.out; do [ -e "$f" ] || continue
    ! { gzip -dcf "$f" | grep -qF -- "$OpenRouter__ApiKey"; } || exit 1
  done
)
record() {  # snapshot + gzipped logs of step $1, committed in basta2026
  local id=$1 f
  for f in "logs/step$id-$RUN.jsonl" "logs/step$id-$RUN.log" "logs/step$id-skills-$RUN.log"; do
    if [ -e "$f" ]; then gzip -kf "$f"; fi
  done
  if is_tagged "$id"; then
    rm -rf snapshot && mkdir snapshot && git -C grader archive "step$id" | tar -x -C snapshot
  fi
  leak_check || { echo "KEY LEAK in snapshot/logs - not committed"; exit 1; }
  [ "${SNAPSHOT_COMMIT:-1}" = 1 ] || return 0
  cd "$ROOT"
  git add -A -f rehearsal/snapshot
  for f in rehearsal/logs/*.gz rehearsal/logs/*.out; do if [ -e "$f" ]; then git add -f "$f"; fi; done
  git add -A rehearsal/prompts rehearsal/scripts
  if git commit -q -m "Rehearsal $RUN: step $id" -m "$TRAILER" -- rehearsal/snapshot rehearsal/logs rehearsal/prompts rehearsal/scripts; then
    echo "committed step $id"
  else
    echo "nothing to commit for step $id"
  fi
  cd "$R"
}

# Start: step 1 from scratch, otherwise reset grader/ to the last committing step before FIRST
BASE=""
for s in "${STEPS[@]}"; do [ "$s" = "$FIRST" ] && break; is_tagged "$s" && BASE=$s; done
if [ -n "$BASE" ]; then
  cd grader
  git checkout -q main && git reset -q --hard "step$BASE" && git clean -fdq -e node_modules
  (cd GraderWeb && npm ci --silent)
  cd "$R"
fi
started=0
for s in "${STEPS[@]}"; do
  [ "$s" = "$FIRST" ] && started=1
  [ $started = 1 ] || continue
  echo "=== step $s ($(date +%H:%M:%S))"
  run_step "$s"
  record "$s"
  [ "$s" = "$LAST" ] && break
done
echo "=== done ($(date +%H:%M:%S))"
