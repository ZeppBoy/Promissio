param([switch]$NoRestore)

$ErrorActionPreference = 'Stop'
function Invoke-DotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}

$previousDiffEngineSetting = $env:DiffEngine_Disabled
$env:DiffEngine_Disabled = 'true' # Compare snapshots without launching desktop diff tools.
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    if (-not $NoRestore) { Invoke-DotNet @('restore', 'Promissio.slnx') }
    Invoke-DotNet @('build', 'Promissio.slnx', '--no-restore')
    Invoke-DotNet @('test', 'Promissio.slnx', '--no-build', '--logger', 'trx', '--results-directory', 'TestResults')
    Invoke-DotNet @('format', 'Promissio.slnx', '--no-restore', '--verify-no-changes', '--include',
        'src/Promissio.Domain/ScheduleGeneration',
        'src/Promissio.Domain/ValueObjects/HolidayCalendar.cs',
        'tests/Promissio.Domain.Tests/ScheduleGeneration',
        'tests/Promissio.Domain.Tests/ValueObjects/HolidayCalendarTests.cs',
        'src/Promissio.BatchProcessor', 'tests/Promissio.BatchProcessor.Tests', 'benchmarks')
    & "$PSScriptRoot/check-documentation.ps1"
    foreach ($project in @('Promissio.Domain.Benchmarks', 'Promissio.Benchmarks')) {
        Invoke-DotNet @('run', '--project', "benchmarks/$project", '--no-build', '--', '--list', 'flat')
    }
    Write-Host 'Baseline verification passed. See docs/verification for assurance limits.'
} finally {
    Pop-Location
    $env:DiffEngine_Disabled = $previousDiffEngineSetting
}
