[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl,

    [Parameter(Mandatory = $true)]
    [Guid]$EquipmentPublicId,

    [Parameter(Mandatory = $true)]
    [string]$SearcherUsername,

    [Parameter(Mandatory = $true)]
    [string]$EditorUsername,

    [Parameter(Mandatory = $true)]
    [string]$MasterUsername
)

$ErrorActionPreference = "Stop"
$ApiUrl = $ApiUrl.Trim().TrimEnd('/')
$parsedApiUrl = $null
if (-not [Uri]::TryCreate($ApiUrl, [UriKind]::Absolute, [ref]$parsedApiUrl) -or -not $ApiUrl.StartsWith("https://")) {
    throw "ApiUrl debe ser HTTPS."
}
$qrUrl = "$ApiUrl/e/$($EquipmentPublicId.ToString('D'))"

function Convert-Password([Security.SecureString]$SecurePassword) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}

function Get-Token([string]$Html) {
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken" value="([^"]+)"')
    if (-not $match.Success) { throw "No se encontró el token antifalsificación." }
    [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}

function Test-Role([string]$Label, [string]$Username, [Security.SecureString]$SecurePassword,
    [string]$UserAgent, [bool]$CanWrite, [bool]$CanAdminister) {
    Write-Host "Probando $Label con sesión y agente independientes..." -ForegroundColor Cyan
    $loginPage = Invoke-WebRequest $qrUrl -SessionVariable session -UserAgent $UserAgent -TimeoutSec 60
    if ($loginPage.Content -notmatch "Acceso protegido") { throw "${Label}: no apareció el acceso protegido." }
    $password = Convert-Password $SecurePassword
    try {
        $equipmentPage = Invoke-WebRequest "$qrUrl/login" -Method Post -WebSession $session -UserAgent $UserAgent `
            -Body @{ username = $Username; password = $password; __RequestVerificationToken = (Get-Token $loginPage.Content) } `
            -TimeoutSec 90
    }
    finally { $password = $null }
    if ($equipmentPage.Content -notmatch "HOJA DE VIDA DEL EQUIPO") { throw "${Label}: el inicio de sesión no abrió el equipo." }
    if ($equipmentPage.Content -notmatch "Documentos y certificados") { throw "${Label}: no aparece la sección documental." }
    $hasWrite = $equipmentPage.Content -match "Agregar mantenimiento"
    $hasAdmin = $equipmentPage.Content -match "Editar ficha del equipo"
    if ($hasWrite -ne $CanWrite) { throw "${Label}: permisos operativos inesperados." }
    if ($hasAdmin -ne $CanAdminister) { throw "${Label}: permisos administrativos inesperados." }
    if ($equipmentPage.Content -match "Eliminar|Borrar") { throw "${Label}: apareció una acción móvil destructiva." }
    $pdf = Invoke-WebRequest "$qrUrl/hoja-de-vida.pdf" -WebSession $session -UserAgent $UserAgent -TimeoutSec 90
    if ($pdf.StatusCode -ne 200 -or $pdf.Headers['Content-Type'] -notmatch 'application/pdf') { throw "${Label}: falló la descarga PDF." }
    $logout = Invoke-WebRequest "$qrUrl/logout" -Method Post -WebSession $session -UserAgent $UserAgent `
        -Body @{ __RequestVerificationToken = (Get-Token $equipmentPage.Content) } -TimeoutSec 60
    if ($logout.Content -notmatch "Ingresar y ver hoja de vida") { throw "${Label}: cerrar sesión no volvió al inicio del QR." }
}

$searcherPassword = Read-Host "Contraseña del Consultor $SearcherUsername" -AsSecureString
$editorPassword = Read-Host "Contraseña del Editor $EditorUsername" -AsSecureString
$masterPassword = Read-Host "Contraseña del Administrador Maestro $MasterUsername" -AsSecureString
Test-Role "Consultor" $SearcherUsername $searcherPassword "Mozilla/5.0 (Linux; Android 14) Chrome/126 Mobile" $false $false
Test-Role "Editor" $EditorUsername $editorPassword "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) Version/17 Mobile Safari" $true $false
Test-Role "Administrador Maestro" $MasterUsername $masterPassword "Mozilla/5.0 (Linux; Android 13; SM-A536B) SamsungBrowser/25 Mobile" $true $true
Write-Host "PRUEBAS E2E MÓVILES CORRECTAS: 3 sesiones, 3 roles, PDF, permisos y cierre de sesión." -ForegroundColor Green
