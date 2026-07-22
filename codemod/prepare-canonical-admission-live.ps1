param(
    [string]$RuntimeSettings = 'work\1784673033-battle-runtime-settings.canonical-admission-sentinel.json',
    [string]$GameLog = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt',
    [string]$AutosaveSnapshot,
    [switch]$NoArchiveLog,
    [switch]$DryRun
)

# Prepare the narrow canonical-admission/template live proof.
# This helper intentionally does not edit Reloaded-II AppConfig or enable/disable mods.
$ErrorActionPreference = 'Stop'

function Invoke-Native {
    param(
        [string]$Command,
        [string[]]$Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE"
    }
}

function Assert-AutosaveFixtureMetadata {
    param(
        [string]$SnapshotPath,
        [string]$RequiredKind
    )

    if (-not (Test-Path -LiteralPath $SnapshotPath -PathType Leaf)) {
        throw "Autosave snapshot not found: $SnapshotPath"
    }

    $metadataPath = "$SnapshotPath.fixture.json"
    if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
        throw "Autosave snapshot metadata is required for fixture kind '$RequiredKind': $metadataPath"
    }

    $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
    if ([string]$metadata.FixtureKind -ne $RequiredKind) {
        throw "Autosave snapshot fixture kind '$($metadata.FixtureKind)' does not match required kind '$RequiredKind'."
    }

    $currentHash = (Get-FileHash -LiteralPath $SnapshotPath -Algorithm SHA256).Hash
    if ([string]$metadata.Sha256 -ne $currentHash) {
        throw "Autosave snapshot hash does not match its fixture metadata: $SnapshotPath"
    }
}

$repo = Split-Path -Parent $PSScriptRoot
$deploy = Join-Path $PSScriptRoot 'build-deploy.ps1'
$runtimeSettingsPath = if ([IO.Path]::IsPathRooted($RuntimeSettings)) {
    [IO.Path]::GetFullPath($RuntimeSettings)
} else {
    [IO.Path]::GetFullPath((Join-Path $repo $RuntimeSettings))
}
$autosaveSnapshotPath = if ($AutosaveSnapshot) {
    if ([IO.Path]::IsPathRooted($AutosaveSnapshot)) {
        [IO.Path]::GetFullPath($AutosaveSnapshot)
    } else {
        [IO.Path]::GetFullPath((Join-Path $repo $AutosaveSnapshot))
    }
} else {
    $null
}

Write-Host "Preparing canonical admission live proof" -ForegroundColor Cyan
Write-Host "Runtime settings: $runtimeSettingsPath" -ForegroundColor DarkGray
if ($DryRun) {
    Write-Host "Dry run: no deploy or log archive will be performed." -ForegroundColor Yellow
}

if (-not (Test-Path -LiteralPath $runtimeSettingsPath)) {
    throw "Runtime settings not found: $runtimeSettingsPath"
}
if ($autosaveSnapshotPath) {
    Assert-AutosaveFixtureMetadata -SnapshotPath $autosaveSnapshotPath -RequiredKind 'canonical-admission-pre-action'
    $autosaveManager = Join-Path $repo 'tools\manage_fft_enhanced_autosave.ps1'
    $restoreArgs = @{
        Action = 'Restore'
        SnapshotPath = $autosaveSnapshotPath
        RequireFixtureKind = 'canonical-admission-pre-action'
    }
    if ($DryRun) {
        Write-Host "Dry run: would restore verified canonical-admission pre-action autosave fixture:" -ForegroundColor DarkGray
        Write-Host "  $autosaveManager -Action Restore -SnapshotPath $autosaveSnapshotPath -RequireFixtureKind canonical-admission-pre-action"
    }
    else {
        & $autosaveManager @restoreArgs
        if (-not $?) {
            throw 'Autosave fixture restore failed.'
        }
    }
}

Invoke-Native 'python' @(
    'tools\analyze_dcl_canonical_admission_probe_readiness.py',
    $runtimeSettingsPath,
    '--check-only'
)

if ($DryRun) {
    Write-Host "Dry run: would deploy code mod with admission sentinel settings:" -ForegroundColor DarkGray
    Write-Host "  $deploy -RuntimeSettings $runtimeSettingsPath -SuppressGameRunningWarning"
}
else {
    & $deploy -RuntimeSettings $runtimeSettingsPath -SuppressGameRunningWarning
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (-not $NoArchiveLog) {
    if ($DryRun) {
        Write-Host "Dry run: would archive old game log if present: $GameLog" -ForegroundColor DarkGray
    }
    elseif (Test-Path -LiteralPath $GameLog) {
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $backup = "$GameLog.bak-canonical-admission-$stamp"
        Move-Item -LiteralPath $GameLog -Destination $backup -Force
        Write-Host "Archived old game log -> $backup" -ForegroundColor DarkYellow
    }
    else {
        Write-Host "No existing game log to archive: $GameLog" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Next live step:" -ForegroundColor Green
Write-Host "1. Launch Enhanced through Reloaded-II."
Write-Host "2. Restore/load a verified pre-action canonical-admission battle fixture; Manual Save 05 is only a world-map baseline."
Write-Host "   Snapshot after manually reaching that state once:"
Write-Host "   tools\manage_fft_enhanced_autosave.ps1 -Action Snapshot -FixtureKind canonical-admission-pre-action -FixtureLabel '<acting unit / target / menu state>'"
Write-Host "   Restore can be delegated to this helper with -AutosaveSnapshot <path>."
Write-Host "3. Use one ordinary Fire (ability 16) on exactly one target and let the result finish."
Write-Host "4. Collect and analyze the fresh log:"
Write-Host "   python tools\collect_dcl_canonical_admission_live_log.py"
Write-Host ""
Write-Host "Fresh log path: $GameLog" -ForegroundColor Yellow
