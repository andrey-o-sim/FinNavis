#!/usr/bin/env bash
# PreToolUse hook for the Bash tool.
#
# Blocks any git command that would put a local config file into a commit:
#   .env, .env.<anything> (except .env.example), launchSettings.json
#
# Checks, in order:
#   1a. paths named outright in a `git add` / `git stage` segment
#   1b. paths git itself reports for that segment, via `git add --dry-run`
#   2.  files already in the index, when the command is a commit
#
# 1a and 1b cover different gaps. 1a sees a path even when the add would run in
# another directory or name a file that does not exist. 1b sees `git add .`, -A,
# -u, directories and globs, which name no path 1a could match.
#
# Only the arguments of the add segment are scanned, never the whole command line.
# Otherwise a commit message that names one of these files would block itself.
#
# Exit code 2 = refuse the tool call and show the message to Claude.

set -uo pipefail

payload=$(cat)
cmd=$(printf '%s' "$payload" | jq -r '.tool_input.command // ""')

printf '%s' "$cmd" | grep -Eq '\bgit\b' || exit 0

BLOCKED_RE='(^|/)(\.env(\.[^/]+)?|launchSettings\.json)$'
ALLOW_RE='(^|/)\.env\.example$'

filter() { grep -E "$BLOCKED_RE" | grep -Ev "$ALLOW_RE" || true; }

# Split into simple commands first, keep the `git add` / `git stage` ones, then drop
# everything up to and including the subcommand. What is left is the arguments.
add_args=$(printf '%s\n' "$cmd" \
  | tr ';|&\n' '\n\n\n\n' \
  | grep -E '\bgit\b.*\b(add|stage)\b' \
  | sed -E 's/^.*\b(add|stage)\b//' || true)

# 1a. Paths named outright.
named=$(printf '%s\n' "$add_args" | sed "s/[\"']/ /g" | tr -s ' \t' '\n' | filter)

# 1b. What git says it would stage. --dry-run leaves the index untouched.
expanded=""
while IFS= read -r args; do
  [ -z "${args//[[:space:]]/}" ] && continue
  # Never eval a command substitution or a redirection: the tool call may still be
  # refused, and the hook must not have side effects of its own.
  case "$args" in *'$('*|*'`'*|*'>'*|*'<'*) continue ;; esac
  expanded="$expanded
$(eval "git add --dry-run $args" 2>/dev/null | sed -nE "s/^add '(.*)'\$/\1/p")"
done <<< "$add_args"
expanded=$(printf '%s\n' "$expanded" | filter)

# 2. Files already staged, when the command is a commit.
from_index=""
if printf '%s' "$cmd" | grep -Eq '\bcommit\b'; then
  from_index=$(git diff --cached --name-only 2>/dev/null | filter)
fi

hits=$(printf '%s\n%s\n%s\n' "$named" "$expanded" "$from_index" \
  | grep -v '^$' | sort -u || true)

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
