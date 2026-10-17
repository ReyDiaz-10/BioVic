$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $projectRoot "src\BioVic.Api\BioVic.Api.csproj"
$desktopProject = Join-Path $projectRoot "src\BioVic.Desktop\BioVic.Desktop.csproj"
$healthUrl = "http://127.0.0.1:5180/health"

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "El comando dotnet falló con código ${LASTEXITCODE}: dotnet $($Arguments -join ' ')"
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontró el SDK de .NET. Instale .NET 10 antes de ejecutar BioVic."
}

if (-not (Test-Path (Join-Path $projectRoot ".env"))) {
    $environmentTemplate = Join-Path $projectRoot ".env.example"
    if (-not (Test-Path $environmentTemplate)) {
        throw "No se encontró .env ni .env.example en la raíz de BioVic."
    }

    Copy-Item $environmentTemplate (Join-Path $projectRoot ".env")
}

# La configuración de red del .env de la raíz debe ser determinista durante el
# desarrollo. Evita que una variable heredada o un directorio de trabajo
# diferente haga que la API vuelva silenciosamente a 127.0.0.1.
$environmentValues = @{}
foreach ($rawLine in [System.IO.File]::ReadAllLines((Join-Path $projectRoot ".env"))) {
    $line = $rawLine.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith("#")) { continue }
    $separator = $line.IndexOf("=")
    if ($separator -le 0) { continue }
    $environmentValues[$line.Substring(0, $separator).Trim()] = $line.Substring($separator + 1).Trim().Trim('"')
}
foreach ($networkKey in @("BIOVIC_API_URL", "BIOVIC_API_URLS", "BIOVIC_QR_BASE_URL")) {
    if ($environmentValues.ContainsKey($networkKey)) {
        [Environment]::SetEnvironmentVariable($networkKey, $environmentValues[$networkKey], "Process")
    }
}

$listeners = Get-NetTCPConnection -LocalPort 5180 -State Listen -ErrorAction SilentlyContinue
if ($listeners) {
    $processDescriptions = foreach ($processId in ($listeners.OwningProcess | Sort-Object -Unique)) {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId = $processId" -ErrorAction SilentlyContinue
        if ($process) { "PID $processId · $($process.Name) · $($process.CommandLine)" }
        else { "PID $processId" }
    }

    throw "El puerto 5180 ya está ocupado. BioVic no iniciará contra una API posiblemente antigua.`n$($processDescriptions -join "`n")`nCierre ese proceso y vuelva a ejecutar el script."
}

Write-Host "Compilando BioVic.Api y todas sus dependencias..."
Invoke-DotNet -Arguments @("build", $apiProject, "--configuration", "Debug", "--no-incremental")
Write-Host "Compilando BioVic Desktop y todas sus dependencias..."
Invoke-DotNet -Arguments @("build", $desktopProject, "--configuration", "Debug", "--no-incremental")

$startupInstance = [Guid]::NewGuid().ToString("N")
$previousStartupInstance = $env:BIOVIC_STARTUP_INSTANCE
$env:BIOVIC_STARTUP_INSTANCE = $startupInstance
$apiArguments = "run --project `"$apiProject`" --configuration Debug --no-build --no-launch-profile"
$apiProcess = $null

try {
    $apiProcess = Start-Process dotnet -ArgumentList $apiArguments -WorkingDirectory $projectRoot -PassThru
    $healthy = $false
    for ($attempt = 0; $attempt -lt 120; $attempt++) {
        if ($apiProcess.HasExited) {
            throw "BioVic.Api terminó inesperadamente con código $($apiProcess.ExitCode). Revise el registro en src\BioVic.Api\bin\Debug\net10.0\logs."
        }

        try {
            $response = Invoke-RestMethod $healthUrl -TimeoutSec 1
            if ($response.status -eq "healthy" -and $response.startupInstance -eq $startupInstance) {
                $healthy = $true
                break
            }
        }
        catch { }

        Start-Sleep -Milliseconds 500
    }

    if (-not $healthy) {
        throw "BioVic.Api no confirmó que la instancia recién compilada estuviera activa en el tiempo esperado."
    }

    if ($response.qrRemoteAccess -eq $true) {
        Write-Host "Portal QR disponible para la red configurada." -ForegroundColor Green
    }
    else {
        Write-Warning "La API está activa, pero el QR aún no está configurado para teléfonos. Ejecute .\scripts\configure-qr-lan.ps1 desde la raíz del proyecto."
    }
    if ($response.configurationFile -ne "loaded") {
        Write-Warning "La API no confirmó la carga de .env. Revise que el archivo esté en la raíz del proyecto."
    }

    Write-Host "BioVic.Api verificada. Abriendo la aplicación de escritorio..."
    Invoke-DotNet -Arguments @("run", "--project", $desktopProject, "--configuration", "Debug", "--no-build")
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id
    }

    if ($null -eq $previousStartupInstance) {
        Remove-Item Env:BIOVIC_STARTUP_INSTANCE -ErrorAction SilentlyContinue
    }
    else {
        $env:BIOVIC_STARTUP_INSTANCE = $previousStartupInstance
    }
}
