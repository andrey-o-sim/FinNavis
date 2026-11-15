#!/bin/bash
command=$(jq -r '.tool_input.command')
if echo "$command" | grep -qE 'git push.*\bmaster\b'; then
  echo "Direct push to master is blocked. Push only in named worktree-branch." >&2
  exit 2
fi
