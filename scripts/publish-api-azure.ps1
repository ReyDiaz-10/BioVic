param(
    [ValidateSet("linux-x64")]
    [string]$RuntimeIdentifier = "linux-x64"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$projectRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $projectRoot "src\BioVic.Api\BioVic.Api.csproj"
$artifactsRoot = Join-Path $projectRoot "artifacts"
$packageRoot = Join-Path $artifactsRoot "azure-api-$RuntimeIdentifier"
$publishDirectory = Join-Path $packageRoot "app"
$zipPath = Join-Path $packageRoot "BioVic.Api-$RuntimeIdentifier.zip"
$hashPath = "$zipPath.sha256.txt"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontro el SDK estable de .NET 10."
}

if (Test-Path $packageRoot) {
    $safeParent = [System.IO.Path]::GetFullPath($artifactsRoot)
    $resolvedPackageRoot = [System.IO.Path]::GetFullPath($packageRoot)
    if (-not $resolvedPackageRoot.StartsWith($safeParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "La ruta de salida no esta dentro de artifacts."
    }
    Remove-Item $packageRoot -Recurse -Force
}

New-Item $publishDirectory -ItemType Directory -Force | Out-Null

& dotnet restore $apiProject --runtime $RuntimeIdentifier
if ($LASTEXITCODE -ne 0) {
    throw "La restauracion de BioVic.Api fallo con codigo ${LASTEXITCODE}."
}

& dotnet publish $apiProject `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained false `
    --output $publishDirectory `
    --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "La publicacion de BioVic.Api fallo con codigo ${LASTEXITCODE}."
}

if (-not (Test-Path (Join-Path $publishDirectory "BioVic.Api.dll"))) {
    throw "El paquete no contiene BioVic.Api.dll."
}

$forbiddenFiles = Get-ChildItem $publishDirectory -Recurse -File | Where-Object {
    $_.Name -eq ".env" -or
    $_.Name -like "*.db" -or
    $_.Name -like "*.db-shm" -or
    $_.Name -like "*.db-wal" -or
    $_.Name -like "*.log"
}
if ($forbiddenFiles) {
    throw "La publicacion contiene archivos privados o datos locales y fue cancelada."
}

Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash
"$hash  $([System.IO.Path]::GetFileName($zipPath))" | Set-Content $hashPath -Encoding Ascii

Write-Host "Paquete Azure creado:" -ForegroundColor Green
Write-Host $zipPath
Write-Host "SHA-256: $hash"
Write-Host "El ZIP no contiene .env, contrasenas, base de datos, logs ni copias de seguridad."
