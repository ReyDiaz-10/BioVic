[CmdletBinding()]
param(
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$projectFile = Join-Path $root "BioVic.sln"
if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "No se encontró BioVic.sln en '$root'. Abra PowerShell en la carpeta raíz de BioVic y ejecute nuevamente el script."
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $root "artifacts\source-backup-reglas-locales-$stamp"
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Get-ProjectPath {
    param([Parameter(Mandatory)][string]$RelativePath)
    return Join-Path $root ($RelativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
}

function Save-Backup {
    param([Parameter(Mandatory)][string]$RelativePath)
    $source = Get-ProjectPath $RelativePath
    if (-not (Test-Path -LiteralPath $source)) {
        throw "No se encontró el archivo requerido: $RelativePath"
    }
    $destination = Join-Path $backupRoot ($RelativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
    $destinationDirectory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

function Set-ProjectText {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][string]$Text
    )
    $path = Get-ProjectPath $RelativePath
    Save-Backup $RelativePath
    [IO.File]::WriteAllText($path, $Text, $utf8NoBom)
    Write-Host "ACTUALIZADO: $RelativePath" -ForegroundColor Cyan
}

function Replace-One {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Replacement,
        [Parameter(Mandatory)][string]$Description,
        [System.Text.RegularExpressions.RegexOptions]$Options = [System.Text.RegularExpressions.RegexOptions]::None
    )
    $regex = [regex]::new($Pattern, $Options)
    $matches = $regex.Matches($Text)
    if ($matches.Count -ne 1) {
        throw "No se pudo aplicar '$Description': se esperó una coincidencia y se encontraron $($matches.Count). No se modificó ese archivo."
    }
    return $regex.Replace($Text, $Replacement, 1)
}

Write-Host "Aplicando revisión automática local de BioVic..." -ForegroundColor Green
Write-Host "Copia de seguridad: $backupRoot"

# 1. Sustituir exclusivamente la consulta generativa por reglas determinísticas locales.
$outputRelative = "src/BioVic.Infrastructure/Services/EquipmentOutputService.cs"
$outputPath = Get-ProjectPath $outputRelative
$output = [IO.File]::ReadAllText($outputPath)

if (-not $output.Contains("EQUIPMENT_RULE_REVIEW")) {
    $output = [regex]::Replace(
        $output,
        '(?m)^using (System\.Net\.Http\.Headers|System\.Net\.Http\.Json|System\.Text|System\.Text\.Json);\r?\n',
        '')

    $hasEmailFeature = $output.Contains("SendLifeSheetEmailAsync")
    if ($hasEmailFeature) {
        $output = Replace-One $output `
            'public sealed class EquipmentOutputService\(BioVicDbContext db, IAuditService audit, BioVicOptions options,\s*IHttpClientFactory httpClientFactory, LocationAccessService locationAccess, QrAccessService qrAccess\)' `
            "public sealed class EquipmentOutputService(BioVicDbContext db, IAuditService audit, BioVicOptions options,`r`n    LocationAccessService locationAccess, QrAccessService qrAccess)" `
            "retirar el cliente HTTP del servicio"
    }
    else {
        $output = [regex]::Replace($output, '(?m)^using BioVic\.Infrastructure\.Options;\r?\n', '')
        $output = Replace-One $output `
            'public sealed class EquipmentOutputService\(BioVicDbContext db, IAuditService audit, BioVicOptions options,\s*IHttpClientFactory httpClientFactory, LocationAccessService locationAccess, QrAccessService qrAccess\)' `
            "public sealed class EquipmentOutputService(BioVicDbContext db, IAuditService audit,`r`n    LocationAccessService locationAccess, QrAccessService qrAccess)" `
            "retirar el proveedor externo del constructor"
    }

    $localMethod = @'
    public async Task<AiAnswerDto> AskAsync(Guid equipmentId, AiQuestionRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question)) throw new ValidationException("Ejecute una revisión automática válida.");
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var equipment = await LoadAsync(equipmentId, cancellationToken);
        var documents = await db.EquipmentDocuments.AsNoTracking()
            .Where(item => item.EquipmentId == equipmentId && item.IsCurrent && !item.IsDeleted)
            .Select(item => new
            {
                item.DocumentType,
                item.OriginalName,
                item.ApprovedForAiKnowledge
            })
            .ToListAsync(cancellationToken);

        var hasPhoto = documents.Any(item => item.DocumentType == EquipmentDocumentTypes.Photo);
        var hasApprovedManual = documents.Any(item => item.DocumentType == EquipmentDocumentTypes.Manual
            && item.ApprovedForAiKnowledge);
        var hasApprovedTechnicalSheet = documents.Any(item => item.DocumentType == EquipmentDocumentTypes.TechnicalSheet
            && item.ApprovedForAiKnowledge);
        var alerts = EquipmentRuleEngine.Evaluate(new(equipment.Status, equipment.NextMaintenanceUtc, equipment.RequiresCalibration,
            equipment.NextCalibrationUtc, hasPhoto, !string.IsNullOrWhiteSpace(equipment.Manufacturer), !string.IsNullOrWhiteSpace(equipment.Model),
            !string.IsNullOrWhiteSpace(equipment.ResponsibleName), equipment.Location is not null, hasApprovedManual,
            hasApprovedTechnicalSheet), DateTimeOffset.UtcNow).Select(item => item.Message).ToList();
        var summary = alerts.Count == 0
            ? "No se identificaron alertas automáticas con la información registrada."
            : string.Join(" ", alerts);
        var approvedReferences = documents
            .Where(item => item.ApprovedForAiKnowledge)
            .Select(item => item.OriginalName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var referenceSummary = approvedReferences.Count == 0
            ? "No hay referencias técnicas aprobadas para este equipo."
            : "Referencias técnicas aprobadas: " + string.Join(", ", approvedReferences) + ".";

        await audit.AppendAsync(new(actor, "EQUIPMENT_RULE_REVIEW", "Equipment", equipmentId.ToString("D"), equipmentId,
            "Se ejecutó la revisión automática local del equipo."), cancellationToken);

        return new($"{summary}\n\n{referenceSummary}\n\nRevisión realizada mediante reglas locales de BioVic. No se enviaron datos a servicios externos.",
            alerts, "Reglas locales BioVic", false);
    }

'@
    $output = Replace-One $output `
        '(?s)    public async Task<AiAnswerDto> AskAsync\(.*?\r?\n    }\r?\n\r?\n(?=    private async Task<BioVic\.Domain\.Entities\.Equipment> LoadAsync)' `
        $localMethod `
        "reemplazar la consulta generativa por reglas locales"
    $output = [regex]::Replace($output, '(?m)^    private const string SystemPrompt = .*?;\r?\n', '')
    $output = [regex]::Replace($output, '(?m)^    private static string Limit\(string value, int length\) => .*?;\r?\n', '')
    Set-ProjectText $outputRelative $output
}
else {
    Write-Host "SIN CAMBIOS: el servicio ya usa reglas locales." -ForegroundColor DarkGray
}

# 2. Retirar únicamente las opciones BIOVIC_AI_*; no tocar la configuración de migraciones SQL Server.
$diRelative = "src/BioVic.Infrastructure/InfrastructureServiceCollectionExtensions.cs"
$diPath = Get-ProjectPath $diRelative
$di = [IO.File]::ReadAllText($diPath)
$diUpdated = [regex]::Replace(
    $di,
    '(?m)^\s*(AiEnabled|AiProvider|AiBaseUrl|AiModel|AiApiKey)\s*=.*?(,)?\r?\n',
    '')
$diUpdated = [regex]::Replace(
    $diUpdated,
    '(QrBaseUrl\s*=\s*[^\r\n]+),\r?\n\s*};',
    { param($match) $match.Groups[1].Value + "`r`n        };" })
if ($diUpdated -ne $di) { Set-ProjectText $diRelative $diUpdated }
else { Write-Host "SIN CAMBIOS: no hay opciones BIOVIC_AI_* en la configuración." -ForegroundColor DarkGray }

$optionsRelative = "src/BioVic.Infrastructure/Options/BioVicOptions.cs"
$optionsPath = Get-ProjectPath $optionsRelative
$options = [IO.File]::ReadAllText($optionsPath)
$optionsUpdated = [regex]::Replace(
    $options,
    '(?m)^\s*public\s+(bool|string)\s+(AiEnabled|AiProvider|AiBaseUrl|AiModel|AiApiKey)\s*\{[^\r\n]+\}\r?\n',
    '')
if ($optionsUpdated -ne $options) { Set-ProjectText $optionsRelative $optionsUpdated }
else { Write-Host "SIN CAMBIOS: BioVicOptions ya no expone opciones de IA." -ForegroundColor DarkGray }

# 3. Interfaz: revisión automática, sin caja de preguntas ni promesas de IA.
$detailsXamlRelative = "src/BioVic.Desktop/Views/EquipmentDetailsWindow.xaml"
$detailsXamlPath = Get-ProjectPath $detailsXamlRelative
$detailsXaml = [IO.File]::ReadAllText($detailsXamlPath)
if ($detailsXaml.Contains('Header="BioVic IA"')) {
    $reviewTab = @'
                <TabItem Header="Revisión automática">
                    <Grid Margin="8,18"><Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions><TextBlock Text="BioVic revisa fechas, estado, datos obligatorios y referencias técnicas mediante reglas locales. Ningún dato se envía a servicios externos." TextWrapping="Wrap" Foreground="{StaticResource MutedBrush}"/><Button Grid.Row="1" Content="Revisar equipo" HorizontalAlignment="Left" Style="{StaticResource PrimaryButton}" Margin="0,12" Click="ReviewButton_Click"/><TextBox x:Name="ReviewAnswerBox" Grid.Row="2" IsReadOnly="True" AcceptsReturn="True" TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"/></Grid>
                </TabItem>
'@
    $detailsXaml = Replace-One $detailsXaml `
        '(?s)                <TabItem Header="BioVic IA">.*?                </TabItem>\r?\n' `
        $reviewTab `
        "actualizar la pestaña BioVic IA"
    Set-ProjectText $detailsXamlRelative $detailsXaml
}
else {
    Write-Host "SIN CAMBIOS: la pestaña ya se llama Revisión automática." -ForegroundColor DarkGray
}

$detailsCodeRelative = "src/BioVic.Desktop/Views/EquipmentDetailsWindow.xaml.cs"
$detailsCodePath = Get-ProjectPath $detailsCodeRelative
$detailsCode = [IO.File]::ReadAllText($detailsCodePath)
if ($detailsCode.Contains("AskAiButton_Click")) {
    $reviewHandler = @'
    private async void ReviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_equipment is null) return;
        ReviewAnswerBox.Text = "Revisando…";
        try
        {
            var answer = await _client.AskAiAsync(_equipment.Id, "Ejecutar revisión automática local");
            ReviewAnswerBox.Text = answer.Answer;
        }
        catch (Exception exception)
        {
            ReviewAnswerBox.Text = exception.Message;
        }
    }
'@
    $detailsCode = Replace-One $detailsCode `
        '(?ms)^    private async void AskAiButton_Click\(.*?^    }\r?\n' `
        $reviewHandler `
        "actualizar el controlador de revisión"
    Set-ProjectText $detailsCodeRelative $detailsCode
}
else {
    Write-Host "SIN CAMBIOS: el controlador de revisión ya está actualizado." -ForegroundColor DarkGray
}

$editorRelative = "src/BioVic.Desktop/Views/EquipmentDocumentEditorWindow.xaml"
$editorPath = Get-ProjectPath $editorRelative
$editor = [IO.File]::ReadAllText($editorPath)
$editorUpdated = $editor.Replace(
    'Aprobar para conocimiento de BioVic IA',
    'Marcar como referencia técnica aprobada para las reglas de BioVic')
if ($editorUpdated -ne $editor) { Set-ProjectText $editorRelative $editorUpdated }

$documentsRelative = "src/BioVic.Desktop/Views/EquipmentDocumentsWindow.xaml"
$documentsPath = Get-ProjectPath $documentsRelative
$documents = [IO.File]::ReadAllText($documentsPath)
$documentsUpdated = [regex]::Replace(
    $documents,
    '<DataGridCheckBoxColumn Header="IA" Binding="\{Binding ApprovedForAiKnowledge\}"[^>]*/>',
    '<DataGridCheckBoxColumn Header="Referencia" Binding="{Binding ApprovedForAiKnowledge}" Width="80"/>')
if ($documentsUpdated -ne $documents) { Set-ProjectText $documentsRelative $documentsUpdated }

$adminRelative = "src/BioVic.Desktop/Views/AdminWindow.xaml"
$adminPath = Get-ProjectPath $adminRelative
$admin = [IO.File]::ReadAllText($adminPath)
$adminUpdated = $admin.Replace("Bandeja BioVic IA", "Bandeja BioVic")
if ($adminUpdated -ne $admin) { Set-ProjectText $adminRelative $adminUpdated }

$labelsRelative = "src/BioVic.Desktop/Converters/SpanishLabels.cs"
$labelsPath = Get-ProjectPath $labelsRelative
$labels = [IO.File]::ReadAllText($labelsPath)
$labelsUpdated = $labels.Replace(
    '"AI_QUESTION" => "Consulta a BioVic IA",',
    '"AI_QUESTION" => "Consulta histórica a BioVic IA", "EQUIPMENT_RULE_REVIEW" => "Revisión automática local",')
if (-not $labelsUpdated.Contains('"EQUIPMENT_RULE_REVIEW"')) {
    $labelsUpdated = $labelsUpdated.Replace(
        '"AI_QUESTION" => "Consulta histórica a BioVic IA",',
        '"AI_QUESTION" => "Consulta histórica a BioVic IA", "EQUIPMENT_RULE_REVIEW" => "Revisión automática local",')
}
if ($labelsUpdated -ne $labels) { Set-ProjectText $labelsRelative $labelsUpdated }

# 4. Agregar una prueba de regresión sin reemplazar las pruebas existentes.
$testsRelative = "tests/BioVic.Tests/EquipmentRuleEngineTests.cs"
$testsPath = Get-ProjectPath $testsRelative
$tests = [IO.File]::ReadAllText($testsPath)
if (-not $tests.Contains("Evaluate_DoesNotReportPresentTechnicalReferencesAsMissing")) {
    $testMethod = @'

    [Fact]
    public void Evaluate_DoesNotReportPresentTechnicalReferencesAsMissing()
    {
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var input = new EquipmentRuleInput(EquipmentStatus.Operational, now.AddDays(90), true, now.AddDays(90),
            true, true, true, true, true, true, true);

        var alerts = EquipmentRuleEngine.Evaluate(input, now);

        Assert.DoesNotContain(alerts, alert => alert.Code == "MISSING_PHOTO");
        Assert.DoesNotContain(alerts, alert => alert.Code == "MISSING_MANUAL");
        Assert.DoesNotContain(alerts, alert => alert.Code == "MISSING_TECHNICAL_SHEET");
    }
'@
    $lastBrace = $tests.LastIndexOf('}')
    if ($lastBrace -lt 0) { throw "No se pudo localizar el cierre de EquipmentRuleEngineTests.cs." }
    $tests = $tests.Insert($lastBrace, $testMethod)
    Set-ProjectText $testsRelative $tests
}

# 5. Verificaciones estáticas antes de entregar el proyecto al compilador.
$serviceCheck = [IO.File]::ReadAllText($outputPath)
$xamlCheck = [IO.File]::ReadAllText($detailsXamlPath)
$codeCheck = [IO.File]::ReadAllText($detailsCodePath)
$diCheck = [IO.File]::ReadAllText($diPath)

$problems = [Collections.Generic.List[string]]::new()
if ($serviceCheck -match 'IHttpClientFactory|PostAsJsonAsync|SystemPrompt|options\.Ai') {
    $problems.Add("El servicio todavía contiene una ruta hacia un proveedor externo.")
}
if (-not $serviceCheck.Contains("EQUIPMENT_RULE_REVIEW")) {
    $problems.Add("No quedó registrada la revisión automática local.")
}
if (-not ($xamlCheck.Contains("ReviewButton_Click") -and $codeCheck.Contains("ReviewButton_Click"))) {
    $problems.Add("La pantalla y su controlador no quedaron enlazados.")
}
if ($diCheck -match 'BIOVIC_AI_|AiEnabled|AiProvider|AiBaseUrl|AiModel|AiApiKey') {
    $problems.Add("Todavía existen opciones BIOVIC_AI_* en la configuración ejecutable.")
}
if (-not $diCheck.Contains('MigrationsAssembly("BioVic.Infrastructure.SqlServerMigrations")')) {
    Write-Warning "No se detectó MigrationsAssembly para SQL Server. Este script no lo eliminó, pero verifique que conserve la corrección aplicada anteriormente."
}

if ($problems.Count -gt 0) {
    throw ("La aplicación del cambio no superó las comprobaciones:`n- " + ($problems -join "`n- "))
}

Write-Host "" 
Write-Host "BIOVIC QUEDÓ CONFIGURADO CON REGLAS LOCALES" -ForegroundColor Green
Write-Host "No se requiere Azure AI Foundry, Ollama, claves de IA ni cuotas." -ForegroundColor Green
Write-Host "No se creó ninguna migración de base de datos." -ForegroundColor Green
Write-Host "" 
Write-Host "Siguiente comando:" -ForegroundColor Yellow
Write-Host 'pwsh.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-comprehensive-source.ps1'
