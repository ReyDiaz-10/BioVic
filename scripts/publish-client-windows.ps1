param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl,

    [string]$Version = "1.0.0",

    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64",

    [switch]$SkipInstaller,

    [string]$SigningCertificateThumbprint,

    [string]$TimestampServer = "http://timestamp.digicert.com",

    [string]$ReleaseNotes = "Mejoras de seguridad y estabilidad de BioVic.",

    [switch]$MandatoryUpdate,

    [switch]$DevelopmentUnsigned
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$normalizedApiUrl = $ApiUrl.Trim().TrimEnd('/')
$parsedApiUrl = $null
if (-not [Uri]::TryCreate($normalizedApiUrl, [UriKind]::Absolute, [ref]$parsedApiUrl) -or
    $parsedApiUrl.Scheme -ne "https" -or
    $parsedApiUrl.AbsolutePath.Trim('/') -ne "" -or
    $parsedApiUrl.Query -ne "" -or
    $parsedApiUrl.Fragment -ne "") {
    throw "ApiUrl debe ser la raiz HTTPS del servidor, por ejemplo https://mi-biovic.azurewebsites.net"
}

$parsedVersion = $null
if (-not [Version]::TryParse($Version, [ref]$parsedVersion)) {
    throw "Version debe tener un formato valido, por ejemplo 1.0.0."
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$desktopProject = Join-Path $projectRoot "src\BioVic.Desktop\BioVic.Desktop.csproj"
$installerScript = Join-Path $projectRoot "installers\BioVic.iss"
$artifactsRoot = Join-Path $projectRoot "artifacts"
$packageRoot = Join-Path $artifactsRoot "client-$RuntimeIdentifier"
$appOutput = Join-Path $packageRoot "app"
$installerOutput = Join-Path $packageRoot "installer"
$portableZip = Join-Path $packageRoot "BioVic-Portable-$RuntimeIdentifier.zip"
$manifestPath = Join-Path $installerOutput "manifest.json"
$updateManifestUrl = "$normalizedApiUrl/api/updates/manifest"
$signingKey = $null
$publicKeyPem = $null

if (-not $SkipInstaller -and -not $DevelopmentUnsigned) {
    if ([string]::IsNullOrWhiteSpace($SigningCertificateThumbprint)) {
        throw "Para una entrega real indique -SigningCertificateThumbprint. Use -DevelopmentUnsigned solo para pruebas locales."
    }
    $normalizedThumbprint = $SigningCertificateThumbprint.Replace(" ", "").ToUpperInvariant()
    $certificate = Get-Item "Cert:\CurrentUser\My\$normalizedThumbprint" -ErrorAction Stop
    if (-not $certificate.HasPrivateKey) { throw "El certificado de firma no contiene una clave privada." }
    if ($certificate.NotAfter -le [DateTime]::UtcNow) { throw "El certificado de firma está vencido." }
    $signingKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($certificate)
    if ($null -eq $signingKey) { throw "El certificado debe utilizar una clave RSA." }
    $publicKeyPem = $signingKey.ExportSubjectPublicKeyInfoPem()
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontro el SDK estable de .NET 10."
}

if (Test-Path $packageRoot) {
    $safeParent = [System.IO.Path]::GetFullPath($artifactsRoot)
    $resolvedPackageRoot = [System.IO.Path]::GetFullPath($packageRoot)
    if (-not $resolvedPackageRoot.StartsWith($safeParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "La ruta de salida no esta dentro de artifacts."
    }
    Remove-Item $packageRoot -Recurse -Force
}

New-Item $appOutput -ItemType Directory -Force | Out-Null
New-Item $installerOutput -ItemType Directory -Force | Out-Null

& dotnet restore $desktopProject --runtime $RuntimeIdentifier
if ($LASTEXITCODE -ne 0) {
    throw "La restauracion del cliente BioVic fallo con codigo ${LASTEXITCODE}."
}

& dotnet publish $desktopProject `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $appOutput `
    --no-restore `
    -p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    throw "La publicacion del cliente BioVic fallo con codigo ${LASTEXITCODE}."
}

$clientConfiguration = [ordered]@{ apiUrl = $normalizedApiUrl }
if (-not [string]::IsNullOrWhiteSpace($publicKeyPem)) {
    $clientConfiguration.updateManifestUrl = $updateManifestUrl
    $clientConfiguration.updatePublicKeyPem = $publicKeyPem
}
$clientConfiguration |
    ConvertTo-Json |
    Set-Content (Join-Path $appOutput "biovic.client.json") -Encoding UTF8

$exampleConfiguration = Join-Path $appOutput "biovic.client.example.json"
if (Test-Path $exampleConfiguration) {
    Remove-Item $exampleConfiguration -Force
}

$forbiddenFiles = Get-ChildItem $appOutput -Recurse -File | Where-Object {
    $_.Name -eq ".env" -or
    $_.Name -like "*.db" -or
    $_.Name -like "*.db-shm" -or
    $_.Name -like "*.db-wal" -or
    $_.Name -like "*.log"
}
if ($forbiddenFiles) {
    throw "El cliente contiene secretos o datos del servidor y la publicacion fue cancelada."
}

Compress-Archive -Path (Join-Path $appOutput "*") -DestinationPath $portableZip -CompressionLevel Optimal

if (-not $SkipInstaller) {
    $isccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($null -eq $isccCommand) {
        $programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
        $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
        $knownIsccPaths = @(
            (Join-Path $programFiles "Inno Setup 7\ISCC.exe"),
            (Join-Path $programFilesX86 "Inno Setup 7\ISCC.exe"),
            (Join-Path $programFilesX86 "Inno Setup 6\ISCC.exe")
        )
        $knownIscc = $knownIsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
        if ($null -ne $knownIscc) {
            $isccPath = $knownIscc
        }
        else {
            throw "No se encontro Inno Setup. Instale JRSoftware.InnoSetup.7 con winget y ejecute de nuevo este script."
        }
    }
    else {
        $isccPath = $isccCommand.Source
    }

    & $isccPath `
        "/DSourceDir=$appOutput" `
        "/DOutputDir=$installerOutput" `
        "/DAppVersion=$Version" `
        $installerScript
    if ($LASTEXITCODE -ne 0) {
        throw "La creacion del instalador fallo con codigo ${LASTEXITCODE}."
    }

    $installerPath = Join-Path $installerOutput "BioVic-Setup-win-x64.exe"
    if (-not (Test-Path $installerPath)) { throw "Inno Setup no creó el instalador esperado." }
    if (-not $DevelopmentUnsigned) {
        $signatureResult = Set-AuthenticodeSignature -FilePath $installerPath -Certificate $certificate `
            -HashAlgorithm SHA256 -TimestampServer $TimestampServer
        if ($signatureResult.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            throw "La firma Authenticode falló: $($signatureResult.StatusMessage)"
        }

        $installerInfo = Get-Item $installerPath
        $installerHash = (Get-FileHash $installerPath -Algorithm SHA256).Hash.ToUpperInvariant()
        $publishedAt = [DateTimeOffset]::UtcNow
        $downloadUrl = "$normalizedApiUrl/api/updates/download/$($installerInfo.Name)"
        $notesBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($ReleaseNotes))
        $canonical = @(
            $parsedVersion.ToString(),
            $installerInfo.Name,
            $downloadUrl,
            $installerHash,
            $installerInfo.Length.ToString([Globalization.CultureInfo]::InvariantCulture),
            $publishedAt.ToString("O", [Globalization.CultureInfo]::InvariantCulture),
            $(if ($MandatoryUpdate) { "1" } else { "0" }),
            $notesBase64
        ) -join "`n"
        $signatureBytes = $signingKey.SignData([Text.Encoding]::UTF8.GetBytes($canonical),
            [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        [ordered]@{
            version = $parsedVersion.ToString()
            fileName = $installerInfo.Name
            downloadUrl = $downloadUrl
            sha256 = $installerHash
            sizeBytes = $installerInfo.Length
            publishedAtUtc = $publishedAt.ToString("O", [Globalization.CultureInfo]::InvariantCulture)
            releaseNotes = $ReleaseNotes
            mandatory = [bool]$MandatoryUpdate
            signature = [Convert]::ToBase64String($signatureBytes)
        } | ConvertTo-Json | Set-Content $manifestPath -Encoding UTF8
    }
}

Write-Host "Cliente BioVic publicado para: $normalizedApiUrl" -ForegroundColor Green
Write-Host "Carpeta autocontenida: $appOutput"
Write-Host "Paquete portatil: $portableZip"
if (-not $SkipInstaller) {
    Write-Host "Instalador: $installerOutput"
    if ($DevelopmentUnsigned) {
        Write-Host "MODO DE PRUEBA: el instalador no está firmado y las actualizaciones automáticas están deshabilitadas." -ForegroundColor Yellow
    }
    else {
        Write-Host "Instalador Authenticode y manifiesto RSA firmados correctamente." -ForegroundColor Green
        Write-Host "Manifiesto de actualización: $manifestPath"
    }
}
Write-Host "El usuario final no necesita .NET, .env, ngrok, SQLite ni ejecutar scripts."
