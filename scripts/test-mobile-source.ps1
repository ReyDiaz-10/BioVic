[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$mobileEndpoints = Join-Path $root "src\BioVic.Api\Endpoints\MobilePortalEndpoints.cs"
$mobileRenderer = Join-Path $root "src\BioVic.QrPortal\MobilePortalRenderer.cs"
$rules = Join-Path $root "src\BioVic.Application\Services\MobileAccessRules.cs"

foreach ($required in @($mobileEndpoints, $mobileRenderer, $rules)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Falta el archivo requerido: $required"
    }
}

$endpointText = Get-Content -LiteralPath $mobileEndpoints -Raw
$rendererText = Get-Content -LiteralPath $mobileRenderer -Raw
$rulesText = Get-Content -LiteralPath $rules -Raw

if ($endpointText -match 'MapDelete') {
    throw "Se detectó una ruta de eliminación en el portal móvil."
}

$requiredRoutes = @(
    '/e/login',
    '/e/mobile',
    '/maintenance',
    '/calibration',
    '/failure',
    '/incident',
    '/edit',
    '/users/new',
    '/locations/new',
    '/logout'
)

foreach ($route in $requiredRoutes) {
    if (-not $endpointText.Contains($route)) {
        throw "No se encontró la ruta móvil requerida: $route"
    }
}

foreach ($marker in @('RequireAuthorization', 'ValidateRequestAsync', 'AuthenticatedWrite')) {
    if (-not $endpointText.Contains($marker)) {
        throw "Falta el control de seguridad: $marker"
    }
}

if (-not $rulesText.Contains('CanDeleteFromMobile') -or -not $rulesText.Contains('false')) {
    throw "No se confirmó la prohibición de eliminación móvil."
}

if (-not $rendererText.Contains('Cerrar sesión') -or -not $rendererText.Contains('Descargar PDF')) {
    throw "Faltan acciones esenciales en la interfaz móvil."
}

Write-Host "Controles estáticos del portal móvil: CORRECTOS" -ForegroundColor Green

dotnet restore (Join-Path $root 'BioVic.sln') --force
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet restore." }

dotnet build (Join-Path $root 'BioVic.sln') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet build." }

dotnet test (Join-Path $root 'BioVic.sln') -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet test." }

Write-Host "PORTAL MÓVIL: FUENTE, COMPILACIÓN Y PRUEBAS CORRECTAS" -ForegroundColor Green

