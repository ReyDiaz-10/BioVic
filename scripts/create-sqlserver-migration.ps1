param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$projectRoot = Split-Path -Parent $PSScriptRoot
$migrationProject = Join-Path $projectRoot "src\BioVic.Infrastructure.SqlServerMigrations\BioVic.Infrastructure.SqlServerMigrations.csproj"
$startupProject = Join-Path $projectRoot "src\BioVic.Api\BioVic.Api.csproj"
$migrationDirectory = Join-Path $projectRoot "src\BioVic.Infrastructure.SqlServerMigrations\Migrations"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontro el SDK estable de .NET 10."
}

$existingMigrations = @(
    Get-ChildItem $migrationDirectory -Filter "*.cs" -File -ErrorAction SilentlyContinue
)
if ($existingMigrations.Count -gt 0) {
    throw "Ya existen migraciones SQL Server. No se genero otra para evitar duplicados."
}

Push-Location $projectRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo restaurar dotnet-ef 10.0.11."
    }

    & dotnet tool run dotnet-ef migrations add InitialSqlServer `
        --project $migrationProject `
        --startup-project $startupProject `
        --context BioVicDbContext `
        --output-dir Migrations
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo crear la migracion inicial de SQL Server."
    }

    & dotnet tool run dotnet-ef migrations has-pending-model-changes `
        --project $migrationProject `
        --startup-project $startupProject `
        --context BioVicDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "El modelo SQL Server aun tiene cambios pendientes."
    }

    & dotnet build BioVic.sln -c Debug --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "La solucion no compilo despues de crear la migracion."
    }

    & dotnet test BioVic.sln -c Debug --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Una o mas pruebas fallaron despues de crear la migracion."
    }

    Write-Host "MIGRACION SQL SERVER CREADA Y VALIDADA" -ForegroundColor Green
    Get-ChildItem $migrationDirectory -Filter "*.cs" -File |
        Select-Object -ExpandProperty FullName
}
finally {
    Pop-Location
}
