param(
    [string]$RuntimeSettings = 'work\battle-runtime-settings.sentinel-coarse-v1.json',
    [string]$SimulationScenarios = 'work\runtime-simulation.sentinel-coarse-v1.json',
    [string]$PlaceholderMode = 'sentinel-coarse-v1',
    [string]$GameLog = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt',
    [string]$ReloadedMods = 'C:\Reloaded-II\Mods',
    [string]$ProcessName = 'FFT_enhanced',
    [switch]$SkipNxdRebuild,
    [switch]$NoArchiveLog,
    [switch]$DryRun
)

# Prepare the action-identity sentinel calibration profile.
# With -DryRun, this validates the profile and prints the live plan without copying files,
# moving logs, rebuilding NXD, touching AppConfig, launching the game, or touching saves.
$ErrorActionPreference = 'Stop'

function Invoke-Native {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [switch]$SkipWhenDryRun
    )

    Write-Host ("> {0} {1}" -f $Command, ($Arguments -join ' ')) -ForegroundColor DarkGray
    if ($DryRun -and $SkipWhenDryRun) {
        Write-Host "Dry run: skipped command." -ForegroundColor DarkGray
        return
    }

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE"
    }
}

function Resolve-RepoPath {
    param([string]$Path)

    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }

    return [IO.Path]::GetFullPath((Join-Path $repo $Path))
}

function Copy-DataMod {
    param(
        [string]$Source,
        [string]$Destination
    )

    Write-Host "Deploying data mod -> $Destination" -ForegroundColor Cyan
    if ($DryRun) {
        Write-Host "Dry run: would copy files from $Source" -ForegroundColor DarkGray
        return
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $files = Get-ChildItem -Recurse -File -LiteralPath $Source | Where-Object { $_.Name -ne '.gitkeep' }
    foreach ($file in $files) {
        $rel = $file.FullName.Substring($Source.Length).TrimStart('\')
        $target = Join-Path $Destination $rel
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }

    $failed = @()
    foreach ($file in $files) {
        $rel = $file.FullName.Substring($Source.Length).TrimStart('\')
        $target = Join-Path $Destination $rel
        $srcHash = (Get-FileHash -LiteralPath $file.FullName).Hash
        $dstHash = (Get-FileHash -LiteralPath $target).Hash
        if ($srcHash -ne $dstHash) {
            $failed += $rel
        }
    }
    if ($failed.Count -gt 0) {
        throw "data mod hash verification failed: $($failed -join ', ')"
    }

    Write-Host "Data mod deploy OK ($($files.Count) file(s))." -ForegroundColor Green
}

$repo = Split-Path -Parent $PSScriptRoot
$runtimeSettingsPath = Resolve-RepoPath $RuntimeSettings
$simulationScenariosPath = Resolve-RepoPath $SimulationScenarios
$dataModId = 'fftivc.generic.chronicle'
$dataSrc = Resolve-RepoPath "mod\$dataModId"
$dataDst = Join-Path $ReloadedMods $dataModId
$buildDeploy = Join-Path $PSScriptRoot 'build-deploy.ps1'
$buildNeuterScript = Resolve-RepoPath 'tools\build_neuter_data.py'
$testNeuterScript = Resolve-RepoPath 'tools\test_neuter_data.py'
$settingsValidateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj'
$settingsSimulateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj'

Write-Host "Preparing Generic Chronicle sentinel coarse calibration" -ForegroundColor Cyan
Write-Host "Runtime settings: $runtimeSettingsPath" -ForegroundColor DarkGray
Write-Host "Placeholder mode: $PlaceholderMode" -ForegroundColor DarkGray
if ($DryRun) {
    Write-Host "Dry run: no files will be copied, moved, deployed, or rebuilt." -ForegroundColor Yellow
}

if (-not (Test-Path -LiteralPath $runtimeSettingsPath)) {
    throw "runtime settings not found: $runtimeSettingsPath"
}
if (-not (Test-Path -LiteralPath $simulationScenariosPath)) {
    throw "simulation scenarios not found: $simulationScenariosPath"
}
if (-not (Test-Path -LiteralPath $dataSrc)) {
    throw "data mod source not found: $dataSrc"
}

$runningGame = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
$runningReloaded = Get-Process -Name 'Reloaded-II' -ErrorAction SilentlyContinue
if (($runningGame -or $runningReloaded) -and -not $DryRun) {
    Write-Host "Reloaded-II or $ProcessName is running. Close both before preparing sentinel calibration." -ForegroundColor Yellow
    if ($runningReloaded) { $runningReloaded | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize }
    if ($runningGame) { $runningGame | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize }
    exit 1
}

if (-not $SkipNxdRebuild) {
    Invoke-Native 'python' @($buildNeuterScript, '--placeholder-mode', $PlaceholderMode, '--build-nxd') -SkipWhenDryRun
}
Invoke-Native 'python' @($testNeuterScript)
Invoke-Native 'dotnet' @(
    'run',
    '--project',
    $settingsValidateProject,
    '-c',
    'Release',
    '--',
    $runtimeSettingsPath
)
Invoke-Native 'dotnet' @(
    'run',
    '--project',
    $settingsSimulateProject,
    '-c',
    'Release',
    '--',
    $runtimeSettingsPath,
    $simulationScenariosPath,
    '--no-trace'
)

Copy-DataMod -Source $dataSrc -Destination $dataDst

Invoke-Native 'powershell' @(
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $buildDeploy,
    '-RuntimeSettings',
    $runtimeSettingsPath,
    '-SuppressGameRunningWarning'
) -SkipWhenDryRun

if (-not $NoArchiveLog) {
    if ($DryRun) {
        Write-Host "Dry run: would archive old log if present: $GameLog" -ForegroundColor DarkGray
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
Write-Host "1. Launch FFT through Reloaded-II with high-HP targets."
Write-Host "2. Trigger one low-band action (sword/knife/katana/etc.), one mid-band action (spear/bow/gun/Jump/Aim), and one high-band action (faith/magic spell)."
Write-Host "3. Watch for action identity, CT attacker, and rewrite evidence:"
Write-Host "   python tools\watch_live_mapping.py --runtime-events 3 --ct-runtime-attackers 1 --action-signals 3 --require-action-signal 301 --require-action-signal 302 --require-action-signal 303 --require-action-var sentinelLow --require-action-var sentinelMid --require-action-var sentinelHigh --require-trace-var trace.finaldamage --placeholder-rewrites 3 --max-placeholder-damage 90 --max-rewrite-failures 0"
Write-Host "4. Analyze the fresh log:"
Write-Host "   python tools\analyze_battleprobe_log.py"
Write-Host "5. Review: Runtime Context Summary -> Attacker sources, Actions, Action Variables, Formula Trace Variables."
Write-Host ""
Write-Host "Fresh log path: $GameLog" -ForegroundColor Yellow
