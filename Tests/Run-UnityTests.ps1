param(
    [ValidateSet('EditMode', 'PlayMode', 'Both')][string]$Mode = 'Both',
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe',
    [switch]$KnownGaps
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $Unity)) { throw "Unity editor not found: $Unity" }
# A second Unity process cannot open the same project. Use SGH > Tests in the
# open editor, or close that editor before invoking this CI/batch entry point.
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile')) {
    throw 'The project is open in Unity. Use SGH > Tests there, or close Unity before running this script.'
}
$resultsDirectory = Join-Path $projectRoot 'TestResults'
New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
$modes = if ($Mode -eq 'Both') { @('EditMode', 'PlayMode') } else { @($Mode) }
$anyFailures = $false
foreach ($platform in $modes) {
    $suffix = if ($KnownGaps) { '-diagnostics' } else { '' }
    $resultPath = Join-Path $resultsDirectory "sgh-$platform$suffix.xml"
    $logPath = Join-Path $resultsDirectory "sgh-$platform$suffix.log"
    $category = if ($KnownGaps) { 'KnownGap' } else { '!KnownGap' }
    # Unity exits when its test run finishes; -quit can terminate the run early.
    & $Unity -batchmode -projectPath $projectRoot -runTests -testPlatform $platform `
        -assemblyNames "SGH.Tests.$platform" -testCategory $category -testResults $resultPath -logFile $logPath
    if ($LASTEXITCODE -ne 0) { $anyFailures = $true }
    if (-not (Test-Path -LiteralPath $resultPath)) { throw "No test results produced. Inspect $logPath" }
    [xml]$result = Get-Content -LiteralPath $resultPath
    $run = $result.'test-run'
    Write-Host "$platform : $($run.passed) passed, $($run.failed) failed, $($run.skipped) skipped"
    if ([int]$run.failed -gt 0) { $anyFailures = $true }
}
if ($anyFailures) { exit 1 }
