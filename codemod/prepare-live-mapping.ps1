param(
    [string]$RuntimeSettings = 'work\battle-runtime-settings.v0.2.scan.live-noop.json',
    [string]$GameLog = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt',
    [string]$AppConfig = 'C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json',
    [string]$ModId = 'fftivc.generic.chronicle.codemod',
    [string]$ProcessName = 'FFT_enhanced',
    [switch]$EnableModInAppConfig,
    [switch]$NoArchiveLog
)

# Prepare a clean live mapping session:
# - build/deploy the code mod
# - install a vanilla-preserving runtime settings profile
# - archive the old game-side battleprobe_log.txt so the next launch produces unmistakably fresh evidence
$ErrorActionPreference = 'Stop'

function Test-AppModEnabled {
    param(
        [string]$AppConfigPath,
        [string]$ModId
    )

    if (-not (Test-Path -LiteralPath $AppConfigPath)) {
        Write-Host "AppConfig not found; cannot check enabled mod: $AppConfigPath" -ForegroundColor Yellow
        return $false
    }

    $app = Get-Content -Raw -LiteralPath $AppConfigPath | ConvertFrom-Json
    $enabled = @($app.EnabledMods)
    return ($enabled -contains $ModId)
}

function Enable-AppMod {
    param(
        [string]$AppConfigPath,
        [string]$ModId
    )

    if (-not (Test-Path -LiteralPath $AppConfigPath)) {
        Write-Host "AppConfig not found; cannot enable mod: $AppConfigPath" -ForegroundColor Yellow
        return
    }

    $app = Get-Content -Raw -LiteralPath $AppConfigPath | ConvertFrom-Json
    $enabled = @($app.EnabledMods)
    if ($enabled -contains $ModId) {
        Write-Host "AppConfig already enables $ModId" -ForegroundColor DarkGray
        return
    }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backup = "$AppConfigPath.bak-$stamp"
    $temp = "$AppConfigPath.tmp-$stamp"
    try {
        $app.EnabledMods = @($enabled + $ModId)
        $json = $app | ConvertTo-Json -Depth 20
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($temp, $json + [Environment]::NewLine, $utf8NoBom)
        Get-Content -Raw -LiteralPath $temp | ConvertFrom-Json | Out-Null
        [System.IO.File]::Replace($temp, $AppConfigPath, $backup, $false)
        Write-Host "Enabled $ModId in AppConfig -> $AppConfigPath" -ForegroundColor Cyan
        Write-Host "Backed up AppConfig -> $backup" -ForegroundColor DarkYellow
    }
    catch {
        Write-Host "Could not enable $ModId in AppConfig: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "Close FFT/Reloaded-II, then rerun this helper before collecting live evidence." -ForegroundColor Yellow
        if (Test-Path -LiteralPath $temp) {
            Remove-Item -LiteralPath $temp -Force
        }
    }
}

function Test-ProcessLoadedModule {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$ModuleName
    )

    try {
        $match = $Process.Modules | Where-Object { $_.ModuleName -ieq $ModuleName } | Select-Object -First 1
        return ($null -ne $match)
    }
    catch {
        Write-Host "Could not inspect loaded modules for $($Process.ProcessName) pid=$($Process.Id): $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

$repo = Split-Path -Parent $PSScriptRoot
$deploy = Join-Path $PSScriptRoot 'build-deploy.ps1'
$runningGame = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
$runningReloaded = Get-Process -Name 'Reloaded-II' -ErrorAction SilentlyContinue
$modModuleName = "$ModId.dll"

Write-Host "Preparing Generic Chronicle live mapping session" -ForegroundColor Cyan
if ($runningGame) {
    Write-Host "$ProcessName is currently running. Restart it through Reloaded-II before collecting evidence; the current process will not load the freshly built DLL." -ForegroundColor Yellow
    foreach ($process in @($runningGame)) {
        if (Test-ProcessLoadedModule -Process $process -ModuleName $modModuleName) {
            Write-Host "Current $ProcessName pid=$($process.Id) has loaded $modModuleName, but a restart is still needed after rebuilding." -ForegroundColor DarkYellow
        }
        else {
            Write-Host "Current $ProcessName pid=$($process.Id) has not loaded $modModuleName; no live runtime log will be produced by this process." -ForegroundColor Yellow
        }
    }
}

if ($EnableModInAppConfig -and ($runningGame -or $runningReloaded)) {
    Write-Host "Refusing to edit AppConfig while Reloaded-II or $ProcessName is running. Close both, then rerun with -EnableModInAppConfig." -ForegroundColor Yellow
    exit 1
}
elseif ($EnableModInAppConfig) {
    Enable-AppMod -AppConfigPath $AppConfig -ModId $ModId
}
elseif (Test-AppModEnabled -AppConfigPath $AppConfig -ModId $ModId) {
    Write-Host "AppConfig already enables $ModId" -ForegroundColor DarkGray
}
else {
    Write-Host "AppConfig does not enable $ModId. Enable it in Reloaded-II, or rerun this helper with -EnableModInAppConfig after closing Reloaded-II." -ForegroundColor Yellow
}

if ($runningGame -or $runningReloaded) {
    Write-Host "Stopping before deploy because Reloaded-II or $ProcessName is running." -ForegroundColor Yellow
    Write-Host "Close both, rerun this helper, then reopen Reloaded-II and launch FFT for live evidence." -ForegroundColor Yellow
    exit 1
}

& $deploy -RuntimeSettings $RuntimeSettings -SuppressGameRunningWarning
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not $NoArchiveLog) {
    if ($runningGame) {
        Write-Host "Skipped archiving game log because FFT_enhanced is running: $GameLog" -ForegroundColor Yellow
    }
    elseif (Test-Path -LiteralPath $GameLog) {
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $backup = "$GameLog.bak-$stamp"
        Move-Item -LiteralPath $GameLog -Destination $backup -Force
        Write-Host "Archived old game log -> $backup" -ForegroundColor DarkYellow
    }
    else {
        Write-Host "No existing game log to archive: $GameLog" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Next:" -ForegroundColor Green
Write-Host "1. Launch FFT through Reloaded-II and make one or more controlled damage events."
Write-Host "2. Analyze the fresh log:"
Write-Host "   python tools\analyze_battleprobe_log.py"
Write-Host "3. If slots are stable, promote exact settings:"
Write-Host "   python tools\promote_runtime_offsets.py --min-events 3 --base-settings work\battle-runtime-settings.v0.2.scan.live-noop.json --output work\battle-runtime-settings.v0.2.live-noop.exact-from-log.json --also-policy --policy-base-settings work\battle-runtime-settings.v0.2.scan.generated.json --policy-output work\battle-runtime-settings.v0.2.policy.exact-from-log.json"
Write-Host ""
Write-Host "Fresh log path: $GameLog" -ForegroundColor Yellow
