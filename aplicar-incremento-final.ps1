[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$payloadRoot = Join-Path $packageRoot "BioVic"
$resolvedProject = [IO.Path]::GetFullPath($ProjectRoot)
if (-not (Test-Path (Join-Path $resolvedProject "BioVic.sln")) -or
    -not (Test-Path (Join-Path $resolvedProject "src\BioVic.Api\Program.cs"))) {
    throw "ProjectRoot no corresponde a la raíz del código fuente de BioVic."
}
if (-not (Test-Path $payloadRoot)) { throw "El paquete está incompleto: falta la carpeta BioVic." }
if (-not (Test-Path (Join-Path $resolvedProject "src\BioVic.Api\Endpoints\MobilePortalEndpoints.cs"))) {
    throw "No se detectó el incremento web/móvil seguro ya integrado. Aplique primero esa etapa."
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $resolvedProject "artifacts\backups\incremento-final-completo-$stamp"
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
foreach ($migrationFolder in @("src\BioVic.Infrastructure\Migrations", "src\BioVic.Infrastructure.SqlServerMigrations\Migrations")) {
    $sourceFolder = Join-Path $resolvedProject $migrationFolder
    if (Test-Path $sourceFolder) {
        $backupFolder = Join-Path $backupRoot $migrationFolder
        New-Item -ItemType Directory -Path (Split-Path -Parent $backupFolder) -Force | Out-Null
        Copy-Item $sourceFolder $backupFolder -Recurse -Force
    }
}

$payloadFiles = Get-ChildItem $payloadRoot -Recurse -File
foreach ($source in $payloadFiles) {
    $relative = [IO.Path]::GetRelativePath($payloadRoot, $source.FullName)
    $target = Join-Path $resolvedProject $relative
    if (Test-Path $target -PathType Leaf) {
        $backup = Join-Path $backupRoot $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Copy-Item $target $backup -Force
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Unblock-File $source.FullName -ErrorAction SilentlyContinue
    Copy-Item $source.FullName $target -Force
}

foreach ($relative in @("src\BioVic.Desktop\Views\EmailWindow.xaml", "src\BioVic.Desktop\Views\EmailWindow.xaml.cs")) {
    $target = Join-Path $resolvedProject $relative
    if (-not (Test-Path $target)) { continue }
    $backup = Join-Path $backupRoot $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
    Copy-Item $target $backup -Force
    Remove-Item $target -Force
}

Write-Host "Copia de seguridad creada: $backupRoot" -ForegroundColor Cyan
Write-Host "Creando migraciones aditivas; no se elimina ni se reinicia la base de datos..." -ForegroundColor Cyan
& (Join-Path $resolvedProject "scripts\create-comprehensive-migrations.ps1") -ProjectRoot $resolvedProject
if ($LASTEXITCODE -ne 0) { throw "No terminaron las migraciones. La copia de seguridad está en $backupRoot" }

& (Join-Path $resolvedProject "scripts\verify-comprehensive-source.ps1") -ProjectRoot $resolvedProject
if ($LASTEXITCODE -ne 0) { throw "La validación no terminó correctamente. La copia de seguridad está en $backupRoot" }

Write-Host "INCREMENTO FINAL DE BIOVIC APLICADO CORRECTAMENTE" -ForegroundColor Green
Write-Host "Documentos versionados, recuperación, portal QR documental y actualización firmada están en la fuente." -ForegroundColor Green
Write-Host "Siguiente paso: publicar primero el API de Azure. No publique aún el instalador hasta verificar /health." -ForegroundColor Yellow
