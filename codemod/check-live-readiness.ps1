param(
    [string]$AppConfig = 'C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json',
    [string]$ModId = 'fftivc.generic.chronicle.codemod',
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles',
    [string]$ProcessName = 'FFT_enhanced'
)

# Read-only diagnostics for the Generic Chronicle live mapping loop.
$ErrorActionPreference = 'Stop'

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

Write-Host "Generic Chronicle live readiness" -ForegroundColor Cyan

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

$modDir = Join-Path 'C:\Reloaded-II\Mods' $ModId
$modDll = Join-Path $modDir "$ModId.dll"
Write-Host ""
Write-Host "Installed code mod" -ForegroundColor Green
if (Test-Path -LiteralPath $modDll) {
    Get-Item -LiteralPath $modDll | Select-Object FullName, Length, LastWriteTime | Format-List
}
else {
    Write-Host "Missing: $modDll" -ForegroundColor Yellow
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
