param(
    [string]$RuntimeSettings = 'work\battle-runtime-settings.death-test.json',
    [string]$GameLog = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt',
    [string]$ReloadedMods = 'C:\Reloaded-II\Mods',
    [string]$ProcessName = 'FFT_enhanced',
    [switch]$KillFlag,
    [switch]$NeuterSpotcheck,
    [switch]$SkipNxdRebuild,
    [switch]$NoArchiveLog,
    [switch]$DryRun
)

# Prepare the live death gate (docs/modding/07 Test 2b), or with -NeuterSpotcheck, the safe
# dry-run placeholder validation that should be run before the HP=0 proof.
# This helper intentionally does not edit Reloaded-II AppConfig, launch the game, touch saves, or
# remove game-side modded packs. It only deploys repo-built artifacts when explicitly run without
# -DryRun and when Reloaded-II/FFT are closed.
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
if ($KillFlag -and $NeuterSpotcheck) {
    throw "-KillFlag and -NeuterSpotcheck are mutually exclusive"
}
if ($KillFlag) {
    $RuntimeSettings = 'work\battle-runtime-settings.death-test-killflag.json'
}
elseif ($NeuterSpotcheck) {
    $RuntimeSettings = 'work\battle-runtime-settings.neuter-spotcheck.json'
}
$runtimeSettingsPath = Resolve-RepoPath $RuntimeSettings
$dataModId = 'fftivc.generic.chronicle'
$dataSrc = Resolve-RepoPath "mod\$dataModId"
$dataDst = Join-Path $ReloadedMods $dataModId
$buildDeploy = Join-Path $PSScriptRoot 'build-deploy.ps1'
$buildNeuterScript = Resolve-RepoPath 'tools\build_neuter_data.py'
$testNeuterScript = Resolve-RepoPath 'tools\test_neuter_data.py'
$settingsValidateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj'
$settingsSimulateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj'
$simulationScenarios = Resolve-RepoPath $(if ($NeuterSpotcheck) {
    'docs\modding\examples\runtime-simulation-neuter-spotcheck.example.json'
} else {
    'docs\modding\examples\runtime-simulation-death-gate.example.json'
})

Write-Host "Preparing Generic Chronicle death gate" -ForegroundColor Cyan
Write-Host "Runtime settings: $runtimeSettingsPath" -ForegroundColor DarkGray
if ($DryRun) {
    Write-Host "Dry run: no files will be copied or moved." -ForegroundColor Yellow
}

if (-not (Test-Path -LiteralPath $runtimeSettingsPath)) {
    throw "runtime settings not found: $runtimeSettingsPath"
}
if (-not (Test-Path -LiteralPath $dataSrc)) {
    throw "data mod source not found: $dataSrc"
}

$runningGame = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
$runningReloaded = Get-Process -Name 'Reloaded-II' -ErrorAction SilentlyContinue
if (($runningGame -or $runningReloaded) -and -not $DryRun) {
    Write-Host "Reloaded-II or $ProcessName is running. Close both before preparing the death gate." -ForegroundColor Yellow
    if ($runningReloaded) { $runningReloaded | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize }
    if ($runningGame) { $runningGame | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize }
    exit 1
}

if (-not $SkipNxdRebuild) {
    Invoke-Native 'python' @($buildNeuterScript, '--build-nxd') -SkipWhenDryRun
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
    $simulationScenarios,
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
Write-Host "1. Launch FFT through Reloaded-II."
if ($NeuterSpotcheck) {
    Write-Host "2. Trigger representative neutered damage actions (attack/spell/Throw/Jump/Aim)."
    Write-Host "3. Watch/analyze placeholder evidence:"
    Write-Host "   python tools\watch_live_mapping.py --runtime-events 0 --placeholder-rewrites 3 --max-placeholder-damage 30 --max-large-vanilla-rewrites 0 --max-rewrite-failures 0"
    Write-Host "   python tools\analyze_battleprobe_log.py"
    Write-Host "   Review: Neuter Placeholder Check, Runtime Context Summary, Formula Trace Variables."
}
else {
    Write-Host "2. Trigger a neutered damage action against an enemy."
    Write-Host "3. Watch/analyze evidence:"
    if ($KillFlag) {
        Write-Host "   python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --death-writes 1 --max-rewrite-failures 0 --max-death-write-failures 0"
    }
    else {
        Write-Host "   # Outcome A: HP=0 alone produced death evidence."
        Write-Host "   python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --max-rewrite-failures 0"
        Write-Host "   # Outcome B: lethal HP rewrite happened, but no death evidence appeared after a short settle window."
        Write-Host "   python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --max-death-events 0 --settle-seconds 2 --max-rewrite-failures 0"
    }
    Write-Host "   python tools\analyze_battleprobe_log.py"
    Write-Host "   Review: Neuter Placeholder Check, HP Write Proof Check, Death Gate Outcome, Death State, Runtime Context Summary."
}
