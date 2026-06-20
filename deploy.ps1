# Deploy do mod "Generic Chronicle" para o Reloaded-II.
# Copia D:\Projects\FFTGenericChronicle\mod\fftivc.generic.chronicle  ->  C:\Reloaded-II\Mods\fftivc.generic.chronicle
# Uso: clique-direito > "Run with PowerShell", ou rode .\deploy.ps1 (ou deploy.bat).

$ErrorActionPreference = 'Stop'

# --- caminhos ---
$modId = 'fftivc.generic.chronicle'
$src   = Join-Path $PSScriptRoot "mod\$modId"
$dst   = "C:\Reloaded-II\Mods\$modId"

Write-Host ""
Write-Host "==== Deploy Generic Chronicle ====" -ForegroundColor Cyan
Write-Host "  origem : $src"
Write-Host "  destino: $dst"
Write-Host ""

if (-not (Test-Path $src)) { Write-Host "ERRO: pasta de origem nao encontrada: $src" -ForegroundColor Red; exit 1 }

# versao/nome do mod (do ModConfig)
$cfgPath = Join-Path $src 'ModConfig.json'
if (Test-Path $cfgPath) {
    $cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
    Write-Host ("  mod    : {0}  v{1}" -f $cfg.ModName, $cfg.ModVersion) -ForegroundColor Gray
}

New-Item -ItemType Directory -Force -Path $dst | Out-Null

# --- copia todos os arquivos preservando a estrutura (sem aninhar pasta) ---
$files = Get-ChildItem -Recurse -File -LiteralPath $src | Where-Object { $_.Name -ne '.gitkeep' }
$copied = 0
foreach ($f in $files) {
    $rel    = $f.FullName.Substring($src.Length).TrimStart('\')
    $target = Join-Path $dst $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    Copy-Item -LiteralPath $f.FullName -Destination $target -Force
    $copied++
}
Write-Host ("  copiados: {0} arquivo(s)" -f $copied) -ForegroundColor Gray
Write-Host ""

# --- verificacao por hash (origem vs destino) ---
$ok = $true
Write-Host "Verificacao (hash origem == destino):"
foreach ($f in $files) {
    $rel    = $f.FullName.Substring($src.Length).TrimStart('\')
    $target = Join-Path $dst $rel
    $h1 = (Get-FileHash -LiteralPath $f.FullName).Hash
    $h2 = (Get-FileHash -LiteralPath $target).Hash
    if ($h1 -eq $h2) {
        Write-Host ("  [OK]   {0}" -f $rel) -ForegroundColor Green
    } else {
        Write-Host ("  [FALHA] {0}" -f $rel) -ForegroundColor Red
        $ok = $false
    }
}

Write-Host ""
if ($ok) {
    Write-Host "DEPLOY OK." -ForegroundColor Green
    Write-Host "Agora abra/relance o jogo PELO Reloaded-II (ele regenera o modded.pac no launch)." -ForegroundColor Yellow
} else {
    Write-Host "DEPLOY COM FALHA - veja os itens [FALHA] acima." -ForegroundColor Red
    exit 1
}

# pausa so quando rodado por duplo-clique (sem console interativo herdado)
if ($Host.Name -eq 'ConsoleHost' -and -not [Environment]::GetCommandLineArgs().Contains('-NonInteractive')) {
    Write-Host ""
    Read-Host "Pressione Enter para fechar"
}
