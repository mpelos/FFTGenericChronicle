param(
    [string]$Settings = 'work\battle-runtime-settings.v0.2.generated.json',
    [string]$MatrixResponseSettings = 'work\battle-runtime-settings.v0.2.matrix.generated.json',
    [string]$ScanSettings = 'work\battle-runtime-settings.v0.2.scan.generated.json',
    [string]$GurpsDrSettings = 'docs\modding\examples\battle-runtime-settings.gurps-dr.example.json',
    [string]$Scenarios = 'docs\modding\examples\runtime-simulation-scenarios.example.json',
    [string]$MatrixScenarios = 'docs\modding\examples\runtime-simulation-matrix.v0.2.example.json',
    [string]$MatrixResponseScenarios = 'docs\modding\examples\runtime-simulation-matrix-response.v0.2.example.json',
    [string]$GurpsDrScenarios = 'docs\modding\examples\runtime-simulation-gurps-dr.example.json',
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
            Invoke-Native 'python' @('tools\test_memtable_candidates.py')
        }

        Invoke-Step "JSON files" {
            $jsonFiles = @(
                'docs\modding\examples\runtime-simulation-scenarios.example.json',
                'docs\modding\examples\runtime-simulation-matrix.v0.2.example.json',
                'docs\modding\examples\runtime-simulation-matrix-response.v0.2.example.json',
                'docs\modding\examples\runtime-simulation-gurps-dr.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2-response.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.generated.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.matrix.generated.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.scan.generated.example.json',
                'docs\modding\examples\battle-runtime-settings.v0.2.scan.live-noop.example.json',
                'docs\modding\examples\battle-runtime-settings.gurps-dr.example.json',
                'docs\modding\examples\battle-runtime-settings.memtable-probe.disabled.example.json',
                'work\battle-runtime-settings.v0.2.generated.json',
                'work\battle-runtime-settings.v0.2.matrix.generated.json',
                'work\battle-runtime-settings.v0.2.scan.generated.json',
                'work\battle-runtime-settings.v0.2.scan.live-noop.json',
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
