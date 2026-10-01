#!/usr/bin/env bash
# Pre-commit checks for FinNavis.
#
# Started by the Claude Code PreToolUse hook in .claude/settings.json, which fires on
# `git commit`. The checks are: build, dotnet format style, then the whole test suite.
#
# Exit 0 lets the commit through. Exit 2 blocks it and sends the reason back to Claude,
# so it can fix the problem instead of just seeing "command failed".
#
# The integration tests use Testcontainers, so Docker must be running. See
# CLAUDE.md > Tests. To skip them for one commit:
#   FINNAVIS_PRECOMMIT_TESTS=unit git commit -m "..."
#
# Each step has its own time limit, see STEP_TIMEOUT below. To raise it for one commit:
#   FINNAVIS_PRECOMMIT_TIMEOUT=600 git commit -m "..."

set -uo pipefail

SOLUTION="FinNavis.slnx"

# "all" runs every test project. "unit" skips the ones that need Docker.
TEST_SCOPE="${FINNAVIS_PRECOMMIT_TESTS:-all}"

# Time limit in seconds for one step: build, format, or tests. It is per step, not for
# the whole hook.
#
# The hook in .claude/settings.json has no "timeout" on purpose. A hook killed from the
# outside dies without a message, so the commit just fails with no reason. Stopping the
# step here instead lets us say what hung and how long it got.
STEP_TIMEOUT="${FINNAVIS_PRECOMMIT_TIMEOUT:-180}"

# GNU coreutils timeout. Git Bash and Linux ship it. On macOS it is gtimeout, and only
# when coreutils is installed. With neither, run without a limit: the checks still matter
# more than the limit.
if command -v timeout >/dev/null 2>&1; then
  TIMEOUT_CMD=(timeout --kill-after=10s "$STEP_TIMEOUT")
elif command -v gtimeout >/dev/null 2>&1; then
  TIMEOUT_CMD=(gtimeout --kill-after=10s "$STEP_TIMEOUT")
else
  TIMEOUT_CMD=()
fi

UNIT_TEST_PROJECTS=(
  "tests/FinNavis.Domain.UnitTests/FinNavis.Domain.UnitTests.csproj"
  "tests/FinNavis.Application.UnitTests/FinNavis.Application.UnitTests.csproj"
  "tests/FinNavis.ArchitectureTests/FinNavis.ArchitectureTests.csproj"
)

repo="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"
[ -n "$repo" ] && [ -d "$repo" ] || exit 0
cd "$repo" || exit 0

fail() {
  printf '%s\n' "$1" >&2
  exit 2
}

step_output=$(mktemp) || fail "Cannot create a temp file for the pre-commit log, commit blocked."
trap 'rm -f "$step_output"' EXIT

# Runs one step under the time limit. Puts the last 40 lines of its output in $step_log and
# its exit code in $step_status. Read the code from $step_status, not from $?: after
# `if ! run_step ...` the shell reports the negated result, so $? is always 0 there.
#
# The output goes to a file, not to a pipe. A dotnet child process can outlive the timeout,
# and while it holds a pipe open the shell keeps waiting for it. That is exactly the hang we
# are trying to stop, so we never read through a pipe here.
run_step() {
  step_status=0
  "${TIMEOUT_CMD[@]}" "$@" >"$step_output" 2>&1 || step_status=$?
  step_log=$(tail -n 40 "$step_output")
  return "$step_status"
}

# Call right after a failed run_step, before the normal error message.
# Exits when the step ran out of time. Returns when it failed for a real reason.
#
# timeout exits with 124 after SIGTERM, and with 137 when --kill-after had to use SIGKILL.
check_timeout() {
  local status=$1 name=$2 command=$3

  case "$status" in
    124 | 137) ;;
    *) return 0 ;;
  esac

  fail "$name did not finish in ${STEP_TIMEOUT}s and was stopped, commit blocked:
$step_log

The step was cut off, so we do not know whether it would have passed. Run it yourself to
see what is slow:
  $command

To give it more time for one commit:
  FINNAVIS_PRECOMMIT_TIMEOUT=600 git commit -m \"...\""
}

# `git commit -a` stages files only at commit time, so unstaged changes count too.
changed_files() {
  {
    git diff --cached --name-only --diff-filter=ACMR -- "$@"
    git diff --name-only --diff-filter=ACMR -- "$@"
  } | sort -u
}

# Keep only paths that still exist. A rename leaves the old path in the list.
keep_existing() {
  local file
  while IFS= read -r file; do
    [ -f "$file" ] && printf '%s\n' "$file"
  done
}

mapfile -t cs_files < <(changed_files '*.cs' | keep_existing)

# A change to any of these can alter the result of the build or of dotnet format
# for files that were not touched in this commit.
mapfile -t config_files < <(
  changed_files '.editorconfig' '*.csproj' '*.slnx' '*.props' 'Directory.Build.props' | keep_existing
)

# Nothing here can break the build or the tests.
if [ ${#cs_files[@]} -eq 0 ] && [ ${#config_files[@]} -eq 0 ]; then
  exit 0
fi

if ! run_step dotnet build "$SOLUTION" --nologo -v q; then
  check_timeout "$step_status" "The build" "dotnet build $SOLUTION"
  fail "Build failed, commit blocked:
$step_log"
fi

# A changed .editorconfig or build file can reformat code this commit did not touch,
# so check the whole solution. Otherwise only the changed C# files, which is much faster.
if [ ${#config_files[@]} -gt 0 ]; then
  if ! run_step dotnet format style "$SOLUTION" --verify-no-changes --no-restore; then
    check_timeout "$step_status" "dotnet format style" "dotnet format style $SOLUTION --verify-no-changes"
    fail "dotnet format style found unformatted code, commit blocked:
$step_log

The whole solution was checked because a build or style file changed:
  ${config_files[*]}

Run: dotnet format style $SOLUTION"
  fi
elif [ ${#cs_files[@]} -gt 0 ]; then
  if ! run_step dotnet format style "$SOLUTION" --verify-no-changes --no-restore --include "${cs_files[@]}"; then
    check_timeout "$step_status" "dotnet format style" "dotnet format style $SOLUTION --verify-no-changes --include ${cs_files[*]}"
    fail "dotnet format style found unformatted code, commit blocked:
$step_log

Run: dotnet format style $SOLUTION --include ${cs_files[*]}"
  fi
fi

if [ "$TEST_SCOPE" = "unit" ]; then
  if ! run_step dotnet test "${UNIT_TEST_PROJECTS[@]}" --no-build --nologo -v q; then
    check_timeout "$step_status" "The unit tests" "dotnet test ${UNIT_TEST_PROJECTS[*]} --no-build"
    fail "Unit tests failed, commit blocked:
$step_log"
  fi
  exit 0
fi

# Testcontainers starts a PostgreSQL container for the integration tests.
if ! docker info >/dev/null 2>&1; then
  fail "The integration tests need Docker, and Docker is not responding.
Start Docker Desktop, then commit again.

To commit without the integration tests:
  FINNAVIS_PRECOMMIT_TESTS=unit git commit -m \"...\""
fi

if ! run_step dotnet test "$SOLUTION" --no-build --nologo -v q; then
  check_timeout "$step_status" "The tests" "dotnet test $SOLUTION --no-build"
  fail "Tests failed, commit blocked:
$step_log"
fi
