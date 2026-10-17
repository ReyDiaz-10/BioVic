$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $projectRoot "BioVic.sln"

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "El comando dotnet falló con código ${LASTEXITCODE}: dotnet $($Arguments -join ' ')"
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontró el SDK estable de .NET 10."
}

Invoke-DotNet -Arguments @("clean", $solution, "--configuration", "Debug")
Invoke-DotNet -Arguments @("build", $solution, "--configuration", "Debug", "--no-incremental")
Invoke-DotNet -Arguments @("test", $solution, "--configuration", "Debug", "--no-build")

Write-Host "Etapa 1 verificada: compilación y pruebas completadas." -ForegroundColor Green
