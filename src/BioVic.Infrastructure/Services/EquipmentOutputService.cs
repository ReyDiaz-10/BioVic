using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Application.Services;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using QRCoder;

namespace BioVic.Infrastructure.Services;

public sealed class EquipmentOutputService(BioVicDbContext db, IAuditService audit,
    LocationAccessService locationAccess, QrAccessService qrAccess) : IEquipmentOutputService
{
    public async Task<QrAccessInfoDto> GetQrAccessInfoAsync(Guid equipmentId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var publicId = await db.Equipment.AsNoTracking()
            .Where(equipment => equipment.Id == equipmentId)
            .Select(equipment => (Guid?)equipment.PublicId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el equipo.");
        return qrAccess.GetAccessInfo(publicId);
    }

    public async Task<byte[]> CreateQrPngAsync(Guid equipmentId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var equipment = await db.Equipment.AsNoTracking().SingleOrDefaultAsync(x => x.Id == equipmentId, cancellationToken) ?? throw new NotFoundException("No se encontró el equipo.");
        var access = qrAccess.GetAccessInfo(equipment.PublicId);
        if (!access.IsRemoteAccessReady)
            throw new ValidationException("El QR no se generó porque el enlace todavía no es accesible desde otro dispositivo. "
                + string.Join(" ", access.Warnings));
        var bytes = PngByteQRCodeHelper.GetQRCode(access.Url, QRCodeGenerator.ECCLevel.Q, 14);
        await audit.AppendAsync(new(actor, "QR_GENERATED", "Equipment", equipmentId.ToString("D"), equipmentId, $"Se generó el QR de {equipment.Name}."), cancellationToken);
        return bytes;
    }

    public async Task<byte[]> CreateLifeSheetPdfAsync(Guid equipmentId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var equipment = await LoadAsync(equipmentId, cancellationToken);
        var bytes = BuildPdf(equipment);
        await audit.AppendAsync(new(actor, "LIFE_SHEET_PDF_GENERATED", "Equipment", equipmentId.ToString("D"), equipmentId, $"Se generó la hoja de vida PDF de {equipment.Name}."), cancellationToken);
        return bytes;
    }

    public async Task<byte[]> CreatePublicLifeSheetPdfAsync(Guid publicId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        var equipment = await db.Equipment.AsNoTracking().Include(x=>x.Location).Include(x=>x.Maintenances)
            .Include(x=>x.Calibrations).Include(x=>x.Failures).Include(x=>x.Incidents).Include(x=>x.StatusHistory).AsSplitQuery()
            .SingleOrDefaultAsync(x=>x.PublicId==publicId,cancellationToken) ?? throw new NotFoundException("No se encontró el equipo.");
        await locationAccess.EnsureEquipmentAccessAsync(equipment.Id, actor, cancellationToken);
        var bytes = BuildPdf(equipment);
        await audit.AppendAsync(new(actor,"QR_PDF_VIEWED","Equipment",equipment.Id.ToString("D"),equipment.Id,$"Consulta autenticada del PDF mediante QR de {equipment.Name}."),cancellationToken);
        return bytes;
    }

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
    private async Task<BioVic.Domain.Entities.Equipment> LoadAsync(Guid id, CancellationToken ct) => await db.Equipment.AsNoTracking().Include(x => x.Location).Include(x => x.Maintenances).Include(x => x.Calibrations).Include(x => x.Failures).Include(x => x.Incidents).Include(x => x.StatusHistory).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("No se encontró el equipo.");

    private static byte[] BuildPdf(BioVic.Domain.Entities.Equipment e)
    {
        using var document = new PdfDocument(); document.Info.Title = $"Hoja de vida · {e.Name}";
        PdfPage page = document.AddPage(); page.Size = PdfSharp.PageSize.A4; XGraphics gfx = XGraphics.FromPdfPage(page);
        var title = new XFont("BioVic Sans", 18, XFontStyleEx.Bold); var section = new XFont("BioVic Sans", 12, XFontStyleEx.Bold); var body = new XFont("BioVic Sans", 9);
        double y = 42;
        void Line(string text, bool heading = false)
        {
            var font = heading ? section : body;
            var maxWidth = page.Width.Point - 80;
            foreach (var wrappedLine in WrapText(text, font, gfx, maxWidth))
            {
                if (y > page.Height.Point - 45) { gfx.Dispose(); page = document.AddPage(); page.Size = PdfSharp.PageSize.A4; gfx = XGraphics.FromPdfPage(page); y = 42; }
                gfx.DrawString(wrappedLine, font, XBrushes.Black,
                    new XRect(40, y, maxWidth, 20), XStringFormats.TopLeft);
                y += heading ? 22 : 15;
            }
        }
        gfx.DrawString("BIOVIC · HOJA DE VIDA DEL EQUIPO", title,
            new XSolidBrush(XColor.FromArgb(18, 58, 67)),
            new XRect(40, y, page.Width.Point - 80, 28), XStringFormats.TopLeft); y += 34;
        Line($"Equipo: {e.Name}", true); Line($"Código: {e.InternalCode}   Serial: {e.SerialNumber ?? "No registrado"}"); Line($"Número de inventario: {e.InventoryNumber ?? "No registrado"}   Código de activo: {e.AssetCode ?? "No registrado"}"); Line($"Tipo / categoría: {e.EquipmentType ?? "No registrado"} / {e.Category ?? "No registrada"}"); Line($"Marca / modelo: {e.Brand ?? "No registrado"} / {e.Model ?? "No registrado"}"); Line($"Fabricante: {e.Manufacturer ?? "No registrado"}"); if (!string.IsNullOrWhiteSpace(e.Description)) Line($"Descripción: {e.Description}"); Line($"Ubicación: {e.Location?.DisplayName ?? "Ubicación no disponible"}"); Line($"Responsable: {e.ResponsibleName ?? "No registrado"} · Cargo: {e.ResponsiblePosition ?? "No registrado"}"); Line($"Estado: {StatusLabel(e.Status)}   Criticidad: {CriticalityLabel(e.Criticality)}"); Line($"Próximo mantenimiento: {Date(e.NextMaintenanceUtc)}   Próxima calibración: {Date(e.NextCalibrationUtc)}");
        if (e.AcquisitionDateUtc is not null || !string.IsNullOrWhiteSpace(e.Supplier) || e.AcquisitionCost is not null) { Line("ADQUISICIÓN", true); Line($"Fecha: {Date(e.AcquisitionDateUtc)} · Proveedor: {e.Supplier ?? "No registrado"} · Costo: {(e.AcquisitionCost is null ? "No registrado" : e.AcquisitionCost.Value.ToString("N2"))}"); Line($"Factura: {e.InvoiceNumber ?? "No registrada"} · Contrato: {e.ContractNumber ?? "No registrado"} · Garantía hasta: {Date(e.WarrantyEndUtc)}"); }
        if (!string.IsNullOrWhiteSpace(e.Voltage) || !string.IsNullOrWhiteSpace(e.TechnicalCharacteristics) || !string.IsNullOrWhiteSpace(e.SoftwareVersion)) { Line("INFORMACIÓN TÉCNICA", true); Line($"Voltaje: {e.Voltage ?? "N/A"} · Frecuencia: {e.Frequency ?? "N/A"} · Potencia: {e.Power ?? "N/A"} · Batería: {e.Battery ?? "N/A"}"); if (!string.IsNullOrWhiteSpace(e.TechnicalCharacteristics)) Line($"Características: {e.TechnicalCharacteristics}"); Line($"Software: {e.SoftwareName ?? "N/A"} {e.SoftwareVersion ?? string.Empty} · Firmware: {e.FirmwareVersion ?? "N/A"}"); }
        Line("MANTENIMIENTOS", true); foreach (var m in e.Maintenances.OrderByDescending(x => x.PerformedAtUtc)) { Line($"{Date(m.PerformedAtUtc)} · {MaintenanceTypeLabel(m.Type)} · {MaintenanceResultLabel(m.Result)} · Realizó: {m.PerformedBy} · Próximo: {Date(m.NextMaintenanceUtc)}"); Line($"Actividades: {m.Activities}"); if (!string.IsNullOrWhiteSpace(m.Findings)) Line($"Hallazgos: {m.Findings}"); if (!string.IsNullOrWhiteSpace(m.TechnicalDiagnosis)) Line($"Diagnóstico: {m.TechnicalDiagnosis}"); if (!string.IsNullOrWhiteSpace(m.ActionsTaken)) Line($"Acciones: {m.ActionsTaken}"); if (!string.IsNullOrWhiteSpace(m.SparePartsSummary)) Line($"Repuestos: {m.SparePartsSummary}"); if (!string.IsNullOrWhiteSpace(m.Notes)) Line($"Pendientes y observaciones: {m.Notes}"); }
        Line("CALIBRACIONES", true); foreach (var c in e.Calibrations.OrderByDescending(x => x.PerformedAtUtc)) { Line($"{Date(c.PerformedAtUtc)} · {c.Laboratory} · Certificado: {c.CertificateNumber ?? "N/A"} · {CalibrationResultLabel(c.Result)} · Próxima: {Date(c.NextCalibrationUtc)}"); if (!string.IsNullOrWhiteSpace(c.Traceability)) Line($"Trazabilidad metrológica: {c.Traceability}"); if (!string.IsNullOrWhiteSpace(c.Notes)) Line($"Observaciones: {c.Notes}"); }
        Line("TECNOVIGILANCIA", true); foreach (var f in e.Failures.OrderByDescending(x => x.ReportedAtUtc)) { Line($"Falla {Date(f.ReportedAtUtc)}: {f.Description} · Reportó: {f.ReportedBy}"); if (!string.IsNullOrWhiteSpace(f.ImmediateAction)) Line($"Acción inmediata: {f.ImmediateAction}"); if (!string.IsNullOrWhiteSpace(f.Diagnosis)) Line($"Diagnóstico: {f.Diagnosis}"); Line($"Solución o pendiente: {f.Solution ?? "Pendiente"}"); } foreach (var i in e.Incidents.OrderByDescending(x => x.OccurredAtUtc)) { Line($"Incidente {Date(i.OccurredAtUtc)}: {i.EventType} · Estado: {IncidentStatusLabel(i.Status)}"); Line($"Descripción: {i.Description}"); if (!string.IsNullOrWhiteSpace(i.ActionsTaken)) Line($"Acciones: {i.ActionsTaken}"); if (!string.IsNullOrWhiteSpace(i.Conclusions)) Line($"Conclusiones: {i.Conclusions}"); }
        Line("HISTORIAL DE ESTADOS", true); foreach (var history in e.StatusHistory.OrderByDescending(x => x.ChangedAtUtc)) Line($"{Date(history.ChangedAtUtc)} · {StatusLabel(history.PreviousStatus)} → {StatusLabel(history.NewStatus)} · Motivo: {history.Reason}");
        if (!string.IsNullOrWhiteSpace(e.PublicOperationalNotes)) { Line("OBSERVACIONES OPERATIVAS", true); Line(e.PublicOperationalNotes); }
        gfx.Dispose(); using var stream = new MemoryStream(); document.Save(stream, false); return stream.ToArray();
    }

    private static string Date(DateTimeOffset? date) => date?.ToLocalTime().ToString("dd/MM/yyyy") ?? "No registrada";
    private static IEnumerable<string> WrapText(string? text, XFont font, XGraphics graphics, double maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text)) return ["No registrado"];
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            var current = string.Empty;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && graphics.MeasureString(candidate, font).Width > maxWidth)
                {
                    lines.Add(current);
                    current = word;
                }
                else current = candidate;
            }
            if (current.Length > 0) lines.Add(current);
        }
        return lines.Count == 0 ? ["No registrado"] : lines;
    }
    private static string StatusLabel(EquipmentStatus value) => value switch { EquipmentStatus.Operational => "Operativo", EquipmentStatus.OperationalWithObservations => "Operativo con observaciones", EquipmentStatus.InMaintenance => "En mantenimiento", EquipmentStatus.MaintenancePending => "Mantenimiento pendiente", EquipmentStatus.OutOfService => "Fuera de servicio", EquipmentStatus.InRepair => "En reparación", EquipmentStatus.AwaitingSparePart => "En espera de repuesto", EquipmentStatus.InCalibration => "En calibración", EquipmentStatus.Quarantine => "En cuarentena", EquipmentStatus.Retired => "Retirado", EquipmentStatus.Decommissioned => "Dado de baja", _ => value.ToString() };
    private static string CriticalityLabel(Criticality value) => value switch { Criticality.Low => "Baja", Criticality.Medium => "Media", Criticality.High => "Alta", Criticality.Critical => "Crítica", _ => value.ToString() };
    private static string MaintenanceTypeLabel(MaintenanceType value) => value switch { MaintenanceType.Preventive => "Preventivo", MaintenanceType.Corrective => "Correctivo", MaintenanceType.Predictive => "Predictivo", MaintenanceType.Inspection => "Inspección", MaintenanceType.Verification => "Verificación", MaintenanceType.Other => "Otro", _ => value.ToString() };
    private static string MaintenanceResultLabel(MaintenanceResult value) => value switch { MaintenanceResult.Successful => "Exitoso", MaintenanceResult.SuccessfulWithObservations => "Exitoso con observaciones", MaintenanceResult.Unsuccessful => "No exitoso", MaintenanceResult.Pending => "Pendiente", _ => value.ToString() };
    private static string CalibrationResultLabel(CalibrationResult value) => value switch { CalibrationResult.Compliant => "Conforme", CalibrationResult.NonCompliant => "No conforme", CalibrationResult.Conditional => "Conforme condicionado", CalibrationResult.Pending => "Pendiente", _ => value.ToString() };
    private static string IncidentStatusLabel(IncidentStatus value) => value switch { IncidentStatus.Open => "Abierto", IncidentStatus.Investigating => "En investigación", IncidentStatus.Reported => "Reportado", IncidentStatus.Closed => "Cerrado", _ => value.ToString() };
}
