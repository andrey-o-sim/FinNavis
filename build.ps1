# Task runner for FinNavis. Usage: ./build.ps1 [build|test]
# Default task is "build".
[CmdletBinding()]
param(
    [ValidateSet('build', 'test')]
    [string]$Task = 'build'
)

$ErrorActionPreference = 'Stop'
$solution = 'FinNavis.slnx'

switch ($Task) {
    'build' { dotnet build $solution }
    'test'  { dotnet test $solution }
}

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
