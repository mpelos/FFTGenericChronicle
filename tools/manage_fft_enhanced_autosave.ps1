param(
    [ValidateSet('Status', 'Snapshot', 'Restore')]
    [string]$Action = 'Status',

    [string]$SnapshotPath,

    [string]$SavePath,

    [string]$WorkRoot,

    [string]$FixtureKind,

    [string]$FixtureLabel,

    [string]$RequireFixtureKind
)

$ErrorActionPreference = 'Stop'

function Resolve-AutosavePath {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $documents = [Environment]::GetFolderPath('MyDocuments')
    $gameRoot = Join-Path $documents 'My Games\FINAL FANTASY TACTICS - The Ivalice Chronicles'
    if (-not (Test-Path -LiteralPath $gameRoot)) {
        throw "FFT Enhanced save root was not found: $gameRoot"
    }

    $matches = @(Get-ChildItem -LiteralPath $gameRoot -Recurse -Force -File -Filter 'autoenhanced.png')
    if ($matches.Count -ne 1) {
        $found = ($matches | ForEach-Object FullName) -join [Environment]::NewLine
        throw "Expected exactly one autoenhanced.png below $gameRoot; found $($matches.Count).$([Environment]::NewLine)$found"
    }

    return $matches[0].FullName
}

function Assert-GameClosed {
    $running = @(
        Get-Process -Name 'FFT_enhanced' -ErrorAction SilentlyContinue |
            Where-Object { -not $_.HasExited }
    )
    if ($running.Count -gt 0) {
        throw 'FFT_enhanced.exe is running. Close the game before snapshotting or restoring its autosave container.'
    }
}

function Get-FileSummary {
    param([string]$Path)

    $item = Get-Item -LiteralPath $Path
    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    [pscustomobject]@{
        Path = $item.FullName
        Length = $item.Length
        LastWriteTime = $item.LastWriteTime
        Sha256 = $hash
    }
}

function Get-FixtureMetadataPath {
    param([string]$Path)

    return "$Path.fixture.json"
}

function Write-FixtureMetadata {
    param(
        [string]$Path,
        [string]$Kind,
        [string]$Label
    )

    if (-not $Kind) {
        return
    }

    $summary = Get-FileSummary -Path $Path
    $metadata = [ordered]@{
        FixtureKind = $Kind
        FixtureLabel = $Label
        SnapshotPath = $summary.Path
        Length = $summary.Length
        Sha256 = $summary.Sha256
        CreatedUnixTime = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    }
    $metadataPath = Get-FixtureMetadataPath -Path $Path
    $metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $metadataPath -Encoding UTF8
}

function Assert-FixtureMetadata {
    param(
        [string]$Path,
        [string]$RequiredKind
    )

    if (-not $RequiredKind) {
        return
    }

    $metadataPath = Get-FixtureMetadataPath -Path $Path
    if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
        throw "Autosave snapshot metadata is required for fixture kind '$RequiredKind': $metadataPath"
    }

    $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
    if ([string]$metadata.FixtureKind -ne $RequiredKind) {
        throw "Autosave snapshot fixture kind '$($metadata.FixtureKind)' does not match required kind '$RequiredKind'."
    }

    $currentHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ([string]$metadata.Sha256 -ne $currentHash) {
        throw "Autosave snapshot hash does not match its fixture metadata: $Path"
    }
}

$resolvedSavePath = Resolve-AutosavePath -ExplicitPath $SavePath
$resolvedWorkRoot = if ($WorkRoot) {
    [IO.Path]::GetFullPath($WorkRoot)
} else {
    [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\work'))
}

if (-not (Test-Path -LiteralPath $resolvedWorkRoot)) {
    throw "Work directory was not found: $resolvedWorkRoot"
}

if ($Action -eq 'Status') {
    Get-FileSummary -Path $resolvedSavePath | Format-List
    exit 0
}

Assert-GameClosed
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

if ($Action -eq 'Snapshot') {
    $destination = if ($SnapshotPath) {
        [IO.Path]::GetFullPath($SnapshotPath)
    } else {
        Join-Path $resolvedWorkRoot "$timestamp-fft-autoenhanced-snapshot.png"
    }

    if (Test-Path -LiteralPath $destination) {
        throw "Snapshot destination already exists: $destination"
    }

    Copy-Item -LiteralPath $resolvedSavePath -Destination $destination
    Write-FixtureMetadata -Path $destination -Kind $FixtureKind -Label $FixtureLabel
    Get-FileSummary -Path $destination | Format-List
    if ($FixtureKind) {
        Get-Content -LiteralPath (Get-FixtureMetadataPath -Path $destination)
    }
    exit 0
}

if (-not $SnapshotPath) {
    throw '-SnapshotPath is required for Restore.'
}

$resolvedSnapshotPath = (Resolve-Path -LiteralPath $SnapshotPath).Path
if ([IO.Path]::GetFullPath($resolvedSnapshotPath) -eq [IO.Path]::GetFullPath($resolvedSavePath)) {
    throw 'Restore source and autosave destination resolve to the same file.'
}

$sourceInfo = Get-Item -LiteralPath $resolvedSnapshotPath
if ($sourceInfo.Length -le 0) {
    throw "Restore source is empty: $resolvedSnapshotPath"
}
Assert-FixtureMetadata -Path $resolvedSnapshotPath -RequiredKind $RequireFixtureKind

$backupPath = Join-Path $resolvedWorkRoot "$timestamp-fft-autoenhanced-before-restore.png"
Copy-Item -LiteralPath $resolvedSavePath -Destination $backupPath
Copy-Item -LiteralPath $resolvedSnapshotPath -Destination $resolvedSavePath -Force

$sourceHash = (Get-FileHash -LiteralPath $resolvedSnapshotPath -Algorithm SHA256).Hash
$restoredHash = (Get-FileHash -LiteralPath $resolvedSavePath -Algorithm SHA256).Hash
if ($sourceHash -ne $restoredHash) {
    throw "Restore verification failed. Backup preserved at $backupPath"
}

[pscustomobject]@{
    RestoredFrom = $resolvedSnapshotPath
    RestoredTo = $resolvedSavePath
    Backup = $backupPath
    Sha256 = $restoredHash
} | Format-List
