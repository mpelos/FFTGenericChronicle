param(
    [string]$AppConfig = 'C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json',
    [string[]]$EnabledMods = @(
        'fftivc.utility.modloader',
        'ffttic.jobs.genericjobs',
        'fftivc.battles.ngplus'
    ),
    [switch]$IncludeCodeMod
)

# Restore the known-good FFT Reloaded-II enabled mod list without touching it while
# Reloaded-II or the game is running.
$ErrorActionPreference = 'Stop'

$running = Get-Process -Name 'Reloaded-II', 'FFT_enhanced' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Refusing to edit AppConfig while Reloaded-II or FFT_enhanced is running:" -ForegroundColor Yellow
    $running | Select-Object ProcessName, Id, StartTime | Format-Table -AutoSize
    Write-Host "Close Reloaded-II and FFT, then rerun this script." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path -LiteralPath $AppConfig)) {
    Write-Host "AppConfig not found: $AppConfig" -ForegroundColor Red
    exit 1
}

$app = Get-Content -Raw -LiteralPath $AppConfig | ConvertFrom-Json
$mods = @($EnabledMods)
if ($IncludeCodeMod -and ($mods -notcontains 'fftivc.generic.chronicle.codemod')) {
    $mods += 'fftivc.generic.chronicle.codemod'
}

$sorted = @($app.SortedMods)
foreach ($mod in $mods) {
    if ($sorted -notcontains $mod) {
        $sorted += $mod
    }
}

$app.EnabledMods = $mods
$app.SortedMods = $sorted

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = "$AppConfig.bak-$stamp"
$temp = "$AppConfig.tmp-$stamp"

try {
    $json = $app | ConvertTo-Json -Depth 20
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($temp, $json + [Environment]::NewLine, $utf8NoBom)
    Get-Content -Raw -LiteralPath $temp | ConvertFrom-Json | Out-Null
    [System.IO.File]::Replace($temp, $AppConfig, $backup, $false)
}
catch {
    if (Test-Path -LiteralPath $temp) {
        Remove-Item -LiteralPath $temp -Force
    }
    Write-Host "Failed to restore AppConfig: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "Restored enabled mods -> $AppConfig" -ForegroundColor Cyan
Write-Host "Backup -> $backup" -ForegroundColor DarkYellow
Write-Host "EnabledMods: $($mods -join ', ')" -ForegroundColor Green
