#!/usr/bin/env bash
# Usage: run-agent.sh <prompt-file> <log-name>   (run from rehearsal/)
set -euo pipefail
PROMPT_FILE="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
LOG="$(pwd)/logs/$2.jsonl"
cd grader
START=$(date +%s)
claude -p "$(cat "$PROMPT_FILE")" --model claude-opus-5-5 --effort high \
  --permission-mode auto ${CLAUDE_EXTRA_ARGS:-} --output-format stream-json --verbose > "$LOG" 2>&1 || echo "claude exited with $?"
echo "Duration: $(( $(date +%s) - START ))s"
tail -1 "$LOG" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('subtype'), 'turns:', d.get('num_turns'), 'cost:', d.get('total_cost_usd')); print(d.get('result','')[:3000])"
