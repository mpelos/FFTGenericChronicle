param(
    [string]$RuntimeSettings = 'docs\modding\examples\battle-runtime-settings.dry-run.example.json',
    [string]$Scenarios = 'docs\modding\examples\runtime-simulation-dry-run.example.json',
    [string]$GameLog = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt',
    [string]$ProcessName = 'FFT_enhanced',
    [switch]$NoArchiveLog,
    [switch]$DryRun
)

# Prepare the live-safe dry-run evaluation proof.
# This helper does not edit Reloaded-II AppConfig, launch the game, touch saves, or deploy data
# mods. It only deploys the code mod/runtime settings when explicitly run without -DryRun and when
# Reloaded-II/FFT are closed.
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

$repo = Split-Path -Parent $PSScriptRoot
$runtimeSettingsPath = Resolve-RepoPath $RuntimeSettings
$scenariosPath = Resolve-RepoPath $Scenarios
$buildDeploy = Join-Path $PSScriptRoot 'build-deploy.ps1'
$settingsValidateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj'
$settingsSimulateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj'

Write-Host "Preparing Generic Chronicle dry-run evaluation" -ForegroundColor Cyan
Write-Host "Runtime settings: $runtimeSettingsPath" -ForegroundColor DarkGray
Write-Host "Scenarios: $scenariosPath" -ForegroundColor DarkGray
if ($DryRun) {
    Write-Host "Dry run: no files will be copied or moved." -ForegroundColor Yellow
}

if (-not (Test-Path -LiteralPath $runtimeSettingsPath)) {
    throw "runtime settings not found: $runtimeSettingsPath"
}
if (-not (Test-Path -LiteralPath $scenariosPath)) {
    throw "scenario file not found: $scenariosPath"
}

$runningGame = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
$runningReloaded = Get-Process -Name 'Reloaded-II' -ErrorAction SilentlyContinue
if (($runningGame -or $runningReloaded) -and -not $DryRun) {
    Write-Host "Reloaded-II or $ProcessName is running. Close both before preparing dry-run evaluation." -ForegroundColor Yellow
    if ($runningReloaded) { $runningReloaded | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize }
    if ($runningGame) { $runningGame | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize }
    exit 1
}

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
    $scenariosPath,
    '--no-trace'
)

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
Write-Host "2. Trigger one HP damage, one HP healing, one MP loss, and one MP gain event."
Write-Host "3. Watch/analyze dry-run evidence:"
Write-Host "   python tools\watch_live_mapping.py --runtime-events 0 --placeholder-rewrites 1 --hp-healing-rewrites 1 --mp-loss-rewrites 1 --mp-gain-rewrites 1 --max-rewrite-failures 0"
Write-Host "   python tools\analyze_battleprobe_log.py"
Write-Host "   Review: HP Write Proof Check, MP Rewrite Check, Runtime Context Summary, Formula Trace Variables."
