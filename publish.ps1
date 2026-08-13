# =============================================================================
#  IpScope Pro - Publica la aplicacion
#
#  Modos:
#    .\publish.ps1                     -> autocontenida (~78 MB, no requiere .NET)
#    .\publish.ps1 -SelfContained:$false -> pequena (~39 MB, requiere .NET 9 Desktop Runtime)
#
#  Requisito: .NET SDK instalado.
#  Resultado en .\publish\
# =============================================================================
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"
$root    = $PSScriptRoot
$staging = Join-Path $root "publish"

Write-Host "==> Publicando (self-contained=$SelfContained)..." -ForegroundColor Cyan

$args = @(
    "publish", (Join-Path $root "IpScopePro.csproj"),
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", ($SelfContained.ToString().ToLower()),
    "-p:PublishSingleFile=true",
    "-p:DebugType=none",
    "-o", $staging
)

if ($SelfContained) {
    $args += "-p:EnableCompressionInSingleFile=true"
}

dotnet @args
if ($LASTEXITCODE -ne 0) { throw "dotnet publish ha fallado." }

Write-Host ""
Write-Host "Listo: $(Join-Path $staging 'IpScopePro.exe')" -ForegroundColor Green
if (-not $SelfContained) {
    Write-Host "AVISO: requiere .NET 9 Desktop Runtime en el equipo destino." -ForegroundColor Yellow
}
