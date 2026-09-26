#!/usr/bin/env bash
# Task runner for FinNavis. Usage: ./build.sh [build|test]
# Default task is "build".
set -euo pipefail

SOLUTION="FinNavis.slnx"
TASK="${1:-build}"

case "$TASK" in
  build)
    dotnet build "$SOLUTION"
    ;;
  test)
    dotnet test "$SOLUTION"
    ;;
  *)
    echo "Unknown task: $TASK"
    echo "Available tasks: build, test"
    exit 1
    ;;
esac
