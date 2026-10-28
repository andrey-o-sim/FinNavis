#!/usr/bin/env bash
# Ralph loop — canonical Geoffrey Huntley pattern with three safety nets.
# Lecture 7.2 (Module 7 · Execution & Scale).
#
# The loop is intentionally dumb: it re-runs ONE prompt against `claude -p`
# (headless, fresh context each turn) until the model creates a `DONE` file.
# Everything that survives between turns lives on disk (git, tracker), never
# in the model's memory.

set -euo pipefail

# --- Knobs (override via env) ---------------------------------------------
MAX_ITER="${MAX_ITER:-10}"          # Safety net 1: hard iteration ceiling
PROMPT_FILE="${PROMPT_FILE:-PROMPT.md}"
COST_LOG="${COST_LOG:-cost.log}"    # Safety net 2: per-turn audit trail
PERMISSION_MODE="${PERMISSION_MODE:-acceptEdits}"

ITER=0
TOTAL_COST=0

if [ ! -f "$PROMPT_FILE" ]; then
  echo "No $PROMPT_FILE found. Generate one first (see README: /ralph-prep)." >&2
  exit 2
fi

if ! command -v jq >/dev/null; then
  echo "jq is required to read the stream-json output of claude -p." >&2
  exit 2
fi

# total_cost_usd is the CLI's own estimate (tokens x API list price), not the
# billed amount. Compare it with the billing panel after the run.
log_total() {
  echo "=== $1 at $(date -Iseconds), iterations: $ITER, total_cost_usd: $TOTAL_COST (estimate) ===" >> "$COST_LOG"
}

echo "=== Ralph run started: $(date -Iseconds) (prompt=$PROMPT_FILE, max_iter=$MAX_ITER) ===" >> "$COST_LOG"

# --- Safety net 3: graceful Ctrl-C ----------------------------------------
trap 'echo "Interrupted at iteration $ITER. State preserved — check git status."; log_total "Interrupted"' INT

# One progress line per tool call or text block, printed as it happens.
PROGRESS_FILTER='
  select(.type == "assistant") | .message.content[]? |
  if .type == "tool_use" then
    "  > \(.name) \(.input.command // .input.file_path // .input.pattern // "" | tostring | .[0:100])"
  elif .type == "text" then
    "  \(.text | gsub("\n"; " ") | .[0:160])"
  else empty end'

STREAM_FILE="$(mktemp)"
trap 'rm -f "$STREAM_FILE"' EXIT

# --- The loop -------------------------------------------------------------
while [ ! -f DONE ]; do
  # The model writes BLOCKED when a task fails 3 attempts (PROMPT.md step 8).
  # Stop here, so no paid `claude -p` call is made just to read it and exit.
  if [ -f BLOCKED ]; then
    echo "BLOCKED found after $ITER iterations. The reason is in the tracker.md of the feature."
    log_total "BLOCKED"
    exit 1
  fi

  ITER=$((ITER + 1))

  if [ "$ITER" -gt "$MAX_ITER" ]; then
    ITER=$((ITER - 1))
    echo "Iteration limit ($MAX_ITER) reached. Exiting without DONE."
    log_total "Hit MAX_ITER"
    exit 1
  fi

  echo "--- Iteration $ITER ---"

  # Cold start every turn: a fresh `claude -p` invocation with no accumulated
  # context. The model re-reads PROMPT.md and the current filesystem from
  # scratch — this is what prevents context rot on long runs.
  # Keep the exit code instead of letting set -e stop the script here:
  # the cost of a failed iteration must reach cost.log too.
  CLAUDE_EXIT=0
  claude -p "$(cat "$PROMPT_FILE")" --permission-mode "$PERMISSION_MODE" \
    --verbose --output-format stream-json \
    | tee "$STREAM_FILE" \
    | jq -r --unbuffered "$PROGRESS_FILTER" || CLAUDE_EXIT=$?

  # jq on Windows ends lines with CRLF; strip CR before using the values.
  RESULT="$(jq -r 'select(.type == "result") | "\(.total_cost_usd // "?") \(.num_turns // "?") \(.subtype // "?")"' "$STREAM_FILE" | tr -d '\r')"
  read -r COST TURNS STATUS <<< "${RESULT:-? ? no-result}"

  echo "$(date -Iseconds) iter=$ITER cost_usd=$COST turns=$TURNS status=$STATUS" >> "$COST_LOG"
  if [ "$COST" != "?" ]; then
    TOTAL_COST="$(awk -v a="$TOTAL_COST" -v b="$COST" 'BEGIN { printf "%.4f", a + b }')"
  fi
  echo "  iteration cost: \$$COST, run total: \$$TOTAL_COST (estimate)"

  if [ "$CLAUDE_EXIT" -ne 0 ]; then
    echo "claude -p failed with exit code $CLAUDE_EXIT. Stopping."
    log_total "claude failed (exit $CLAUDE_EXIT)"
    exit "$CLAUDE_EXIT"
  fi

  sleep 1   # make Ctrl-C reliable between turns
done

log_total "DONE found"
echo "Ralph completed in $ITER iterations. Check 'git log' for results."
