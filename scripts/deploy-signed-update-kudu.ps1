param(
    [Parameter(Mandatory = $true)]
    [string]$PublishSettingsPath,

    [string]$ReleaseDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ReleaseDirectory)) {
    $ReleaseDirectory = Join-Path $projectRoot "artifacts\client-win-x64\installer"
}
$manifestPath = Join-Path $ReleaseDirectory "manifest.json"
if (-not (Test-Path $manifestPath)) { throw "No se encontró manifest.json. Cree primero una versión firmada." }
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$installerPath = Join-Path $ReleaseDirectory ([string]$manifest.fileName)
if (-not (Test-Path $installerPath)) { throw "No se encontró el instalador indicado por el manifiesto." }
if ((Get-FileHash $installerPath -Algorithm SHA256).Hash -ne [string]$manifest.sha256) {
    throw "El SHA-256 local no coincide con el manifiesto. La publicación fue cancelada."
}

[xml]$settings = Get-Content ([IO.Path]::GetFullPath($PublishSettingsPath))
$profile = @($settings.publishData.publishProfile) | Where-Object { $_.publishMethod -eq "MSDeploy" } | Select-Object -First 1
if ($null -eq $profile) { throw "El perfil no contiene credenciales MSDeploy/Kudu." }
$kuduHost = ([string]$profile.publishUrl).Split(':')[0]
$kuduBase = "https://$kuduHost"
$credentialBytes = [Text.Encoding]::ASCII.GetBytes("$($profile.userName):$($profile.userPWD)")
$headers = @{ Authorization = "Basic $([Convert]::ToBase64String($credentialBytes))" }

$commandBody = @{ command = "mkdir -p /home/data/biovic-updates"; dir = "/home" } | ConvertTo-Json
Invoke-RestMethod -Uri "$kuduBase/api/command" -Method Post -Headers $headers -ContentType "application/json" -Body $commandBody | Out-Null
foreach ($file in @($installerPath, $manifestPath)) {
    $name = [Uri]::EscapeDataString([IO.Path]::GetFileName($file))
    Invoke-WebRequest -Uri "$kuduBase/api/vfs/data/biovic-updates/$name" -Method Put -Headers $headers `
        -ContentType "application/octet-stream" -InFile $file -UseBasicParsing | Out-Null
}

$publicManifestUrl = [string]$manifest.downloadUrl
$publicBase = $publicManifestUrl.Substring(0, $publicManifestUrl.IndexOf("/api/updates/download/"))
$published = Invoke-RestMethod "$publicBase/api/updates/manifest" -TimeoutSec 90
if ($published.sha256 -ne $manifest.sha256 -or $published.signature -ne $manifest.signature) {
    throw "Azure respondió, pero el manifiesto publicado no coincide con el archivo local."
}
Write-Host "ACTUALIZACIÓN FIRMADA PUBLICADA CORRECTAMENTE" -ForegroundColor Green
Write-Host "Versión: $($manifest.version)"
Write-Host "SHA-256: $($manifest.sha256)"
