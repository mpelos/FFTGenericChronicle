param(
    [string]$RuntimeSettings = '',
    [switch]$SuppressGameRunningWarning,
    [switch]$AllowReloadedOpen,
    [switch]$SkipRuntimeSettingsValidation
)

# Build the Generic Chronicle code mod and deploy it straight into Reloaded-II's Mods folder.
# Usage:
#   .\build-deploy.ps1
#   .\build-deploy.ps1 -RuntimeSettings work\battle-runtime-settings.v0.2.scan.live-noop.json
$ErrorActionPreference = 'Stop'

$proj = Join-Path $PSScriptRoot 'fftivc.generic.chronicle.codemod'
$repo = Split-Path -Parent $PSScriptRoot
$mods = 'C:\Reloaded-II\Mods'
$modDir = Join-Path $mods 'fftivc.generic.chronicle.codemod'
$knownGameLog = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt'
$runningGame = Get-Process -Name 'FFT_enhanced' -ErrorAction SilentlyContinue
$runningReloaded = Get-Process -Name 'Reloaded-II' -ErrorAction SilentlyContinue
$runtimeSettingsSource = ''

Write-Host "Building code mod -> $modDir" -ForegroundColor Cyan
if ($runningReloaded -and -not $AllowReloadedOpen) {
    Write-Host "Reloaded-II is running and may lock files in $modDir." -ForegroundColor Yellow
    Write-Host "Close Reloaded-II, then rerun this helper for a clean deploy. Use -AllowReloadedOpen only for a best-effort build." -ForegroundColor Yellow
    exit 1
}
elseif ($runningReloaded) {
    Write-Host "Reloaded-II is running; deploy may be partial if files are locked." -ForegroundColor Yellow
}

if ($runningGame -and -not $SuppressGameRunningWarning) {
    Write-Host "FFT_enhanced is currently running. Restart it through Reloaded-II before testing; this process will keep the already-loaded DLL." -ForegroundColor Yellow
}

if ($RuntimeSettings) {
    $runtimeSettingsSource = $RuntimeSettings
    if (-not [IO.Path]::IsPathRooted($runtimeSettingsSource)) {
        $runtimeSettingsSource = Join-Path $repo $runtimeSettingsSource
    }
    $runtimeSettingsSource = [IO.Path]::GetFullPath($runtimeSettingsSource)
    if (-not (Test-Path -LiteralPath $runtimeSettingsSource)) {
        Write-Host "Runtime settings not found: $runtimeSettingsSource" -ForegroundColor Red
        exit 1
    }

    if (-not $SkipRuntimeSettingsValidation) {
        Write-Host "Validating runtime settings -> $runtimeSettingsSource" -ForegroundColor Cyan
        $validator = Join-Path $PSScriptRoot 'fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj'
        dotnet run --project "$validator" -c Release -- "$runtimeSettingsSource"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Runtime settings validation failed; deploy aborted." -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
}

dotnet build "$proj" -c Release -p:DeployToReloaded=true -p:RELOADEDIIMODS="$mods"
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED" -ForegroundColor Red; exit 1 }

if ($runtimeSettingsSource) {
    $target = Join-Path $modDir 'battle-runtime-settings.json'
    if (Test-Path -LiteralPath $target) {
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $backup = "$target.bak-$stamp"
        Copy-Item -LiteralPath $target -Destination $backup -Force
        Write-Host "Backed up existing settings -> $backup" -ForegroundColor DarkYellow
    }

    Copy-Item -LiteralPath $runtimeSettingsSource -Destination $target -Force
    Write-Host "Installed runtime settings -> $target" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "OK. In Reloaded-II: enable 'Generic Chronicle (Battle Probe)' for FFT_enhanced.exe, then launch." -ForegroundColor Green
Write-Host "Runtime log is written next to the game exe: battleprobe_log.txt" -ForegroundColor Yellow
Write-Host "Known local log path: $knownGameLog" -ForegroundColor Yellow
