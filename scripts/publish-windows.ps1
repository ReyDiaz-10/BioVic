$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $projectRoot "artifacts\windows-x64"

if (-not (Test-Path (Join-Path $projectRoot ".env.example"))) {
    throw "No se encontró .env.example. La publicación se canceló para no generar un paquete incompleto."
}

dotnet publish (Join-Path $projectRoot "src\BioVic.Api\BioVic.Api.csproj") -c Release -r win-x64 --self-contained true -o (Join-Path $output "api")
dotnet publish (Join-Path $projectRoot "src\BioVic.Desktop\BioVic.Desktop.csproj") -c Release -r win-x64 --self-contained true -o (Join-Path $output "desktop")
Copy-Item (Join-Path $projectRoot ".env.example") (Join-Path $output "api\.env.example") -Force
Copy-Item (Join-Path $projectRoot "src\BioVic.Desktop\biovic.client.example.json") (Join-Path $output "desktop\biovic.client.example.json") -Force
Copy-Item (Join-Path $PSScriptRoot "Start-BioVic.cmd") (Join-Path $output "Start-BioVic.cmd") -Force
Write-Host "BioVic publicado en $output"
