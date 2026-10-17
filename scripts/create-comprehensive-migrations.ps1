[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
$root = [IO.Path]::GetFullPath($ProjectRoot)
$apiProject = Join-Path $root "src\BioVic.Api\BioVic.Api.csproj"
$sqliteProject = Join-Path $root "src\BioVic.Infrastructure\BioVic.Infrastructure.csproj"
$sqlServerProject = Join-Path $root "src\BioVic.Infrastructure.SqlServerMigrations\BioVic.Infrastructure.SqlServerMigrations.csproj"
if (-not (Test-Path $apiProject) -or -not (Test-Path $sqliteProject)) { throw "No se encontró la solución BioVic." }
if (-not (Test-Path $sqlServerProject)) {
    throw "Falta BioVic.Infrastructure.SqlServerMigrations. Aplique primero la arquitectura SQL Server ya usada por Azure."
}

& dotnet restore (Join-Path $root "BioVic.sln") --force
if ($LASTEXITCODE -ne 0) { throw "No se pudo restaurar la solución." }
& dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "No se pudo restaurar dotnet-ef." }

$migrationName = "SecureDocumentsRecovery20260829"
$oldEnvironment = $env:BIOVIC_ENVIRONMENT
$oldProvider = $env:BIOVIC_DB_PROVIDER
$oldConnection = $env:BIOVIC_DB_CONNECTION
$oldHosting = $env:BIOVIC_HOSTING_PROVIDER
try {
    $env:BIOVIC_ENVIRONMENT = "Development"
    $env:BIOVIC_HOSTING_PROVIDER = "standalone"

    $sqliteMarker = Get-ChildItem (Join-Path $root "src\BioVic.Infrastructure\Migrations") -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*$migrationName*.cs" } | Select-Object -First 1
    if ($null -eq $sqliteMarker) {
        $env:BIOVIC_DB_PROVIDER = "sqlite"
        $env:BIOVIC_DB_CONNECTION = "Data Source=data/biovic-migration-design.db"
        & dotnet ef migrations add $migrationName --project $sqliteProject --startup-project $apiProject `
            --context BioVicDbContext --output-dir Migrations
        if ($LASTEXITCODE -ne 0) { throw "No se pudo crear la migración SQLite de documentos." }
    }
    else { Write-Host "Migración SQLite ya presente: $($sqliteMarker.Name)" -ForegroundColor Cyan }

    $sqlMarker = Get-ChildItem (Join-Path $root "src\BioVic.Infrastructure.SqlServerMigrations\Migrations") -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*$migrationName*.cs" } | Select-Object -First 1
    if ($null -eq $sqlMarker) {
        $env:BIOVIC_DB_PROVIDER = "sqlserver"
        $env:BIOVIC_DB_CONNECTION = "Server=(localdb)\MSSQLLocalDB;Database=BioVicMigrationDesign;Trusted_Connection=True;TrustServerCertificate=True"
        & dotnet ef migrations add $migrationName --project $sqlServerProject --startup-project $apiProject `
            --context BioVicDbContext --output-dir Migrations
        if ($LASTEXITCODE -ne 0) { throw "No se pudo crear la migración SQL Server de documentos." }
    }
    else { Write-Host "Migración SQL Server ya presente: $($sqlMarker.Name)" -ForegroundColor Cyan }
}
finally {
    $env:BIOVIC_ENVIRONMENT = $oldEnvironment
    $env:BIOVIC_DB_PROVIDER = $oldProvider
    $env:BIOVIC_DB_CONNECTION = $oldConnection
    $env:BIOVIC_HOSTING_PROVIDER = $oldHosting
}

Write-Host "MIGRACIONES DE DOCUMENTOS Y RECUPERACIÓN CREADAS" -ForegroundColor Green
