param(
    [string]$AppConfig = 'C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json',
    [string]$ReloadedMods = 'C:\Reloaded-II\Mods',
    [string]$ModId = 'fftivc.generic.chronicle.codemod',
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles',
    [string]$ProcessName = 'FFT_enhanced',
    [string]$LiveNoopSettings = 'work\battle-runtime-settings.v0.2.scan.live-noop.json',
    [string]$PolicySettings = 'work\battle-runtime-settings.v0.2.scan.generated.json',
    [string]$ExactNoopSettings = 'work\battle-runtime-settings.v0.2.live-noop.exact-from-log.json',
    [string]$ExactPolicySettings = 'work\battle-runtime-settings.v0.2.policy.exact-from-log.json'
)

# Read-only diagnostics for the Generic Chronicle live mapping loop.
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

function Get-LoadedModuleStatus {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$ModuleName
    )

    try {
        $module = $Process.Modules | Where-Object { $_.ModuleName -ieq $ModuleName } | Select-Object -First 1
        if ($module) {
            return $module.FileName
        }
    }
    catch {
        return "uninspectable: $($_.Exception.Message)"
    }

    return ''
}

$repo = Split-Path -Parent $PSScriptRoot
$liveNoopPath = Resolve-RepoPath $LiveNoopSettings
$policyPath = Resolve-RepoPath $PolicySettings
$exactNoopPath = Resolve-RepoPath $ExactNoopSettings
$exactPolicyPath = Resolve-RepoPath $ExactPolicySettings
$localCodeDll = Resolve-RepoPath "codemod\_build\$ModId\$ModId.dll"
$settingsValidateProject = Resolve-RepoPath 'codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj'

Write-Host "Generic Chronicle live readiness" -ForegroundColor Cyan
Write-Host "Repo: $repo" -ForegroundColor DarkGray

Write-Host ""
Write-Host "Offline profile validation" -ForegroundColor Green
$profilesToValidate = @($liveNoopPath, $policyPath, $exactNoopPath, $exactPolicyPath) | Where-Object { Test-Path -LiteralPath $_ }
if ($profilesToValidate.Count -gt 0) {
    Invoke-Native 'dotnet' (@(
        'run',
        '--project',
        $settingsValidateProject,
        '-c',
        'Release',
        '--'
    ) + $profilesToValidate)
}
else {
    Write-Host "No live mapping profiles found to validate." -ForegroundColor Yellow
}

$processes = Get-Process -Name 'Reloaded-II', $ProcessName -ErrorAction SilentlyContinue
if ($processes) {
    Write-Host ""
    Write-Host "Processes" -ForegroundColor Green
    $processes | Select-Object ProcessName, Id, StartTime, Path | Format-Table -AutoSize
}
else {
    Write-Host "Processes: Reloaded-II and $ProcessName are not running." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Reloaded app config" -ForegroundColor Green
if (Test-Path -LiteralPath $AppConfig) {
    $app = Get-Content -Raw -LiteralPath $AppConfig | ConvertFrom-Json
    $enabled = @($app.EnabledMods)
    $sorted = @($app.SortedMods)
    [pscustomobject]@{
        Path = $AppConfig
        Length = (Get-Item -LiteralPath $AppConfig).Length
        LastWriteTime = (Get-Item -LiteralPath $AppConfig).LastWriteTime
        CodeModEnabled = ($enabled -contains $ModId)
        EnabledMods = ($enabled -join ', ')
        SortedMods = ($sorted -join ', ')
    } | Format-List
}
else {
    Write-Host "Missing: $AppConfig" -ForegroundColor Yellow
}

$modDir = Join-Path $ReloadedMods $ModId
$modDll = Join-Path $modDir "$ModId.dll"
Write-Host ""
Write-Host "Installed code mod" -ForegroundColor Green
if (Test-Path -LiteralPath $modDll) {
    Get-Item -LiteralPath $modDll | Select-Object FullName, Length, LastWriteTime | Format-List

    Write-Host "Installed code mod hash:" -ForegroundColor Green
    $codeHash = Test-HashMatch $localCodeDll $modDll
    $codeHash | Select-Object Status, Source, Destination | Format-Table -AutoSize
    if ($codeHash.Status -eq 'missing-source') {
        Write-Host "Local code mod build output is missing; run codemod\run-offline-checks.ps1 or codemod\build-deploy.ps1 before judging installed DLL freshness." -ForegroundColor Yellow
    }
    elseif ($codeHash.Status -ne 'match') {
        Write-Host "Installed code mod DLL differs from the local Release build; redeploy before live mapping." -ForegroundColor Yellow
    }
}
else {
    Write-Host "Missing: $modDll" -ForegroundColor Yellow
}

$installedSettings = Join-Path $modDir 'battle-runtime-settings.json'
Write-Host ""
Write-Host "Installed runtime settings" -ForegroundColor Green
if (Test-Path -LiteralPath $installedSettings) {
    Get-Item -LiteralPath $installedSettings | Select-Object FullName, Length, LastWriteTime | Format-List

    $installedSettingsHash = (Get-FileHash -LiteralPath $installedSettings).Hash
    $runtimeProfiles = @(
        [pscustomobject]@{ Name = 'scan-live-noop'; Path = $liveNoopPath; Purpose = 'vanilla-preserving slot/response mapping' }
        [pscustomobject]@{ Name = 'scan-policy'; Path = $policyPath; Purpose = 'scan-slot response policy, rewrites HP' }
        [pscustomobject]@{ Name = 'exact-live-noop'; Path = $exactNoopPath; Purpose = 'promoted exact-offset live noop' }
        [pscustomobject]@{ Name = 'exact-policy'; Path = $exactPolicyPath; Purpose = 'promoted exact-offset response policy' }
    )
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
        Write-Host "Installed runtime settings do not match the known live-mapping profiles; redeploy the intended profile before live testing." -ForegroundColor Yellow
    }

    try {
        $settings = Get-Content -Raw -LiteralPath $installedSettings | ConvertFrom-Json
        [pscustomobject]@{
            DryRunRewrites = $settings.DryRunRewrites
            RewriteObservedDamage = $settings.RewriteObservedDamage
            AffectAllies = $settings.AffectAllies
            AffectFoes = $settings.AffectFoes
            FinalDamageFormula = $settings.FinalDamageFormula
            InferAttackerFromRecentUnits = $settings.InferAttackerFromRecentUnits
            LogResolvedRuntimeContext = $settings.LogResolvedRuntimeContext
            ApplyDamageResponseRules = $settings.ApplyDamageResponseRules
            ApplyEquipmentDr = $settings.ApplyEquipmentDr
            EquipmentSlots = @($settings.EquipmentSlots).Count
            AttackerEquipmentSlots = @($settings.AttackerEquipmentSlots).Count
            ActionSignalRules = @($settings.ActionSignalRules).Count
            DamageResponseRules = @($settings.DamageResponseRules).Count
            FormulaTraceVariables = @($settings.FormulaTraceVariables).Count
        } | Format-List
    }
    catch {
        Write-Host "Could not parse installed runtime settings JSON: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
else {
    Write-Host "No installed battle-runtime-settings.json found yet." -ForegroundColor Yellow
}

$game = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
if ($game) {
    $loadedPath = Get-LoadedModuleStatus -Process $game -ModuleName "$ModId.dll"
    Write-Host ""
    Write-Host "Current game process" -ForegroundColor Green
    [pscustomobject]@{
        ProcessId = $game.Id
        CodeModLoaded = -not [string]::IsNullOrWhiteSpace($loadedPath)
        LoadedPath = $loadedPath
    } | Format-List
}

Write-Host ""
Write-Host "Game-side generated packs" -ForegroundColor Green
$dataDir = Join-Path $GameDir 'data'
if (Test-Path -LiteralPath $dataDir) {
    $packs = Get-ChildItem -LiteralPath $dataDir -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'modded*' } |
        Select-Object FullName, Length, LastWriteTime
    if ($packs) {
        $packs | Format-Table -AutoSize
    }
    else {
        Write-Host "No modded* files or folders found under $dataDir" -ForegroundColor DarkGray
    }
}
else {
    Write-Host "Missing game data dir: $dataDir" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Runtime log" -ForegroundColor Green
$log = Join-Path $GameDir 'battleprobe_log.txt'
if (Test-Path -LiteralPath $log) {
    Get-Item -LiteralPath $log | Select-Object FullName, Length, LastWriteTime | Format-List
}
else {
    Write-Host "No battleprobe_log.txt found." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Useful live mapping commands" -ForegroundColor Green
Write-Host "codemod\prepare-live-mapping.ps1 -DryRun"
Write-Host "codemod\prepare-live-mapping.ps1"
Write-Host "python tools\watch_live_mapping.py --runtime-events 3 --target-slots-present 3 --attacker-slots-present 3 --response-events 1 --require-trace-var trace.finaldamage --max-rewrite-failures 0"
Write-Host "python tools\analyze_battleprobe_log.py"
Write-Host "  Review: Runtime Context Summary, DR/Response Proof Check, Slot Recommendations."
Write-Host "python tools\promote_runtime_offsets.py --min-events 3 --base-settings work\battle-runtime-settings.v0.2.scan.live-noop.json --output work\battle-runtime-settings.v0.2.live-noop.exact-from-log.json --also-policy --policy-base-settings work\battle-runtime-settings.v0.2.scan.generated.json --policy-output work\battle-runtime-settings.v0.2.policy.exact-from-log.json"
