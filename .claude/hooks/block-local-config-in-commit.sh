#!/usr/bin/env bash
# PreToolUse hook for the Bash tool.
#
# Blocks any git command that would put a local config file into a commit:
#   .env, .env.<anything> (except .env.example), launchSettings.json
#
# Two checks:
#   1. the paths given to `git add` / `git stage` -> catches `git add -f .env`
#   2. the files already in the index on a commit -> catches a plain `git commit`
#
# Check 1 reads the arguments of the add segment only, never the whole command line.
# Otherwise a commit message that merely names one of these files would block itself.
#
# Exit code 2 = refuse the tool call and show the message to Claude.

set -uo pipefail

payload=$(cat)
cmd=$(printf '%s' "$payload" | jq -r '.tool_input.command // ""')

printf '%s' "$cmd" | grep -Eq '\bgit\b' || exit 0

BLOCKED_RE='(^|/)(\.env(\.[^/]+)?|launchSettings\.json)$'
ALLOW_RE='(^|/)\.env\.example$'

# 1. Split into simple commands, keep the `git add` / `git stage` ones, drop everything
#    up to and including the subcommand. What is left is the path arguments.
add_args=$(printf '%s\n' "$cmd" \
  | tr ';|&\n' '\n\n\n\n' \
  | grep -E '\bgit\b.*\b(add|stage)\b' \
  | sed -E 's/^.*\b(add|stage)\b//' || true)

from_cmd=$(printf '%s\n' "$add_args" \
  | sed "s/[\"']/ /g" \
  | tr -s ' \t' '\n' \
  | grep -E "$BLOCKED_RE" \
  | grep -Ev "$ALLOW_RE" || true)

# 2. Files already staged, when the command is a commit.
from_index=""
if printf '%s' "$cmd" | grep -Eq '\bcommit\b'; then
  from_index=$(git diff --cached --name-only 2>/dev/null \
    | grep -E "$BLOCKED_RE" \
    | grep -Ev "$ALLOW_RE" || true)
fi

hits=$(printf '%s\n%s\n' "$from_cmd" "$from_index" | grep -v '^$' | sort -u || true)

[ -z "$hits" ] && exit 0

{
  echo "Blocked: this command would put a local config file into a commit."
  echo "$hits" | sed 's/^/  - /'
  echo
  echo "These files are git-ignored on purpose (see .gitignore). They hold real"
  echo "connection strings and keys. Do not commit them and do not use 'git add -f'."
  echo "Commit launchSettings.example.json instead."
} >&2

exit 2
