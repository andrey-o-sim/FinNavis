# scripts/w.ps1 - create an isolated FinNavis worktree in one call. PowerShell twin of w.sh.
#
# Usage (Windows PowerShell 5.1 or PowerShell 7):
#   . ./scripts/w.ps1
#   w feature/csv-import             # create the worktree
#   w-rm feature/csv-import          # remove it, keep its database
#   w-rm feature/csv-import -DropDb
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
# command line: it goes from the file to the environment of one child process.

$script:WBranchPattern = '^(feature|bugfix|hotfix|docs|test|chore)/[a-z0-9]+(-[a-z0-9]+)*$'
$script:WLaunchSettings = 'src/FinNavis.Presentation/Properties/launchSettings.json'
$script:WConnectionVariable = 'ConnectionStrings__FinNavisDb'
$script:WDatabasePattern = '(?i)(^|;)\s*Database\s*=\s*([^;]*)'

function Get-WMainRoot {
    $commonDir = git rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $commonDir) {
        throw 'w: run this inside the FinNavis repository'
    }
    Split-Path -Parent $commonDir
}

function Assert-WBranch([string]$Branch) {
    if ($Branch -cnotmatch $script:WBranchPattern) {
        throw ("w: '$Branch' is not a valid branch name. Use <prefix>/<short-slug>, prefix one of " +
            'feature bugfix hotfix docs test chore. Example: feature/csv-import (see .claude/rules/git.md)')
    }
}

function Get-WWorktreeDir([string]$MainRoot, [string]$Branch) {
    Join-Path (Split-Path -Parent $MainRoot) ((Split-Path -Leaf $MainRoot) + '-' + ($Branch -replace '/', '-'))
}

# Both ports are taken from the OS while both listeners are open, so the two are never equal.
function Get-WTwoFreePorts {
    $listeners = 1..2 | ForEach-Object {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
        $listener.Start()
        $listener
    }
    try {
        $listeners | ForEach-Object { $_.LocalEndpoint.Port }
    }
    finally {
        $listeners | ForEach-Object { $_.Stop() }
    }
}

# Windows PowerShell 5.1 writes a BOM with -Encoding utf8, and docker compose reads a BOM in .env
# as part of the first variable name.
function Write-WUtf8File([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Read-WLaunchSettings([string]$Path) {
    Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
}

function Get-WConnectionString($Settings) {
    $httpProfile = $Settings.profiles.http
    if ($httpProfile -and $httpProfile.environmentVariables) {
        return $httpProfile.environmentVariables.$script:WConnectionVariable
    }
    $null
}

function Get-WDatabaseName($Settings) {
    foreach ($launchProfile in $Settings.profiles.PSObject.Properties) {
        $variables = $launchProfile.Value.environmentVariables
        if ($variables -and $variables.$script:WConnectionVariable -match $script:WDatabasePattern) {
            return $Matches[2].Trim()
        }
    }
    'finnavis'
}

function Set-WDatabase([string]$ConnectionString, [string]$Database) {
    if ($ConnectionString -match $script:WDatabasePattern) {
        return [regex]::Replace($ConnectionString, $script:WDatabasePattern, { param($m) $m.Groups[1].Value + "Database=$Database" })
    }
    $ConnectionString.TrimEnd(';') + ";Database=$Database"
}

function Update-WLaunchSettings([string]$Path, [int]$HttpPort, [int]$HttpsPort, [string]$Database) {
    $settings = Read-WLaunchSettings $Path
    foreach ($launchProfile in $settings.profiles.PSObject.Properties) {
        $url = if ($launchProfile.Name -eq 'https') {
            "https://localhost:$HttpsPort;http://localhost:$HttpPort"
        } else {
            "http://localhost:$HttpPort"
        }
        $launchProfile.Value | Add-Member -NotePropertyName applicationUrl -NotePropertyValue $url -Force

        $variables = $launchProfile.Value.environmentVariables
        if ($variables -and $variables.$script:WConnectionVariable) {
            $variables.$script:WConnectionVariable = Set-WDatabase $variables.$script:WConnectionVariable $Database
        }
    }
    Write-WUtf8File $Path (ConvertTo-Json -InputObject $settings -Depth 20)
}

function Set-WEnvPort([string]$Path, [int]$Port) {
    $text = [System.IO.File]::ReadAllText($Path)
    $text = if ($text -match '(?m)^API_PORT=') {
        $text -replace '(?m)^API_PORT=.*$', "API_PORT=$Port"
    } else {
        $text.TrimEnd() + "`nAPI_PORT=$Port`n"
    }
    Write-WUtf8File $Path $text
}

# build.ps1 ends with `exit`, so it runs in a child process. The child inherits the variable;
# the parent clears it again in `finally`.
function Invoke-WWithConnection([string]$Directory, [string]$ConnectionString, [string[]]$Command) {
    $previous = [Environment]::GetEnvironmentVariable($script:WConnectionVariable, 'Process')
    [Environment]::SetEnvironmentVariable($script:WConnectionVariable, $ConnectionString, 'Process')
    Push-Location $Directory
    try {
        & $Command[0] $Command[1..($Command.Length - 1)] *> $null
        $LASTEXITCODE -eq 0
    }
    finally {
        Pop-Location
        [Environment]::SetEnvironmentVariable($script:WConnectionVariable, $previous, 'Process')
    }
}

function w {
    param([Parameter(Mandatory)][string]$Branch)

    # git and dotnet write progress to stderr. Under 'Stop', Windows PowerShell 5.1 turns that into
    # a terminating error, so it is relaxed here; every real failure is checked through $LASTEXITCODE.
    $ErrorActionPreference = 'Continue'
    Assert-WBranch $Branch
    $mainRoot = Get-WMainRoot
    $dir = Get-WWorktreeDir $mainRoot $Branch
    if (Test-Path -LiteralPath $dir) {
        throw "w: $dir already exists"
    }

    git -C $mainRoot worktree add $dir -b $Branch master | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'w: git worktree add failed' }

    $httpPort, $httpsPort = Get-WTwoFreePorts

    $database = 'not configured'
    $mainSettings = Join-Path $mainRoot $script:WLaunchSettings
    $launchSettings = Join-Path $dir $script:WLaunchSettings
    if (Test-Path -LiteralPath $mainSettings) {
        $database = (Get-WDatabaseName (Read-WLaunchSettings $mainSettings)) + '_' + ($Branch -replace '[/-]', '_')
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $launchSettings) | Out-Null
        Copy-Item -LiteralPath $mainSettings -Destination $launchSettings
        Update-WLaunchSettings $launchSettings $httpPort $httpsPort $database
    } else {
        Write-Warning "w: $($script:WLaunchSettings) is missing in $mainRoot. The worktree has no port and no database. Create the file from the example."
    }

    $mainEnv = Join-Path $mainRoot '.env'
    if (Test-Path -LiteralPath $mainEnv) {
        Copy-Item -LiteralPath $mainEnv -Destination (Join-Path $dir '.env')
        Set-WEnvPort (Join-Path $dir '.env') $httpPort
    }

    Write-Host 'w: dotnet restore ...'
    Push-Location $dir
    try {
        dotnet restore FinNavis.slnx --verbosity quiet
        if ($LASTEXITCODE -ne 0) { Write-Warning "w: dotnet restore failed. Run it again inside $dir" }
    }
    finally {
        Pop-Location
    }

    if (Test-Path -LiteralPath $launchSettings) {
        $connectionString = Get-WConnectionString (Read-WLaunchSettings $launchSettings)
        if (-not $connectionString) {
            Write-Warning "w: no $($script:WConnectionVariable) in the http profile, migrations skipped"
        } else {
            Write-Host "w: creating database $database and applying migrations ..."
            $shell = (Get-Process -Id $PID).Path
            $applied = Invoke-WWithConnection $dir $connectionString @($shell, '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', './build.ps1', 'db-update')
            if (-not $applied) {
                Write-Warning ("w: database not ready. Is PostgreSQL running? Then, inside ${dir}: " +
                    "set `$env:$($script:WConnectionVariable) to the connection string from launchSettings.json and run ./build.ps1 db-update")
            }
        }
    }

    Write-Host ''
    Write-Host "worktree ready: $(Split-Path -Leaf $dir)"
    Write-Host "  branch    $Branch"
    Write-Host "  http      http://localhost:$httpPort"
    Write-Host "  https     https://localhost:$httpsPort"
    Write-Host "  database  $database"
    Write-Host "  cd `"$dir`"; ./build.ps1 run"
}

function w-rm {
    param(
        [Parameter(Mandatory)][string]$Branch,
        [switch]$DropDb
    )

    $ErrorActionPreference = 'Continue'
    Assert-WBranch $Branch
    $mainRoot = Get-WMainRoot
    $dir = Get-WWorktreeDir $mainRoot $Branch
    if (-not (Test-Path -LiteralPath $dir)) {
        throw "w-rm: $dir does not exist"
    }
    $fullDir = (Resolve-Path -LiteralPath $dir).Path.TrimEnd('\') + '\'
    $here = (Get-Location).ProviderPath.TrimEnd('\') + '\'
    if ($here.StartsWith($fullDir, [StringComparison]::OrdinalIgnoreCase)) {
        throw "w-rm: you are inside $dir. cd out of it first."
    }

    if ($DropDb) {
        $launchSettings = Join-Path $dir $script:WLaunchSettings
        $settings = if (Test-Path -LiteralPath $launchSettings) { Read-WLaunchSettings $launchSettings }
        $connectionString = if ($settings) { Get-WConnectionString $settings }
        if (-not $connectionString) {
            throw "w-rm: no connection string in $launchSettings - nothing to drop"
        }
        $database = Get-WDatabaseName $settings
        Write-Host "This deletes the database '$database' and all its data."
        $answer = Read-Host 'Type the database name to confirm'
        if ($answer -cne $database) {
            throw 'w-rm: names do not match. Nothing was changed.'
        }
        $dropped = Invoke-WWithConnection $dir $connectionString @(
            'dotnet', 'ef', 'database', 'drop', '--force',
            '--project', 'src/FinNavis.Infrastructure',
            '--startup-project', 'src/FinNavis.Presentation')
        if (-not $dropped) {
            throw 'w-rm: could not drop the database. The worktree is kept.'
        }
    }

    git -C $mainRoot worktree remove $dir
    if ($LASTEXITCODE -ne 0) {
        throw "w-rm: git refused. Commit or discard the changes in $dir first."
    }
    Write-Host "w-rm: removed $dir"

    git -C $mainRoot branch -d $Branch *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "w-rm: branch $Branch kept - it is not merged into the current branch. After a squash merge this is expected. Delete it with: git branch -D $Branch"
    } else {
        Write-Host "w-rm: deleted branch $Branch"
    }
}
