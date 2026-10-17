[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath($ProjectRoot)
$required = @(
    "src\BioVic.Api\Endpoints\MobilePortalEndpoints.cs",
    "src\BioVic.Api\Endpoints\EquipmentDocumentEndpoints.cs",
    "src\BioVic.Api\Endpoints\RecoveryEndpoints.cs",
    "src\BioVic.Api\Endpoints\UpdateEndpoints.cs",
    "src\BioVic.Infrastructure\Services\EquipmentDocumentService.cs",
    "src\BioVic.Infrastructure\Services\RecoveryService.cs",
    "src\BioVic.Desktop\Services\SignedUpdateService.cs",
    "scripts\test-mobile-e2e.ps1"
)
foreach ($relative in $required) {
    if (-not (Test-Path (Join-Path $root $relative))) { throw "Falta $relative" }
}
$mobile = Get-Content (Join-Path $root "src\BioVic.Api\Endpoints\MobilePortalEndpoints.cs") -Raw
if ($mobile -match 'MapDelete') { throw "Se detectó una ruta DELETE en el portal móvil." }
$renderer = Get-Content (Join-Path $root "src\BioVic.QrPortal\MobilePortalRenderer.cs") -Raw
foreach ($text in @("Documentos y certificados", "Cerrar sesión", "Desde el celular no existe ninguna opción para borrar datos")) {
    if (-not $renderer.Contains($text)) { throw "Falta el control móvil: $text" }
}
$sqlMigration = Get-ChildItem (Join-Path $root "src\BioVic.Infrastructure.SqlServerMigrations\Migrations") -File |
    Where-Object { $_.Name -like "*SecureDocumentsRecovery20260829*.cs" } | Select-Object -First 1
if ($null -eq $sqlMigration) { throw "Falta la migración SQL Server del incremento." }

& dotnet restore (Join-Path $root "BioVic.sln") --force
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet restore." }
& dotnet build (Join-Path $root "BioVic.sln") -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet build." }
& dotnet test (Join-Path $root "BioVic.sln") -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet test." }
Write-Host "FUENTE COMPLETA: COMPILACIÓN, PRUEBAS, MÓVIL Y MIGRACIÓN CORRECTOS" -ForegroundColor Green
