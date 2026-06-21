param(
    [string]$AppConfig = 'C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json',
    [string]$ReloadedMods = 'C:\Reloaded-II\Mods',
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles',
    [string]$DataModId = 'fftivc.generic.chronicle',
    [string]$CodeModId = 'fftivc.generic.chronicle.codemod',
    [string]$ModLoaderId = 'fftivc.utility.modloader',
    [string]$ProcessName = 'FFT_enhanced'
)

# Read-only/live-safe readiness check for the death gate (docs/modding/07 Test 2b).
# This script validates local artifacts and installed state, but does not deploy, edit AppConfig,
# remove generated packs, touch saves, or launch the game.
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

function Resolve-RepoPath {
    param([string]$Path)

    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }

    return [IO.Path]::GetFullPath((Join-Path $repo $Path))
}

function Test-HashMatch {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        return [pscustomobject]@{ Source = $Source; Destination = $Destination; Status = 'missing-source' }
    }
    if (-not (Test-Path -LiteralPath $Destination)) {
        return [pscustomobject]@{ Source = $Source; Destination = $Destination; Status = 'missing-installed' }
    }

    $srcHash = (Get-FileHash -LiteralPath $Source).Hash
    $dstHash = (Get-FileHash -LiteralPath $Destination).Hash
    [pscustomobject]@{
        Source = $Source
        Destination = $Destination
        Status = $(if ($srcHash -eq $dstHash) { 'match' } else { 'DIFF' })
    }
}

$repo = Split-Path -Parent $PSScriptRoot
$neuterSpotcheck = Resolve-RepoPath 'work\battle-runtime-settings.neuter-spotcheck.json'
$deathCapture = Resolve-RepoPath 'work\battle-runtime-settings.death-flag-capture.json'
$deathHpOnly = Resolve-RepoPath 'work\battle-runtime-settings.death-test.json'
$deathKillFlag = Resolve-RepoPath 'work\battle-runtime-settings.death-test-killflag.json'
$runtimeProfiles = @(
    [pscustomobject]@{ Name = 'neuter-spotcheck'; Path = $neuterSpotcheck; Purpose = 'safe placeholder dry-run' }
    [pscustomobject]@{ Name = 'death-flag-capture'; Path = $deathCapture; Purpose = 'observe vanilla deaths only' }
    [pscustomobject]@{ Name = 'death-test-hp-only'; Path = $deathHpOnly; Purpose = 'force foe HP=0, no KO flag write' }
    [pscustomobject]@{ Name = 'death-test-killflag'; Path = $deathKillFlag; Purpose = 'force foe HP=0 and write KO flag' }
)
$neuterSpotcheckScenarios = Resolve-RepoPath 'docs\modding\examples\runtime-simulation-neuter-spotcheck.example.json'
$deathGateScenarios = Resolve-RepoPath 'docs\modding\examples\runtime-simulation-death-gate.example.json'
$weaponXml = Resolve-RepoPath 'mod\fftivc.generic.chronicle\FFTIVC\tables\enhanced\ItemWeaponData.xml'
$chargeAimXml = Resolve-RepoPath 'mod\fftivc.generic.chronicle\FFTIVC\tables\enhanced\AbilityChargeAimData.xml'
$abilityNxd = Resolve-RepoPath 'mod\fftivc.generic.chronicle\FFTIVC\data\enhanced\nxd\overrideabilityactiondata.nxd'
$localCodeDll = Resolve-RepoPath "codemod\_build\$CodeModId\$CodeModId.dll"
$testNeuterScript = Resolve-RepoPath 'tools\test_neuter_data.py'
$settingsValidateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj'
$settingsSimulateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj'

Write-Host "Generic Chronicle death gate readiness (read-only)" -ForegroundColor Cyan
Write-Host "Repo: $repo" -ForegroundColor DarkGray

Write-Host ""
Write-Host "Processes" -ForegroundColor Green
$processes = Get-Process -Name 'Reloaded-II', $ProcessName -ErrorAction SilentlyContinue
if ($processes) {
    $processes | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize
    Write-Host "Close Reloaded-II and FFT before deploying changed DLL/data artifacts." -ForegroundColor Yellow
}
else {
    Write-Host "Reloaded-II and $ProcessName are not running." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Offline artifact validation" -ForegroundColor Green
Invoke-Native 'python' @($testNeuterScript)
Invoke-Native 'dotnet' @(
    'run',
    '--project',
    $settingsValidateProject,
    '-c',
    'Release',
    '--',
    $neuterSpotcheck,
    $deathCapture,
    $deathHpOnly,
    $deathKillFlag
)
Invoke-Native 'dotnet' @(
    'run',
    '--project',
    $settingsSimulateProject,
    '-c',
    'Release',
    '--',
    $neuterSpotcheck,
    $neuterSpotcheckScenarios,
    '--no-trace'
)
Invoke-Native 'dotnet' @(
    'run',
    '--project',
    $settingsSimulateProject,
    '-c',
    'Release',
    '--',
    $deathHpOnly,
    $deathGateScenarios,
    '--no-trace'
)
Invoke-Native 'dotnet' @(
    'run',
    '--project',
    $settingsSimulateProject,
    '-c',
    'Release',
    '--',
    $deathKillFlag,
    $deathGateScenarios,
    '--no-trace'
)

Write-Host ""
Write-Host "Reloaded app config" -ForegroundColor Green
if (Test-Path -LiteralPath $AppConfig) {
    $app = Get-Content -Raw -LiteralPath $AppConfig | ConvertFrom-Json
    $enabled = @($app.EnabledMods)
    [pscustomobject]@{
        Path = $AppConfig
        ModLoaderEnabled = ($enabled -contains $ModLoaderId)
        DataModEnabled = ($enabled -contains $DataModId)
        CodeModEnabled = ($enabled -contains $CodeModId)
        EnabledMods = ($enabled -join ', ')
    } | Format-List
}
else {
    Write-Host "Missing AppConfig: $AppConfig" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Installed data mod hashes" -ForegroundColor Green
$installedData = Join-Path $ReloadedMods $DataModId
$hashRows = @(
    Test-HashMatch $weaponXml (Join-Path $installedData 'FFTIVC\tables\enhanced\ItemWeaponData.xml')
    Test-HashMatch $chargeAimXml (Join-Path $installedData 'FFTIVC\tables\enhanced\AbilityChargeAimData.xml')
    Test-HashMatch $abilityNxd (Join-Path $installedData 'FFTIVC\data\enhanced\nxd\overrideabilityactiondata.nxd')
)
$hashRows | Select-Object Status, Source, Destination | Format-Table -AutoSize
if ($hashRows.Status -contains 'DIFF' -or $hashRows.Status -contains 'missing-installed') {
    Write-Host "Installed data mod is not identical to repo source; deploy.ps1 is needed before Test 2b." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Installed code mod" -ForegroundColor Green
$installedCode = Join-Path $ReloadedMods $CodeModId
$modDll = Join-Path $installedCode "$CodeModId.dll"
if (Test-Path -LiteralPath $modDll) {
    Get-Item -LiteralPath $modDll | Select-Object FullName, Length, LastWriteTime | Format-List

    Write-Host "Installed code mod hash:" -ForegroundColor Green
    $codeHash = Test-HashMatch $localCodeDll $modDll
    $codeHash | Select-Object Status, Source, Destination | Format-Table -AutoSize
    if ($codeHash.Status -eq 'missing-source') {
        Write-Host "Local code mod build output is missing; run codemod\run-offline-checks.ps1 or codemod\build-deploy.ps1 before judging installed DLL freshness." -ForegroundColor Yellow
    }
    elseif ($codeHash.Status -ne 'match') {
        Write-Host "Installed code mod DLL differs from the local Release build; redeploy before live testing." -ForegroundColor Yellow
    }
}
else {
    Write-Host "Missing installed code mod DLL: $modDll" -ForegroundColor Yellow
}

$installedSettings = Join-Path $installedCode 'battle-runtime-settings.json'
if (Test-Path -LiteralPath $installedSettings) {
    Write-Host "Installed runtime settings:" -ForegroundColor Green
    Get-Item -LiteralPath $installedSettings | Select-Object FullName, Length, LastWriteTime | Format-List

    $installedSettingsHash = (Get-FileHash -LiteralPath $installedSettings).Hash
    $profileRows = foreach ($profile in $runtimeProfiles) {
        if (-not (Test-Path -LiteralPath $profile.Path)) {
            [pscustomobject]@{
                Profile = $profile.Name
                Status = 'missing-source'
                Purpose = $profile.Purpose
                Source = $profile.Path
            }
            continue
        }

        $profileHash = (Get-FileHash -LiteralPath $profile.Path).Hash
        [pscustomobject]@{
            Profile = $profile.Name
            Status = $(if ($profileHash -eq $installedSettingsHash) { 'INSTALLED' } else { 'different' })
            Purpose = $profile.Purpose
            Source = $profile.Path
        }
    }

    Write-Host "Installed settings profile match:" -ForegroundColor Green
    $profileRows | Select-Object Status, Profile, Purpose, Source | Format-Table -AutoSize
    $installedMatches = @($profileRows | Where-Object { $_.Status -eq 'INSTALLED' })
    if ($installedMatches.Count -eq 0) {
        Write-Host "Installed runtime settings do not match any known death-gate profile; redeploy the intended profile before live testing." -ForegroundColor Yellow
    }

    try {
        $settings = Get-Content -Raw -LiteralPath $installedSettings | ConvertFrom-Json
        $deathStateWriteCount = 0
        if ($null -ne $settings.DeathStateWrites) {
            $deathStateWriteCount = @($settings.DeathStateWrites).Count
        }
        [pscustomobject]@{
            DryRunRewrites = $settings.DryRunRewrites
            RewriteObservedDamage = $settings.RewriteObservedDamage
            AffectFoes = $settings.AffectFoes
            AffectAllies = $settings.AffectAllies
            FinalDamageFormula = $settings.FinalDamageFormula
            CaptureStructOnDeath = $settings.CaptureStructOnDeath
            CauseDeathOnZeroHp = $settings.CauseDeathOnZeroHp
            DeathStateWrites = $deathStateWriteCount
        } | Format-List
    }
    catch {
        Write-Host "Could not parse installed runtime settings JSON: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
else {
    Write-Host "No installed battle-runtime-settings.json found yet." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Game log" -ForegroundColor Green
$log = Join-Path $GameDir 'battleprobe_log.txt'
if (Test-Path -LiteralPath $log) {
    Get-Item -LiteralPath $log | Select-Object FullName, Length, LastWriteTime | Format-List
}
else {
    Write-Host "No battleprobe_log.txt found." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Useful live commands" -ForegroundColor Green
Write-Host "codemod\prepare-death-gate.ps1 -NeuterSpotcheck"
Write-Host "python tools\watch_live_mapping.py --runtime-events 0 --placeholder-rewrites 3 --max-placeholder-damage 30 --max-large-vanilla-rewrites 0 --max-rewrite-failures 0"
Write-Host "python tools\analyze_battleprobe_log.py"
Write-Host "  Review: Neuter Placeholder Check, Runtime Context Summary, Formula Trace Variables."
Write-Host "codemod\prepare-death-gate.ps1"
Write-Host "python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --max-rewrite-failures 0"
Write-Host "python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --max-death-events 0 --settle-seconds 2 --max-rewrite-failures 0"
Write-Host "codemod\prepare-death-gate.ps1 -KillFlag"
Write-Host "python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --death-writes 1 --max-rewrite-failures 0 --max-death-write-failures 0"
Write-Host "python tools\analyze_battleprobe_log.py"
Write-Host "  Review: HP Write Proof Check, Death State, Runtime Context Summary."
