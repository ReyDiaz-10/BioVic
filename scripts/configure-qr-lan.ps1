[CmdletBinding()]
param(
    [string]$LanIpAddress,
    [switch]$SkipFirewall
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$environmentPath = Join-Path $projectRoot ".env"
$environmentTemplate = Join-Path $projectRoot ".env.example"
$port = 5180
$firewallRuleName = "BioVic QR LAN (TCP 5180)"

function Update-EnvironmentValue {
    param(
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $found = $false
    foreach ($line in [System.IO.File]::ReadAllLines($environmentPath)) {
        if ($line -match "^\s*$([regex]::Escape($Key))\s*=") {
            if (-not $found) {
                $lines.Add("$Key=$Value")
                $found = $true
            }
            continue
        }
        $lines.Add($line)
    }

    if (-not $found) { $lines.Add("$Key=$Value") }
    [System.IO.File]::WriteAllLines($environmentPath, $lines, [System.Text.UTF8Encoding]::new($false))
}

if (-not (Test-Path $environmentPath)) {
    if (-not (Test-Path $environmentTemplate)) {
        throw "No se encontró .env ni .env.example en la raíz de BioVic."
    }
    Copy-Item $environmentTemplate $environmentPath
}

if ([string]::IsNullOrWhiteSpace($LanIpAddress)) {
    $candidate = Get-NetIPConfiguration |
        Where-Object { $_.NetAdapter.Status -eq "Up" -and $null -ne $_.IPv4DefaultGateway } |
        ForEach-Object { $_.IPv4Address } |
        Where-Object { $null -ne $_ -and $_.IPAddress -notlike "169.254.*" } |
        Select-Object -First 1
    $LanIpAddress = $candidate.IPAddress
}

$parsedAddress = $null
if ([string]::IsNullOrWhiteSpace($LanIpAddress) -or
    -not [System.Net.IPAddress]::TryParse($LanIpAddress, [ref]$parsedAddress) -or
    $parsedAddress.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork -or
    [System.Net.IPAddress]::IsLoopback($parsedAddress)) {
    throw "No fue posible detectar una IPv4 LAN válida. Ejecute nuevamente indicando la IP, por ejemplo: .\scripts\configure-qr-lan.ps1 -LanIpAddress 192.168.1.50"
}

Update-EnvironmentValue -Key "BIOVIC_API_URL" -Value "http://127.0.0.1:$port"
Update-EnvironmentValue -Key "BIOVIC_API_URLS" -Value "http://0.0.0.0:$port"
Update-EnvironmentValue -Key "BIOVIC_QR_BASE_URL" -Value "http://${LanIpAddress}:$port/e"

$firewallReady = $SkipFirewall.IsPresent
if (-not $SkipFirewall) {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    $isAdministrator = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    if ($isAdministrator) {
        $existingRule = Get-NetFirewallRule -DisplayName $firewallRuleName -ErrorAction SilentlyContinue
        if ($null -eq $existingRule) {
            New-NetFirewallRule -DisplayName $firewallRuleName -Direction Inbound -Action Allow `
                -Protocol TCP -LocalPort $port -Profile Private -RemoteAddress LocalSubnet | Out-Null
        }
        else {
            Set-NetFirewallRule -DisplayName $firewallRuleName -Enabled True -Action Allow -Profile Private | Out-Null
        }
        $firewallReady = $true
    }
    else {
        Write-Warning "La configuración de .env se completó, pero la regla del Firewall requiere una consola de PowerShell abierta como Administrador."
    }
}

Write-Host ""
Write-Host "Configuración QR LAN guardada sin modificar credenciales ni otros valores de .env." -ForegroundColor Green
Write-Host "IP del servidor: $LanIpAddress"
Write-Host "Prueba desde el teléfono: http://${LanIpAddress}:$port/health"
Write-Host ""
if (-not $firewallReady) {
    Write-Host "Abra PowerShell como Administrador y ejecute:" -ForegroundColor Yellow
    Write-Host "powershell -ExecutionPolicy Bypass -File .\scripts\configure-qr-lan.ps1 -LanIpAddress $LanIpAddress"
}
Write-Host "Cierre BioVic si está abierto y vuelva a iniciarlo con .\scripts\run-dev.ps1."
