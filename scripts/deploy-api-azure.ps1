param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [string]$PackagePath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $PackagePath = Join-Path $projectRoot "artifacts\azure-api-linux-x64\BioVic.Api-linux-x64.zip"
}
$resolvedPackage = [System.IO.Path]::GetFullPath($PackagePath)

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "No se encontro Azure CLI. Instale Microsoft.AzureCLI con winget o use Azure Cloud Shell."
}
if (-not (Test-Path $resolvedPackage)) {
    throw "No se encontro el paquete: $resolvedPackage. Ejecute primero scripts\publish-api-azure.ps1."
}

& az account show --output none
if ($LASTEXITCODE -ne 0) {
    throw "Inicie sesion con az login y vuelva a intentarlo."
}

& az webapp deploy `
    --resource-group $ResourceGroup `
    --name $AppName `
    --src-path $resolvedPackage `
    --type zip `
    --restart true `
    --clean true
if ($LASTEXITCODE -ne 0) {
    throw "Azure no pudo publicar BioVic.Api."
}

$publicUrl = "https://$AppName.azurewebsites.net"
$health = Invoke-RestMethod "$publicUrl/health" -TimeoutSec 90
if ($health.status -ne "healthy") {
    throw "Azure respondio, pero BioVic.Api no informo un estado saludable."
}

Write-Host "BioVic.Api fue publicada y esta saludable:" -ForegroundColor Green
Write-Host $publicUrl
