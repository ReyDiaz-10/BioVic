param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd("/")
$uri = $null
if (-not [Uri]::TryCreate($BaseUrl, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne "https") {
    throw "BaseUrl debe ser una dirección HTTPS válida, por ejemplo https://biovic.institucion.org."
}

Write-Host "Comprobando $BaseUrl ..."
$healthResponse = Invoke-WebRequest "$BaseUrl/health" -TimeoutSec 15
$health = $healthResponse.Content | ConvertFrom-Json
if ($health.status -ne "healthy") {
    throw "El servidor no informó un estado saludable."
}
if ($healthResponse.Headers["X-Content-Type-Options"] -ne "nosniff") {
    throw "Falta el encabezado X-Content-Type-Options."
}
if (-not $healthResponse.Headers["Strict-Transport-Security"]) {
    Write-Warning "No se recibió Strict-Transport-Security. Revise HTTPS/HSTS en IIS y BioVic."
}

$probeId = [Guid]::NewGuid().ToString("D")
$portalResponse = Invoke-WebRequest "$BaseUrl/e/$probeId" -TimeoutSec 15
if ($portalResponse.Content -notmatch "__RequestVerificationToken") {
    throw "El formulario QR no contiene protección antifalsificación."
}
if ($portalResponse.Content -notmatch "Acceso protegido") {
    throw "El portal QR no mostró el inicio de sesión protegido."
}
if ($portalResponse.Headers["Cache-Control"] -notmatch "no-store") {
    throw "El portal QR podría almacenarse en caché."
}
if (-not $portalResponse.Headers["Content-Security-Policy"]) {
    throw "Falta Content-Security-Policy en el portal QR."
}

Write-Host "Validación pública superada: HTTPS, health, formulario protegido y encabezados de seguridad." -ForegroundColor Green
