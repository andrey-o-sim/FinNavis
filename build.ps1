# Task runner for FinNavis. Usage: ./build.ps1 [task] [args]
# Default task is "build". Run "./build.ps1 help" for the full list.
[CmdletBinding()]
param(
    [ValidateSet('build', 'test', 'run', 'migrate', 'db-update', 'clean', 'help')]
    [string]$Task = 'build',

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$TaskArgs = @()
)

$ErrorActionPreference = 'Stop'

$solution = 'FinNavis.slnx'
$apiProject = 'src/FinNavis.Presentation'
$efProject = 'src/FinNavis.Infrastructure'
$migrationsDir = 'Persistence/Migrations'
$launchSettings = "$apiProject/Properties/launchSettings.json"
$launchSettingsExample = "$apiProject/Properties/launchSettings.example.json"

function Show-Usage {
    @'
Usage: ./build.ps1 <task> [args]

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
  Copy-Item src/FinNavis.Presentation/Properties/launchSettings.example.json `
            src/FinNavis.Presentation/Properties/launchSettings.json
'@ | Write-Host
}

# Write-Error would print a stack trace on top of the message. Keep the output plain.
function Stop-WithMessage {
    param([string[]]$Message)

    $Message | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

function Assert-LaunchSettings {
    if (-not (Test-Path $launchSettings)) {
        Stop-WithMessage @(
            "Missing $launchSettings",
            'Copy the template and fill in the values:',
            "  Copy-Item $launchSettingsExample $launchSettings"
        )
    }
}

switch ($Task) {
    'build' {
        dotnet build $solution
    }
    'test' {
        dotnet test $solution
    }
    'run' {
        Assert-LaunchSettings
        dotnet run --project $apiProject
    }
    'migrate' {
        $name = $TaskArgs | Select-Object -First 1
        if ([string]::IsNullOrWhiteSpace($name)) {
            Stop-WithMessage 'migrate needs a name. Example: ./build.ps1 migrate AddAccounts'
        }
        Assert-LaunchSettings
        dotnet ef migrations add $name `
            --project $efProject `
            --startup-project $apiProject `
            --output-dir $migrationsDir
    }
    'db-update' {
        Assert-LaunchSettings
        dotnet ef database update `
            --project $efProject `
            --startup-project $apiProject
    }
    'clean' {
        dotnet clean $solution
    }
    'help' {
        Show-Usage
    }
}

# "help" runs no external command, so $LASTEXITCODE may be unset or left over. Check before using it.
if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
