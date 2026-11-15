#!/usr/bin/env bash
# PreToolUse hook for the Bash tool.
#
# Blocks any `git push` that would update master on the remote:
#   - a refspec whose destination is master (`master`, `x:master`, `refs/heads/master`)
#   - `--all` or `--mirror`, which push master along with everything else
#   - a push with no refspec, or with HEAD, while the current branch is master
#
# Only the arguments of the push segment are checked, so a branch such as
# `feature/master-x` or a commit message that mentions master does not block itself.
#
# Exit code 2 = refuse the tool call and show the message to Claude.

set -uo pipefail

payload=$(cat)
cmd=$(printf '%s' "$payload" | jq -r '.tool_input.command // ""')

printf '%s' "$cmd" | grep -Eq '\bgit\b.*\bpush\b' || exit 0

block() {
  echo "Blocked: this command would push to master ($1)." >&2
  echo "Push the work branch by name and open a pull request into master." >&2
  exit 2
}

current_branch=$(git branch --show-current 2>/dev/null || true)

while IFS= read -r segment; do
  args=$(printf '%s' "$segment" | sed -E 's/^.*\bpush\b//' | sed "s/[\"']//g")

  positional=()
  for arg in $args; do
    case "$arg" in
      --all|--mirror) block "$arg pushes every branch" ;;
      -*) ;;
      *) positional+=("$arg") ;;
    esac
  done

  # The first positional argument is the remote. The rest are refspecs.
  refspecs=("${positional[@]:1}")

  if [ "${#refspecs[@]}" -eq 0 ]; then
    [ "$current_branch" = "master" ] && block "no refspec while on master"
    continue
  fi

  for refspec in "${refspecs[@]}"; do
    destination="${refspec##*:}"
    destination="${destination#+}"
    destination="${destination#refs/heads/}"
    [ "$destination" = "master" ] && block "refspec $refspec"
    [ "$destination" = "HEAD" ] && [ "$current_branch" = "master" ] && block "HEAD while on master"
  done
done < <(printf '%s\n' "$cmd" | tr ';|&\n' '\n\n\n\n' | grep -E '\bgit\b.*\bpush\b')

exit 0
