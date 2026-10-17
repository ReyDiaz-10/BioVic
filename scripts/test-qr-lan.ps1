[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$environmentPath = Join-Path $projectRoot ".env"

if (-not (Test-Path $environmentPath)) { throw "No se encontró el archivo .env." }

$values = @{}
foreach ($rawLine in [System.IO.File]::ReadAllLines($environmentPath)) {
    $line = $rawLine.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith("#")) { continue }
    $separator = $line.IndexOf("=")
    if ($separator -le 0) { continue }
    $values[$line.Substring(0, $separator).Trim()] = $line.Substring($separator + 1).Trim().Trim('"')
}

$qrBaseUrl = $values["BIOVIC_QR_BASE_URL"]
if ([string]::IsNullOrWhiteSpace($qrBaseUrl)) { throw "BIOVIC_QR_BASE_URL no está configurado." }

$qrUri = [Uri]$qrBaseUrl
$healthUrl = "{0}://{1}:{2}/health" -f $qrUri.Scheme, $qrUri.Host, $qrUri.Port
$localHealthUrl = "http://127.0.0.1:5180/health"

try {
    $localHealth = Invoke-RestMethod $localHealthUrl -TimeoutSec 5
}
catch {
    throw "BioVic.Api no responde localmente en $localHealthUrl. Inicie la aplicación con scripts/run-dev.ps1 y manténgala abierta."
}

if ($localHealth.qrRemoteAccess -ne $true) {
    $listener = Get-NetTCPConnection -LocalPort 5180 -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1
    $localAddress = if ($listener) { $listener.LocalAddress } else { "sin escucha" }
    $configurationFile = if ($localHealth.configurationFile) { $localHealth.configurationFile } else { "sin diagnóstico" }
    throw "La API está activa, pero inició sin acceso QR remoto. configurationFile=$configurationFile; LocalAddress=$localAddress. Reinicie con la versión corregida de scripts/run-dev.ps1."
}

Write-Host "Consultando $healthUrl ..."
try {
    $health = Invoke-RestMethod $healthUrl -TimeoutSec 10
}
catch {
    throw "La API funciona localmente, pero la dirección LAN $healthUrl no responde. Verifique que la IP siga asignada al computador, que la red sea Privada y que la regla 'BioVic QR LAN (TCP 5180)' esté habilitada."
}

if ($health.status -ne "healthy") { throw "La API respondió, pero su estado no es healthy." }
if ($health.qrRemoteAccess -ne $true) {
    throw "La API está activa, pero todavía informa qrRemoteAccess=false. Reinicie BioVic después de ejecutar configure-qr-lan.ps1."
}

Write-Host "Acceso QR por red local: LISTO" -ForegroundColor Green
$health | Format-List status, utc, startupInstance, processId, configurationFile, pdf, qrRemoteAccess
Write-Host "Ahora abra $healthUrl desde el navegador del teléfono conectado a la misma red Wi-Fi."
