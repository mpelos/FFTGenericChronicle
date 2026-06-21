param(
    [string]$Settings = 'work\battle-runtime-settings.v0.2.generated.json',
    [string]$MatrixResponseSettings = 'work\battle-runtime-settings.v0.2.matrix.generated.json',
    [string]$ScanSettings = 'work\battle-runtime-settings.v0.2.scan.generated.json',
    [string]$GurpsDrSettings = 'docs\modding\examples\battle-runtime-settings.gurps-dr.example.json',
    [string]$StaticDrSettings = 'docs\modding\examples\battle-runtime-settings.static-dr.example.json',
    [string]$MpSettings = 'docs\modding\examples\battle-runtime-settings.mp.example.json',
    [string]$SentinelBandsSettings = 'docs\modding\examples\battle-runtime-settings.sentinel-bands.example.json',
    [string]$DryRunSettings = 'docs\modding\examples\battle-runtime-settings.dry-run.example.json',
    [string]$NeuterSpotcheckSettings = 'work\battle-runtime-settings.neuter-spotcheck.json',
    [string]$DeathHpSettings = 'work\battle-runtime-settings.death-test.json',
    [string]$DeathKillFlagSettings = 'work\battle-runtime-settings.death-test-killflag.json',
    [string]$Scenarios = 'docs\modding\examples\runtime-simulation-scenarios.example.json',
    [string]$MatrixScenarios = 'docs\modding\examples\runtime-simulation-matrix.v0.2.example.json',
    [string]$MatrixResponseScenarios = 'docs\modding\examples\runtime-simulation-matrix-response.v0.2.example.json',
    [string]$GurpsDrScenarios = 'docs\modding\examples\runtime-simulation-gurps-dr.example.json',
    [string]$StaticDrScenarios = 'docs\modding\examples\runtime-simulation-static-dr.example.json',
    [string]$MpScenarios = 'docs\modding\examples\runtime-simulation-mp.example.json',
    [string]$SentinelBandsScenarios = 'docs\modding\examples\runtime-simulation-sentinel-bands.example.json',
    [string]$DryRunScenarios = 'docs\modding\examples\runtime-simulation-dry-run.example.json',
    [string]$NeuterSpotcheckScenarios = 'docs\modding\examples\runtime-simulation-neuter-spotcheck.example.json',
    [string]$DeathGateScenarios = 'docs\modding\examples\runtime-simulation-death-gate.example.json',
    [switch]$SkipPython,
    [switch]$SkipDotNet,
    [switch]$SkipGitDiffCheck
)

# Offline-only regression gate for the Generic Chronicle code mod.
# This script does not deploy to Reloaded-II, edit AppConfig, touch saves, or launch the game.
$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Script
    )

    Write-Host ""
    Write-Host "== $Name" -ForegroundColor Cyan
    & $Script
}

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

Push-Location $repo
try {
    Write-Host "Generic Chronicle offline checks" -ForegroundColor Green
    Write-Host "Repo: $repo" -ForegroundColor DarkGray

    Invoke-Step "PowerShell syntax" {
        $parseFiles = @(
            Get-ChildItem -LiteralPath (Join-Path $repo 'codemod') -Filter '*.ps1' |
                Select-Object -ExpandProperty FullName
        )
        $rootDeploy = Join-Path $repo 'deploy.ps1'
        if (Test-Path -LiteralPath $rootDeploy) {
            $parseFiles += $rootDeploy
        }

        foreach ($scriptFile in $parseFiles) {
            $tokens = $null
            $errors = $null
            [System.Management.Automation.Language.Parser]::ParseFile($scriptFile, [ref]$tokens, [ref]$errors) | Out-Null
            if ($errors.Count -gt 0) {
                $messages = $errors | ForEach-Object { "$($_.Extent.StartLineNumber):$($_.Extent.StartColumnNumber) $($_.Message)" }
                throw "PowerShell parse failed for $scriptFile`n$($messages -join [Environment]::NewLine)"
            }
        }
    }

    if (-not $SkipPython) {
        Invoke-Step "Python syntax" {
            $pythonFiles = @(Get-ChildItem -LiteralPath (Join-Path $repo 'tools') -Filter '*.py' |
                Select-Object -ExpandProperty FullName)
            if ($pythonFiles.Count -gt 0) {
                Invoke-Native 'python' (@('-m', 'py_compile') + $pythonFiles)
            }
        }

        Invoke-Step "Python tooling smoke tests" {
            Invoke-Native 'python' @('tools\test_runtime_tooling.py')
            Invoke-Native 'python' @('tools\test_actor_probe_ct.py')
            Invoke-Native 'python' @('tools\test_memtable_candidates.py')
            Invoke-Native 'python' @('tools\test_neuter_data.py')
            Invoke-Native 'python' @('tools\test_runtime_formula_context.py')
            Invoke-Native 'python' @('tools\test_runtime_profiles.py')
        }

        Invoke-Step "JSON files" {
            $jsonFiles = @(
                'docs\modding\examples\runtime-simulation-scenarios.example.json',
                'docs\modding\examples\runtime-simulation-matrix.v0.2.example.json',
                'docs\modding\examples\runtime-simulation-matrix-response.v0.2.example.json',
                'docs\modding\examples\runtime-simulation-gurps-dr.example.json',
                'docs\modding\examples\runtime-simulation-static-dr.example.json',
                'docs\modding\examples\runtime-simulation-mp.example.json',
                'docs\modding\examples\runtime-simulation-sentinel-bands.example.json',
                'docs\modding\examples\runtime-simulation-dry-run.example.json',
                'docs\modding\examples\runtime-simulation-neuter-spotcheck.example.json',
                'docs\modding\examples\runtime-simulation-death-gate.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2-response.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.generated.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.matrix.generated.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.scan.generated.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.scan.live-noop.example.json',
                'docs\modding\examples\battle-runtime-settings.gurps-dr.example.json',
                'docs\modding\examples\battle-runtime-settings.static-dr.example.json',
                'docs\modding\examples\battle-runtime-settings.mp.example.json',
                'docs\modding\examples\battle-runtime-settings.sentinel-bands.example.json',
                'docs\modding\examples\battle-runtime-settings.dry-run.example.json',
                'docs\modding\examples\battle-runtime-settings.memtable-probe.disabled.example.json',
                'work\battle-runtime-settings.v0.2.generated.json',
                'work\battle-runtime-settings.v0.2.matrix.generated.json',
                'work\battle-runtime-settings.v0.2.scan.generated.json',
                'work\battle-runtime-settings.v0.2.scan.live-noop.json',
                'work\battle-runtime-settings.neuter-spotcheck.json',
                'work\battle-runtime-settings.death-flag-capture.json',
                'work\battle-runtime-settings.actor-probe.json',
                'work\battle-runtime-settings.engine-death-test.json',
                'work\battle-runtime-settings.death-test.json',
                'work\battle-runtime-settings.death-test-killflag.json',
                'work\memtable-probe-candidates.disabled.json',
                'work\runtime-simulation.v0.2.generated.sample.json'
            )
            foreach ($jsonFile in $jsonFiles) {
                $fullPath = Resolve-RepoPath $jsonFile
                if (Test-Path -LiteralPath $fullPath) {
                    Get-Content -Raw -LiteralPath $fullPath | ConvertFrom-Json | Out-Null
                }
            }
        }
    }

    if (-not $SkipDotNet) {
        Invoke-Step "C# build" {
            Invoke-Native 'dotnet' @(
                'build',
                'codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj',
                '-c',
                'Release'
            )
        }

        Invoke-Step "C# smoke tests" {
            Invoke-Native 'dotnet' @(
                'run',
                '--project',
                'codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj',
                '-c',
                'Release'
            )
        }

        Invoke-Step "Runtime settings validator" {
            Invoke-Native 'dotnet' @(
                'run',
                '--project',
                'codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj',
                '-c',
                'Release'
            )
        }

        Invoke-Step "Runtime settings simulator" {
            Write-Host "short fixture -> $Settings" -ForegroundColor DarkGray
            Invoke-Native 'dotnet' @(
                'run',
                '--project',
                'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                '-c',
                'Release',
                '--',
                (Resolve-RepoPath $Settings),
                (Resolve-RepoPath $Scenarios),
                '--no-trace'
            )

            $matrixPath = Resolve-RepoPath $MatrixScenarios
            if (Test-Path -LiteralPath $matrixPath) {
                Write-Host "matrix fixture -> $Settings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    (Resolve-RepoPath $Settings),
                    $matrixPath,
                    '--no-trace'
                )

                $scanSettingsPath = Resolve-RepoPath $ScanSettings
                if (Test-Path -LiteralPath $scanSettingsPath) {
                    Write-Host "matrix fixture -> $ScanSettings" -ForegroundColor DarkGray
                    Invoke-Native 'dotnet' @(
                        'run',
                        '--project',
                        'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                        '-c',
                        'Release',
                        '--',
                        $scanSettingsPath,
                        $matrixPath,
                        '--no-trace'
                    )
                }
            }

            $matrixResponseScenariosPath = Resolve-RepoPath $MatrixResponseScenarios
            $matrixResponseSettingsPath = Resolve-RepoPath $MatrixResponseSettings
            if ((Test-Path -LiteralPath $matrixResponseScenariosPath) -and
                (Test-Path -LiteralPath $matrixResponseSettingsPath)) {
                Write-Host "matrix-response fixture -> $MatrixResponseSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $matrixResponseSettingsPath,
                    $matrixResponseScenariosPath,
                    '--no-trace'
                )
            }

            $gurpsDrScenariosPath = Resolve-RepoPath $GurpsDrScenarios
            $gurpsDrSettingsPath = Resolve-RepoPath $GurpsDrSettings
            if ((Test-Path -LiteralPath $gurpsDrScenariosPath) -and
                (Test-Path -LiteralPath $gurpsDrSettingsPath)) {
                Write-Host "GURPS-DR fixture -> $GurpsDrSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $gurpsDrSettingsPath,
                    $gurpsDrScenariosPath,
                    '--no-trace'
                )
            }

            $mpScenariosPath = Resolve-RepoPath $MpScenarios
            $mpSettingsPath = Resolve-RepoPath $MpSettings
            $staticDrScenariosPath = Resolve-RepoPath $StaticDrScenarios
            $staticDrSettingsPath = Resolve-RepoPath $StaticDrSettings
            if ((Test-Path -LiteralPath $staticDrScenariosPath) -and
                (Test-Path -LiteralPath $staticDrSettingsPath)) {
                Write-Host "static DR fixture -> $StaticDrSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $staticDrSettingsPath,
                    $staticDrScenariosPath,
                    '--no-trace'
                )
            }

            if ((Test-Path -LiteralPath $mpScenariosPath) -and
                (Test-Path -LiteralPath $mpSettingsPath)) {
                Write-Host "MP fixture -> $MpSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $mpSettingsPath,
                    $mpScenariosPath,
                    '--no-trace'
                )
            }

            $sentinelBandsScenariosPath = Resolve-RepoPath $SentinelBandsScenarios
            $sentinelBandsSettingsPath = Resolve-RepoPath $SentinelBandsSettings
            if ((Test-Path -LiteralPath $sentinelBandsScenariosPath) -and
                (Test-Path -LiteralPath $sentinelBandsSettingsPath)) {
                Write-Host "sentinel bands fixture -> $SentinelBandsSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $sentinelBandsSettingsPath,
                    $sentinelBandsScenariosPath,
                    '--no-trace'
                )
            }

            $neuterSpotcheckScenariosPath = Resolve-RepoPath $NeuterSpotcheckScenarios
            $neuterSpotcheckSettingsPath = Resolve-RepoPath $NeuterSpotcheckSettings
            $dryRunScenariosPath = Resolve-RepoPath $DryRunScenarios
            $dryRunSettingsPath = Resolve-RepoPath $DryRunSettings
            if ((Test-Path -LiteralPath $dryRunScenariosPath) -and
                (Test-Path -LiteralPath $dryRunSettingsPath)) {
                Write-Host "dry-run fixture -> $DryRunSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $dryRunSettingsPath,
                    $dryRunScenariosPath,
                    '--no-trace'
                )
            }

            if ((Test-Path -LiteralPath $neuterSpotcheckScenariosPath) -and
                (Test-Path -LiteralPath $neuterSpotcheckSettingsPath)) {
                Write-Host "neuter spot-check fixture -> $NeuterSpotcheckSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $neuterSpotcheckSettingsPath,
                    $neuterSpotcheckScenariosPath,
                    '--no-trace'
                )
            }

            $deathGateScenariosPath = Resolve-RepoPath $DeathGateScenarios
            $deathHpSettingsPath = Resolve-RepoPath $DeathHpSettings
            if ((Test-Path -LiteralPath $deathGateScenariosPath) -and
                (Test-Path -LiteralPath $deathHpSettingsPath)) {
                Write-Host "death gate HP-only fixture -> $DeathHpSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $deathHpSettingsPath,
                    $deathGateScenariosPath,
                    '--no-trace'
                )
            }

            $deathKillFlagSettingsPath = Resolve-RepoPath $DeathKillFlagSettings
            if ((Test-Path -LiteralPath $deathGateScenariosPath) -and
                (Test-Path -LiteralPath $deathKillFlagSettingsPath)) {
                Write-Host "death gate KO-flag fixture -> $DeathKillFlagSettings" -ForegroundColor DarkGray
                Invoke-Native 'dotnet' @(
                    'run',
                    '--project',
                    'codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj',
                    '-c',
                    'Release',
                    '--',
                    $deathKillFlagSettingsPath,
                    $deathGateScenariosPath,
                    '--no-trace'
                )
            }
        }

        Invoke-Step "Live mapping helper dry-run" {
            Invoke-Native 'powershell' @(
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                'codemod\prepare-live-mapping.ps1',
                '-DryRun'
            )
        }

        Invoke-Step "Dry-run evaluation helper dry-run" {
            Invoke-Native 'powershell' @(
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                'codemod\prepare-dry-run-evaluation.ps1',
                '-DryRun'
            )
        }

        Invoke-Step "Death gate helper dry-runs" {
            Invoke-Native 'powershell' @(
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                'codemod\prepare-death-gate.ps1',
                '-DryRun',
                '-NeuterSpotcheck'
            )
            Invoke-Native 'powershell' @(
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                'codemod\prepare-death-gate.ps1',
                '-DryRun'
            )
            Invoke-Native 'powershell' @(
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                'codemod\prepare-death-gate.ps1',
                '-DryRun',
                '-KillFlag'
            )
        }
    }

    if (-not $SkipGitDiffCheck) {
        Invoke-Step "Git diff whitespace check" {
            Invoke-Native 'git' @('diff', '--check')
        }
    }

    Write-Host ""
    Write-Host "Offline checks passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
