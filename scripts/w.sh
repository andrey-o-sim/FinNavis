# scripts/w.sh — create an isolated FinNavis worktree in one call.
#
# Usage (Git Bash, Linux, macOS):
#   source scripts/w.sh
#   w feature/csv-import             # create the worktree
#   w-rm feature/csv-import          # remove it, keep its database
#   w-rm feature/csv-import --drop-db
#
# What `w` does:
#   1. Creates ../FinNavis-<prefix>-<slug> on a new branch cut from master.
#   2. Copies the git-ignored launchSettings.json into it and changes the copy only:
#      its own HTTP and HTTPS ports, and its own database (<base>_<prefix>_<slug>).
#   3. Copies the git-ignored .env, if there is one, and gives it its own API_PORT.
#   4. Runs `dotnet restore` inside the new worktree.
#   5. Creates the database and applies the migrations, if PostgreSQL answers.
#
# The connection string holds a password. The script never prints it and never puts it on a
# command line: it goes from the file to the environment of one dotnet process.

_W_BRANCH_PATTERN='^(feature|bugfix|hotfix|docs|test|chore)/[a-z0-9]+(-[a-z0-9]+)*$'
_W_LAUNCH_SETTINGS='src/FinNavis.Presentation/Properties/launchSettings.json'
_W_CONNECTION_VARIABLE='ConnectionStrings__FinNavisDb'

_w_main_root() {
  local common_dir
  common_dir="$(git rev-parse --path-format=absolute --git-common-dir 2>/dev/null)" || {
    echo "w: run this inside the FinNavis repository" >&2
    return 1
  }
  dirname "$common_dir"
}

_w_check_branch() {
  if ! printf '%s' "$1" | grep -Eq "$_W_BRANCH_PATTERN"; then
    echo "w: '$1' is not a valid branch name." >&2
    echo "   Use <prefix>/<short-slug>, prefix one of feature bugfix hotfix docs test chore." >&2
    echo "   Example: feature/csv-import (see .claude/rules/git.md)" >&2
    return 1
  fi
}

_w_worktree_dir() {
  local main_root="$1" branch="$2"
  printf '%s/%s-%s' "$(dirname "$main_root")" "$(basename "$main_root")" "${branch//\//-}"
}

_w_python() {
  local candidate
  for candidate in python3 python py; do
    if command -v "$candidate" >/dev/null 2>&1 && "$candidate" -c 'import socket' >/dev/null 2>&1; then
      echo "$candidate"
      return 0
    fi
  done
  echo "w: python is needed to find a free port" >&2
  return 1
}

# Both ports are taken from the OS while both sockets are open, so the two are never equal.
_w_two_free_ports() {
  local python
  python="$(_w_python)" || return 1
  "$python" -c '
import socket
sockets = [socket.socket() for _ in range(2)]
for s in sockets:
    s.bind(("127.0.0.1", 0))
print(" ".join(str(s.getsockname()[1]) for s in sockets))
for s in sockets:
    s.close()
' | tr -d '\r'
}

_w_database_name() {
  jq -r --arg var "$_W_CONNECTION_VARIABLE" '
    [.profiles[]?.environmentVariables[$var] // empty
     | capture("(^|;)\\s*Database\\s*=\\s*(?<db>[^;]*)"; "i").db]
    | first // "finnavis"
  ' "$1" | tr -d '\r'
}

_w_rewrite_launch_settings() {
  local file="$1" http_port="$2" https_port="$3" database="$4"
  jq --arg var "$_W_CONNECTION_VARIABLE" \
     --arg http "http://localhost:$http_port" \
     --arg https "https://localhost:$https_port;http://localhost:$http_port" \
     --arg db "$database" '
    def with_database:
      if test("(^|;)\\s*Database\\s*="; "i")
      then sub("(?<pre>(^|;)\\s*)Database\\s*=\\s*[^;]*"; "\(.pre)Database=\($db)"; "i")
      else (rtrimstr(";") + ";Database=" + $db)
      end;
    .profiles |= with_entries(
      .value.applicationUrl = (if .key == "https" then $https else $http end)
      | if (.value.environmentVariables[$var] // "") != ""
        then .value.environmentVariables[$var] |= with_database
        else . end)
  ' "$file" > "$file.tmp" && mv "$file.tmp" "$file"
}

_w_set_env_port() {
  local file="$1" port="$2"
  if grep -q '^API_PORT=' "$file"; then
    sed "s/^API_PORT=.*/API_PORT=${port}/" "$file" > "$file.tmp" && mv "$file.tmp" "$file"
  else
    printf '\nAPI_PORT=%s\n' "$port" >> "$file"
  fi
}

_w_connection_string() {
  jq -r --arg var "$_W_CONNECTION_VARIABLE" \
    '.profiles.http.environmentVariables[$var] // empty' "$1" | tr -d '\r'
}

# Host and port only. The rest of the connection string holds the password.
_w_server() {
  printf '%s' "$1" | tr ';' '\n' | awk -F= '
    { key = tolower($1); gsub(/ /, "", key); value = $2; gsub(/^ +| +$/, "", value) }
    key == "host" || key == "server" { host = value }
    key == "port" { port = value }
    END { printf "%s:%s", (host == "" ? "localhost" : host), (port == "" ? "5432" : port) }'
}

_w_print_error_tail() {
  echo "   Last lines of the output:" >&2
  grep -Ev '^[[:space:]]*$|^[[:space:]]+at ' "$1" | tail -n 8 | sed 's/^/     /' >&2
}

w() {
  local branch="$1"
  if [ -z "$branch" ]; then
    echo "usage: w <prefix>/<short-slug>    example: w feature/csv-import" >&2
    return 1
  fi
  _w_check_branch "$branch" || return 1
  command -v jq >/dev/null 2>&1 || { echo "w: jq is needed" >&2; return 1; }

  local main_root dir
  main_root="$(_w_main_root)" || return 1
  dir="$(_w_worktree_dir "$main_root" "$branch")"

  if [ -e "$dir" ]; then
    echo "w: $dir already exists" >&2
    return 1
  fi

  git -C "$main_root" worktree add "$dir" -b "$branch" master >/dev/null || return 1

  local ports http_port https_port
  ports="$(_w_two_free_ports)" || return 1
  read -r http_port https_port <<< "$ports"

  local database="not configured"
  local launch_settings="$dir/$_W_LAUNCH_SETTINGS"
  if [ -f "$main_root/$_W_LAUNCH_SETTINGS" ]; then
    database="$(_w_database_name "$main_root/$_W_LAUNCH_SETTINGS")_${branch//[\/-]/_}"
    mkdir -p "$(dirname "$launch_settings")"
    cp "$main_root/$_W_LAUNCH_SETTINGS" "$launch_settings"
    _w_rewrite_launch_settings "$launch_settings" "$http_port" "$https_port" "$database" || {
      echo "w: could not rewrite $launch_settings — fix it by hand" >&2
      return 1
    }
  else
    echo "w: warning — $_W_LAUNCH_SETTINGS is missing in $main_root." >&2
    echo "   The worktree has no port and no database. Create the file from the example." >&2
  fi

  if [ -f "$main_root/.env" ]; then
    cp "$main_root/.env" "$dir/.env"
    _w_set_env_port "$dir/.env" "$http_port"
  fi

  echo "w: dotnet restore ..."
  if ! (cd "$dir" && dotnet restore FinNavis.slnx --verbosity quiet); then
    echo "w: warning — dotnet restore failed. Run it again inside $dir" >&2
  fi

  if [ -f "$launch_settings" ]; then
    local connection_string
    connection_string="$(_w_connection_string "$launch_settings")"
    if [ -z "$connection_string" ]; then
      echo "w: warning — no $_W_CONNECTION_VARIABLE in the http profile, migrations skipped" >&2
    else
      echo "w: creating database $database and applying migrations ..."
      local log
      log="$(mktemp)"
      if ! (cd "$dir" && export "$_W_CONNECTION_VARIABLE=$connection_string" && ./build.sh db-update >"$log" 2>&1); then
        echo "w: warning — migrations not applied." >&2
        echo "   Server $(_w_server "$connection_string"), database $database." >&2
        _w_print_error_tail "$log"
        echo "   Fix the cause, then run:" >&2
        printf "   cd \"%s\" && export %s=\"\$(jq -r '.profiles.http.environmentVariables.%s' %s)\" && ./build.sh db-update\n" \
          "$dir" "$_W_CONNECTION_VARIABLE" "$_W_CONNECTION_VARIABLE" "$_W_LAUNCH_SETTINGS" >&2
      fi
      rm -f "$log"
    fi
  fi

  echo
  echo "worktree ready: $(basename "$dir")"
  echo "  branch    $branch"
  echo "  http      http://localhost:$http_port"
  echo "  https     https://localhost:$https_port"
  echo "  database  $database"
  echo "  cd \"$dir\" && ./build.sh run"
}

w-rm() {
  local branch="$1" drop_db=""
  if [ "$2" = "--drop-db" ]; then
    drop_db="yes"
  elif [ -n "$2" ]; then
    echo "w-rm: unknown option $2" >&2
    return 1
  fi
  if [ -z "$branch" ]; then
    echo "usage: w-rm <prefix>/<short-slug> [--drop-db]" >&2
    return 1
  fi
  _w_check_branch "$branch" || return 1

  local main_root dir
  main_root="$(_w_main_root)" || return 1
  dir="$(_w_worktree_dir "$main_root" "$branch")"

  if [ ! -d "$dir" ]; then
    echo "w-rm: $dir does not exist" >&2
    return 1
  fi
  case "$(pwd -P)/" in
    "$(cd "$dir" && pwd -P)/"*)
      echo "w-rm: you are inside $dir. cd out of it first." >&2
      return 1
      ;;
  esac

  if [ -n "$drop_db" ]; then
    command -v jq >/dev/null 2>&1 || { echo "w-rm: jq is needed for --drop-db" >&2; return 1; }
    local launch_settings="$dir/$_W_LAUNCH_SETTINGS" connection_string database answer
    connection_string="$(_w_connection_string "$launch_settings" 2>/dev/null)"
    if [ -z "$connection_string" ]; then
      echo "w-rm: no connection string in $launch_settings — nothing to drop" >&2
      return 1
    fi
    database="$(_w_database_name "$launch_settings")"
    echo "This deletes the database '$database' and all its data."
    printf "Type the database name to confirm: "
    read -r answer
    if [ "$answer" != "$database" ]; then
      echo "w-rm: names do not match. Nothing was changed." >&2
      return 1
    fi
    local log
    log="$(mktemp)"
    (cd "$dir" && export "$_W_CONNECTION_VARIABLE=$connection_string" &&
      dotnet ef database drop --force \
        --project src/FinNavis.Infrastructure \
        --startup-project src/FinNavis.Presentation >"$log" 2>&1) || {
      echo "w-rm: could not drop the database. The worktree is kept." >&2
      echo "   Server $(_w_server "$connection_string"), database $database." >&2
      _w_print_error_tail "$log"
      rm -f "$log"
      return 1
    }
    rm -f "$log"
  fi

  git -C "$main_root" worktree remove "$dir" || {
    echo "w-rm: git refused. Commit or discard the changes in $dir first." >&2
    return 1
  }
  echo "w-rm: removed $dir"

  if ! git -C "$main_root" branch -d "$branch" >/dev/null 2>&1; then
    echo "w-rm: branch $branch kept — it is not merged into the current branch." >&2
    echo "   After a squash merge this is expected. Delete it with: git branch -D $branch" >&2
  else
    echo "w-rm: deleted branch $branch"
  fi
}
