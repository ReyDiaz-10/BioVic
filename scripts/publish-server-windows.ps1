param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $projectRoot "src\BioVic.Api\BioVic.Api.csproj"
$output = Join-Path $projectRoot "artifacts\server-$RuntimeIdentifier"
$template = Join-Path $projectRoot ".env.production.example"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontró el SDK estable de .NET 10."
}
if (-not (Test-Path $template)) {
    throw "No se encontró .env.production.example."
}

if (Test-Path $output) {
    $expectedParent = [System.IO.Path]::GetFullPath((Join-Path $projectRoot "artifacts"))
    $resolvedOutput = [System.IO.Path]::GetFullPath($output)
    if (-not $resolvedOutput.StartsWith($expectedParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "La ruta de publicación no está dentro de artifacts."
    }
    Remove-Item $output -Recurse -Force
}

dotnet publish $apiProject `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained false `
    --output $output `
    --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "La publicación del servidor falló con código ${LASTEXITCODE}."
}

Copy-Item $template (Join-Path $output ".env.production.example") -Force
Copy-Item (Join-Path $projectRoot "docs\PRODUCTION_STAGE1.md") (Join-Path $output "PRODUCTION_STAGE1.md") -Force

Write-Host "Paquete de servidor creado en: $output" -ForegroundColor Green
Write-Host "No contiene .env, base de datos, contraseñas, registros ni copias de seguridad."
