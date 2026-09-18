$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$files = @(Get-ChildItem -LiteralPath $repositoryRoot -Filter '*.md')
foreach ($folder in @('docs', 'src/docs', 'benchmarks')) {
    $files += Get-ChildItem -LiteralPath (Join-Path $repositoryRoot $folder) -Filter '*.md' -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|BenchmarkDotNet.Artifacts)[\\/]' }
}
$failures = @()
foreach ($file in $files) {
    if ($file.FullName -match '[\\/]docs[\\/](archive|audits)[\\/]' -and $file.Name -ne 'README.md') { continue }
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $content = [regex]::Replace($content, '(?ms)^```.*?^```[^\r\n]*', '')
    foreach ($match in [regex]::Matches($content, '\[[^\]\r\n]*\]\(([^)\r\n]+)\)')) {
        $target = $match.Groups[1].Value.Trim().Trim('<', '>')
        if ($target -match '^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|#|//)') { continue }
        $target = [Uri]::UnescapeDataString(($target -split '#', 2)[0])
        if (-not $target) { continue }
        if (-not (Test-Path -LiteralPath (Join-Path $file.DirectoryName $target))) {
            $failures += "$($file.FullName): $target"
        }
    }
}
if ($failures.Count -gt 0) { throw "Broken local documentation links:`n$($failures -join "`n")" }
Write-Host 'Maintained documentation file links passed (historical bodies, URLs and anchors excluded).'
