[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$payloadRoot = Join-Path $packageRoot "BioVic"
$resolvedProject = [System.IO.Path]::GetFullPath($ProjectRoot)

if (-not (Test-Path -LiteralPath (Join-Path $resolvedProject "BioVic.sln") -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $resolvedProject "src\BioVic.Api\Program.cs") -PathType Leaf)) {
    throw "ProjectRoot no corresponde a la carpeta raíz del código fuente de BioVic."
}

if (-not (Test-Path -LiteralPath $payloadRoot -PathType Container)) {
    throw "El paquete está incompleto: no se encontró la carpeta BioVic."
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $resolvedProject "artifacts\backups\incremento-web-movil-seguro-$stamp"
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

$payloadFiles = Get-ChildItem -LiteralPath $payloadRoot -Recurse -File
foreach ($source in $payloadFiles) {
    $relative = [System.IO.Path]::GetRelativePath($payloadRoot, $source.FullName)
    $target = Join-Path $resolvedProject $relative
    $targetParent = Split-Path -Parent $target

    if (Test-Path -LiteralPath $target -PathType Leaf) {
        $backup = Join-Path $backupRoot $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Copy-Item -LiteralPath $target -Destination $backup -Force
    }

    New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
    Unblock-File -LiteralPath $source.FullName -ErrorAction SilentlyContinue
    Copy-Item -LiteralPath $source.FullName -Destination $target -Force
}

$removedEmailFiles = @(
    "src\BioVic.Desktop\Views\EmailWindow.xaml",
    "src\BioVic.Desktop\Views\EmailWindow.xaml.cs"
)

foreach ($relative in $removedEmailFiles) {
    $target = Join-Path $resolvedProject $relative
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { continue }

    $backup = Join-Path $backupRoot $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
    Copy-Item -LiteralPath $target -Destination $backup -Force
    Remove-Item -LiteralPath $target -Force
}

$preservedMigrations = Join-Path $resolvedProject "src\BioVic.Infrastructure.SqlServerMigrations"
if (Test-Path -LiteralPath $preservedMigrations -PathType Container) {
    Write-Host "Migraciones SQL Server detectadas y preservadas: $preservedMigrations" -ForegroundColor Cyan
}

$activeEndpoints = Get-Content -LiteralPath (Join-Path $resolvedProject "src\BioVic.Api\Endpoints\ApiEndpoints.cs") -Raw
$desktopDetails = Get-Content -LiteralPath (Join-Path $resolvedProject "src\BioVic.Desktop\Views\EquipmentDetailsWindow.xaml") -Raw
if ($activeEndpoints.Contains('/email') -or $desktopDetails.Contains('Enviar por correo')) {
    throw "La comprobación posterior detectó que la interfaz de correo continúa activa. Se conservó la copia en $backupRoot"
}

Write-Host "Copia de seguridad: $backupRoot" -ForegroundColor Cyan
Write-Host "Archivos aplicados: $($payloadFiles.Count)" -ForegroundColor Cyan
Write-Host "La función de correo fue retirada; las variables SMTP antiguas, si existen en Azure, quedan ignoradas y pueden eliminarse después." -ForegroundColor Cyan

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $resolvedProject "scripts\test-mobile-source.ps1") -ProjectRoot $resolvedProject
if ($LASTEXITCODE -ne 0) {
    throw "El incremento se copió, pero la validación no terminó correctamente. Revise la salida anterior."
}

Write-Host "INCREMENTO WEB/MÓVIL SEGURO APLICADO CORRECTAMENTE" -ForegroundColor Green
Write-Host "Siguiente paso: publique de nuevo la API de Azure y después genere el instalador Windows con una versión superior." -ForegroundColor Green

