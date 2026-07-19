param(
    [string]$ReloadedPath = 'C:\Reloaded-II\Reloaded-II.exe',

    [string]$GamePath = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe',

    [string]$AutosaveSnapshot,

    [switch]$ValidateOnly,

    [ValidateRange(1, 60)]
    [int]$ProcessWaitSeconds = 15
)

$ErrorActionPreference = 'Stop'

function Resolve-RequiredFile {
    param(
        [string]$Path,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label was not found: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

$resolvedReloaded = Resolve-RequiredFile -Path $ReloadedPath -Label 'Reloaded-II executable'
$resolvedGame = Resolve-RequiredFile -Path $GamePath -Label 'FFT Enhanced executable'
$appConfigPath = 'C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json'
$resolvedAppConfig = Resolve-RequiredFile -Path $appConfigPath -Label 'Reloaded-II FFT application config'
$appConfig = Get-Content -LiteralPath $resolvedAppConfig -Raw | ConvertFrom-Json

if ([IO.Path]::GetFullPath([string]$appConfig.AppLocation) -ne [IO.Path]::GetFullPath($resolvedGame)) {
    throw "Reloaded-II profile targets '$($appConfig.AppLocation)', not '$resolvedGame'."
}

$runningGame = @(Get-Process -Name 'FFT_enhanced' -ErrorAction SilentlyContinue |
    Where-Object { -not $_.HasExited })
if ($runningGame.Count -gt 0) {
    throw 'FFT_enhanced.exe is already running. Close it before starting a deterministic test launch.'
}

$restoredSnapshot = $null
if ($AutosaveSnapshot) {
    $resolvedSnapshot = Resolve-RequiredFile -Path $AutosaveSnapshot -Label 'Autosave snapshot'
    $autosaveManager = Join-Path $PSScriptRoot 'manage_fft_enhanced_autosave.ps1'
    & $autosaveManager -Action Restore -SnapshotPath $resolvedSnapshot
    if (-not $?) {
        throw 'Autosave restore failed.'
    }
    $restoredSnapshot = $resolvedSnapshot
}

$summary = [ordered]@{
    ReloadedPath = $resolvedReloaded
    GamePath = $resolvedGame
    AppConfigPath = $resolvedAppConfig
    AppConfigSha256 = (Get-FileHash -LiteralPath $resolvedAppConfig -Algorithm SHA256).Hash
    AppArguments = [string]$appConfig.AppArguments
    EnabledMods = @($appConfig.EnabledMods) -join ', '
    RestoredAutosaveSnapshot = $restoredSnapshot
    Launched = $false
    ProcessId = $null
}

if ($ValidateOnly) {
    [pscustomobject]$summary | Format-List
    exit 0
}

# Reloaded-II's supported --launch form skips navigation through its profile UI while preserving
# the profile's enabled-mod list and application arguments.
Start-Process -FilePath $resolvedReloaded -ArgumentList @('--launch', ('"' + $resolvedGame + '"'))

$deadline = [DateTime]::UtcNow.AddSeconds($ProcessWaitSeconds)
$process = $null
while ([DateTime]::UtcNow -lt $deadline) {
    $process = Get-Process -Name 'FFT_enhanced' -ErrorAction SilentlyContinue |
        Where-Object { -not $_.HasExited } |
        Select-Object -First 1
    if ($process) {
        break
    }
    Start-Sleep -Milliseconds 100
}

if (-not $process) {
    throw "Reloaded-II accepted the launch request, but FFT_enhanced.exe did not appear within $ProcessWaitSeconds seconds."
}

$summary.Launched = $true
$summary.ProcessId = $process.Id
[pscustomobject]$summary | Format-List
