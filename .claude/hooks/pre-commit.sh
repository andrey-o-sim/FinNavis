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

set -uo pipefail

SOLUTION="FinNavis.slnx"

# "all" runs every test project. "unit" skips the ones that need Docker.
TEST_SCOPE="${FINNAVIS_PRECOMMIT_TESTS:-all}"

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

build_log=$(dotnet build "$SOLUTION" --nologo -v q 2>&1) || fail "Build failed, commit blocked:
$(printf '%s' "$build_log" | tail -n 40)"

# A changed .editorconfig or build file can reformat code this commit did not touch,
# so check the whole solution. Otherwise only the changed C# files, which is much faster.
if [ ${#config_files[@]} -gt 0 ]; then
  format_log=$(dotnet format style "$SOLUTION" --verify-no-changes --no-restore 2>&1) || fail \
    "dotnet format style found unformatted code, commit blocked:
$(printf '%s' "$format_log" | tail -n 40)

The whole solution was checked because a build or style file changed:
  ${config_files[*]}

Run: dotnet format style $SOLUTION"
elif [ ${#cs_files[@]} -gt 0 ]; then
  format_log=$(dotnet format style "$SOLUTION" --verify-no-changes --no-restore --include "${cs_files[@]}" 2>&1) || fail \
    "dotnet format style found unformatted code, commit blocked:
$(printf '%s' "$format_log" | tail -n 40)

Run: dotnet format style $SOLUTION --include ${cs_files[*]}"
fi

run_tests() {
  dotnet test "$@" --no-build --nologo -v q 2>&1
}

if [ "$TEST_SCOPE" = "unit" ]; then
  test_log=$(run_tests "${UNIT_TEST_PROJECTS[@]}") \
    || fail "Unit tests failed, commit blocked:
$(printf '%s' "$test_log" | tail -n 40)"
  exit 0
fi

# Testcontainers starts a PostgreSQL container for the integration tests.
if ! docker info >/dev/null 2>&1; then
  fail "The integration tests need Docker, and Docker is not responding.
Start Docker Desktop, then commit again.

To commit without the integration tests:
  FINNAVIS_PRECOMMIT_TESTS=unit git commit -m \"...\""
fi

test_log=$(run_tests "$SOLUTION") || fail "Tests failed, commit blocked:
$(printf '%s' "$test_log" | tail -n 40)"
