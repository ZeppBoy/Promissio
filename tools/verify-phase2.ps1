param([switch]$NoRestore)

$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)."
    }
}

function Get-LineCoverage {
    param(
        [object[]]$Classes,
        [string]$Label
    )

    $coveredLines = 0
    $validLines = 0
    foreach ($fileGroup in ($Classes | Group-Object filename)) {
        $hitsByLine = @{}
        foreach ($class in $fileGroup.Group) {
            foreach ($line in @($class.lines.line)) {
                $number = [int]$line.number
                $hits = [int]$line.hits
                if (-not $hitsByLine.ContainsKey($number) -or $hits -gt $hitsByLine[$number]) {
                    $hitsByLine[$number] = $hits
                }
            }
        }

        $validLines += $hitsByLine.Count
        $coveredLines += @($hitsByLine.Values | Where-Object { $_ -gt 0 }).Count
    }

    if ($validLines -eq 0) {
        throw "No $Label source lines were found in the coverage report."
    }

    [pscustomobject]@{
        Covered = $coveredLines
        Valid = $validLines
        Percent = 100 * $coveredLines / $validLines
    }
}

$previousDiffEngineSetting = $env:DiffEngine_Disabled
$previousCoverageTelemetrySetting = $env:DOTNET_COVERAGE_TELEMETRY_OPTOUT
$previousCoverageLogoSetting = $env:DOTNET_COVERAGE_NOLOGO
$env:DiffEngine_Disabled = 'true'
$env:DOTNET_COVERAGE_TELEMETRY_OPTOUT = '1'
$env:DOTNET_COVERAGE_NOLOGO = '1'

$repositoryRoot = Split-Path $PSScriptRoot -Parent
$testProject = 'tests/Promissio.Domain.Tests/Promissio.Domain.Tests.csproj'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$coverageDirectory = Join-Path $repositoryRoot "TestResults/phase2-$timestamp"

Push-Location $repositoryRoot
try {
    if (-not $NoRestore) {
        Invoke-DotNet @('restore', $testProject)
        Invoke-DotNet @('tool', 'restore')
    }

    Invoke-DotNet @('build', $testProject, '--no-restore', '-t:Rebuild')
    Invoke-DotNet @('test', $testProject, '--no-build', '--no-restore',
        '--collect:Code Coverage', '--results-directory', $coverageDirectory)

    $coverageFiles = @(Get-ChildItem $coverageDirectory -Recurse -Filter '*.coverage')
    if ($coverageFiles.Count -ne 1) {
        throw "Expected one coverage artifact, found $($coverageFiles.Count)."
    }
    $coverageFile = $coverageFiles[0]
    $coberturaFile = Join-Path $coverageDirectory 'phase2-coverage.cobertura.xml'
    Invoke-DotNet @('coverage', 'merge', '-o', $coberturaFile, '-f', 'cobertura', $coverageFile.FullName)

    [xml]$coverage = Get-Content -Raw $coberturaFile
    $domainRoot = [IO.Path]::GetFullPath(
        (Join-Path $repositoryRoot 'src/Promissio.Domain'))
    $scheduleRoot = [IO.Path]::GetFullPath(
        (Join-Path $repositoryRoot 'src/Promissio.Domain/ScheduleGeneration'))
    $holidayCalendar = [IO.Path]::GetFullPath(
        (Join-Path $repositoryRoot 'src/Promissio.Domain/ValueObjects/HolidayCalendar.cs'))
    $domainClasses = @($coverage.coverage.packages.package.classes.class) | Where-Object {
        $sourceFile = [IO.Path]::GetFullPath([string]$_.filename)
        $sourceFile.StartsWith(
            $domainRoot + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)
    }
    $phase2Classes = $domainClasses | Where-Object {
        $sourceFile = [IO.Path]::GetFullPath([string]$_.filename)
        $sourceFile.StartsWith(
            $scheduleRoot + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase) -or
        $sourceFile.Equals($holidayCalendar, [StringComparison]::OrdinalIgnoreCase)
    }

    $domainCoverage = Get-LineCoverage $domainClasses 'Domain'
    $phase2Coverage = Get-LineCoverage $phase2Classes 'Phase 2'
    Write-Host ("Domain line coverage: {0:N2}% ({1}/{2})" -f
        $domainCoverage.Percent, $domainCoverage.Covered, $domainCoverage.Valid)
    Write-Host ("Phase 2 line coverage: {0:N2}% ({1}/{2})" -f
        $phase2Coverage.Percent, $phase2Coverage.Covered, $phase2Coverage.Valid)
    if ($domainCoverage.Percent -lt 90) {
        throw ("Domain line coverage {0:N2}% is below the 90% gate." -f $domainCoverage.Percent)
    }

    Push-Location 'tests/Promissio.Domain.Tests'
    try {
        Invoke-DotNet @('stryker', '--config-file', '../../stryker-config.json')
    } finally {
        Pop-Location
    }

    Write-Host 'Phase 2 completion gates passed, including the whole-Domain coverage and mutation thresholds.'
} finally {
    Pop-Location
    $env:DiffEngine_Disabled = $previousDiffEngineSetting
    $env:DOTNET_COVERAGE_TELEMETRY_OPTOUT = $previousCoverageTelemetrySetting
    $env:DOTNET_COVERAGE_NOLOGO = $previousCoverageLogoSetting
}
