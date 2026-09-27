#!/usr/bin/env bash
# Task runner for FinNavis. Usage: ./build.sh [task] [args]
# Default task is "build". Run "./build.sh help" for the full list.
set -euo pipefail

SOLUTION="FinNavis.slnx"
API_PROJECT="src/FinNavis.Presentation"
EF_PROJECT="src/FinNavis.Infrastructure"
MIGRATIONS_DIR="Persistence/Migrations"
LAUNCH_SETTINGS="$API_PROJECT/Properties/launchSettings.json"
LAUNCH_SETTINGS_EXAMPLE="$API_PROJECT/Properties/launchSettings.example.json"

usage() {
  cat <<'USAGE'
Usage: ./build.sh <task> [args]

Tasks:
  build             Build the whole solution.
  test              Run all .NET tests, including the architecture tests.
  run               Start the API on the local machine.
  migrate <Name>    Add an EF Core migration called <Name>.
  db-update         Apply every pending migration to the database.
  clean             Delete build output.
  help              Show this text.

Default task is "build".

run, migrate and db-update read the connection string from launchSettings.json.
That file is git-ignored. Copy the template first:
  cp src/FinNavis.Presentation/Properties/launchSettings.example.json \
     src/FinNavis.Presentation/Properties/launchSettings.json
USAGE
}

require_launch_settings() {
  if [ ! -f "$LAUNCH_SETTINGS" ]; then
    echo "Missing $LAUNCH_SETTINGS" >&2
    echo "Copy the template and fill in the values:" >&2
    echo "  cp $LAUNCH_SETTINGS_EXAMPLE $LAUNCH_SETTINGS" >&2
    exit 1
  fi
}

TASK="${1:-build}"
shift || true

case "$TASK" in
  build)
    dotnet build "$SOLUTION"
    ;;
  test)
    dotnet test "$SOLUTION"
    ;;
  run)
    require_launch_settings
    dotnet run --project "$API_PROJECT"
    ;;
  migrate)
    NAME="${1:-}"
    if [ -z "$NAME" ]; then
      echo "migrate needs a name. Example: ./build.sh migrate AddAccounts" >&2
      exit 1
    fi
    require_launch_settings
    dotnet ef migrations add "$NAME" \
      --project "$EF_PROJECT" \
      --startup-project "$API_PROJECT" \
      --output-dir "$MIGRATIONS_DIR"
    ;;
  db-update)
    require_launch_settings
    dotnet ef database update \
      --project "$EF_PROJECT" \
      --startup-project "$API_PROJECT"
    ;;
  clean)
    dotnet clean "$SOLUTION"
    ;;
  help | -h | --help)
    usage
    ;;
  *)
    echo "Unknown task: $TASK" >&2
    echo >&2
    usage >&2
    exit 1
    ;;
esac
