param(
    [ValidateSet('Journey', 'Visual', 'Soak', 'Live', 'Standalone')][string]$Suite = 'Journey',
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $Unity)) { throw 'Unity editor was not found.' }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile')) {
    throw 'Close the Unity editor before starting an isolated scene/live suite.'
}
if ($Suite -eq 'Live' -and (-not $env:SGH_PHOTON_APP_ID -or -not $env:SGH_PHOTON_CHAT_APP_ID)) {
    throw 'Set dedicated SGH_PHOTON_APP_ID and SGH_PHOTON_CHAT_APP_ID in this process environment.'
}
$directory = Join-Path $projectRoot 'TestResults'
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$resultPath = Join-Path $directory "extended-$Suite.xml"
$logPath = Join-Path $directory "extended-$Suite.log"
if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
$assembly = if ($Suite -eq 'Live') { 'SGH.Tests.Live' } else { 'SGH.Tests.System' }
$category = if ($Suite -eq 'Live') { '!KnownGap' } elseif ($Suite -eq 'Standalone') { 'Journey' } else { $Suite }
$platform = if ($Suite -eq 'Standalone') { 'StandaloneWindows64' } else { 'PlayMode' }
# Scene/visual tests require a graphics context. Do not pass -nographics or -quit.
$unityArgs = @('-batchmode', '-projectPath', ('"' + $projectRoot + '"'), '-runTests',
    '-testPlatform', $platform, '-assemblyNames', $assembly, '-testCategory', $category,
    '-testResults', ('"' + $resultPath + '"'), '-logFile', ('"' + $logPath + '"'), '-job-worker-count', '2')
$process = Start-Process -FilePath $Unity -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
$process.WaitForExit()
if (-not (Test-Path -LiteralPath $resultPath)) { throw "No XML results produced; inspect $logPath" }
[xml]$results = Get-Content -LiteralPath $resultPath
Write-Host "$Suite : $($results.'test-run'.passed) passed, $($results.'test-run'.failed) failed"
if ($process.ExitCode -ne 0 -or [int]$results.'test-run'.failed -gt 0 -or [int]$results.'test-run'.total -eq 0) { exit 1 }
