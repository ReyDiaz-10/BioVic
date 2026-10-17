using System.Net;
using System.Text;
using BioVic.Application.DTOs;
using BioVic.Application.Services;
using BioVic.Domain.Enums;

namespace BioVic.QrPortal;

public sealed class MobilePortalRenderer
{
    private const string Styles = """
        :root{color-scheme:light;font-family:Inter,"Segoe UI",sans-serif;background:#f3f7f8;color:#15323a}
        *{box-sizing:border-box}body{margin:0;padding:16px}.shell{max-width:900px;margin:auto}
        header{display:flex;align-items:center;gap:12px;margin:8px 0 20px}.mark{width:44px;height:44px;border-radius:14px;background:#087e8b;color:#fff;display:grid;place-items:center;font-size:24px;font-weight:800}
        h1{margin:0;font-size:24px}h2{margin:8px 0 14px}.subtitle,.muted{color:#64808a}.card{background:#fff;border:1px solid #dfeaec;border-radius:18px;padding:20px;margin:14px 0;box-shadow:0 9px 24px #15323a0d}
        .status,.role{display:inline-flex;padding:7px 11px;border-radius:999px;background:#e6f6f1;color:#12644f;font-weight:700;margin:4px 6px 12px 0}
        .grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}.actions{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}
        dl{display:grid;grid-template-columns:minmax(150px,220px) 1fr;gap:0;margin:0}dt,dd{padding:10px 0;border-bottom:1px solid #edf2f3}dt{font-weight:650;color:#52717a}dd{margin:0}
        label{display:block;font-weight:650;margin:12px 0 5px}input,select,textarea{width:100%;padding:11px;border:1px solid #cadadd;border-radius:10px;font:inherit;background:#fff}textarea{min-height:95px;resize:vertical}
        .check{display:flex;gap:9px;align-items:flex-start;margin:9px 0}.check input{width:auto;margin-top:4px}.check label{margin:0;font-weight:500}
        .button,button{display:inline-block;width:auto;border:0;border-radius:10px;background:#087e8b;color:#fff;text-decoration:none;padding:11px 15px;font-weight:750;font-size:14px;cursor:pointer}.button.secondary,button.secondary{background:#e7f0f2;color:#15323a}.button.danger{background:#7a2b2b}
        .notice,.success,.error{padding:11px 13px;border-radius:10px;margin:12px 0;line-height:1.45}.notice{background:#edf5f7;color:#315762}.success{background:#e6f6f1;color:#12644f}.error{background:#fff0f0;color:#9b2727}
        .record{padding:12px 0;border-bottom:1px solid #edf2f3}.record:last-child{border-bottom:0}.nav{display:flex;flex-wrap:wrap;gap:9px;margin:12px 0}.inline{display:flex;gap:9px;align-items:end}.inline>div{flex:1}
        @media(max-width:620px){body{padding:10px}.card{padding:16px}.grid,.actions{grid-template-columns:1fr}dl{grid-template-columns:1fr}dt{border-bottom:0;padding-bottom:2px}dd{padding-top:0}.inline{display:block}.inline .button{margin-top:8px;width:100%;text-align:center}.button,button{width:100%;text-align:center}}
        """;

    public string RenderEquipment(EquipmentDetailsDto equipment, IReadOnlyList<EquipmentDocumentDto> documents,
        UserRole role, string antiforgeryToken,
        string? message = null, string? error = null)
    {
        var content = new StringBuilder();
        content.Append(RoleBar(role));
        content.Append(Message(message, error));
        content.Append("<section class=\"card\"><div class=\"subtitle\">HOJA DE VIDA DEL EQUIPO</div>")
            .Append("<h2>").Append(E(equipment.Name)).Append("</h2>")
            .Append("<span class=\"status\">● ").Append(E(StatusLabel(equipment.Status))).Append("</span><dl>")
            .Append(Row("Código interno", equipment.InternalCode))
            .Append(Row("Tipo / categoría", Join(equipment.EquipmentType, equipment.Category)))
            .Append(Row("Descripción", equipment.Description))
            .Append(Row("Fabricante", equipment.Manufacturer))
            .Append(Row("Marca / modelo", Join(equipment.Brand, equipment.Model)))
            .Append(Row("Serial", equipment.SerialNumber))
            .Append(Row("Inventario / activo", Join(equipment.InventoryNumber, equipment.AssetCode)))
            .Append(Row("Ubicación", equipment.Location))
            .Append(Row("Responsable", Join(equipment.ResponsibleName, equipment.ResponsiblePosition)))
            .Append(Row("Criticidad", CriticalityLabel(equipment.Criticality)))
            .Append(Row("Próximo mantenimiento", Date(equipment.NextMaintenanceUtc)))
            .Append(Row("Próxima calibración", Date(equipment.NextCalibrationUtc)))
            .Append(Row("Observaciones operativas", equipment.PublicOperationalNotes))
            .Append("</dl><div class=\"nav\">")
            .Append(Link($"/e/{equipment.PublicId:D}/hoja-de-vida.pdf", "Descargar PDF"))
            .Append(Link("/e/mobile", "Buscar otros equipos", true));

        if (MobileAccessRules.CanAddOperationalRecords(role))
        {
            content.Append(Link($"/e/{equipment.PublicId:D}/maintenance/new", "Agregar mantenimiento"))
                .Append(Link($"/e/{equipment.PublicId:D}/calibration/new", "Agregar calibración"))
                .Append(Link($"/e/{equipment.PublicId:D}/failure/new", "Registrar falla"))
                .Append(Link($"/e/{equipment.PublicId:D}/incident/new", "Registrar incidente"));
        }
        if (MobileAccessRules.CanAdminister(role))
            content.Append(Link($"/e/{equipment.PublicId:D}/edit", "Editar ficha del equipo"));

        content.Append("</div>")
            .Append("<p class=\"notice\">Toda consulta, descarga o modificación queda registrada en la auditoría. Desde el celular no existe ninguna opción para borrar datos.</p>")
            .Append(LogoutForm($"/e/{equipment.PublicId:D}/logout", antiforgeryToken))
            .Append("</section>");
        content.Append(RenderDocuments(equipment, documents));
        content.Append(RenderHistory(equipment));
        return Page($"BioVic · {equipment.Name}", content.ToString());
    }

    private static string RenderDocuments(EquipmentDetailsDto equipment, IReadOnlyList<EquipmentDocumentDto> documents)
    {
        var active = documents.Where(item => !item.IsDeleted && item.IsCurrent).ToList();
        var body = new StringBuilder("<section class=\"card\"><h2>Documentos y certificados</h2>");
        if (active.Count == 0)
            return body.Append("<p class=\"muted\">Este equipo todavía no tiene documentos publicados.</p></section>").ToString();
        foreach (var document in active)
        {
            body.Append("<div class=\"record\"><strong>").Append(E(document.OriginalName))
                .Append("</strong><div class=\"muted\">").Append(E(document.DocumentType)).Append(" · versión ")
                .Append(document.Version).Append(document.ExpiresAtUtc is null ? string.Empty : " · vence " + E(Date(document.ExpiresAtUtc)))
                .Append("</div><div class=\"nav\">")
                .Append(Link($"/e/{equipment.PublicId:D}/documents/{document.Id:D}", "Descargar documento"))
                .Append("</div></div>");
        }
        return body.Append("<p class=\"notice\">Los documentos son de solo lectura desde el celular.</p></section>").ToString();
    }

    public string RenderDashboard(PagedResult<EquipmentSearchItemDto> result, UserRole role, string antiforgeryToken,
        string? query, string? message = null, string? error = null)
    {
        var content = new StringBuilder(RoleBar(role));
        content.Append(Message(message, error));
        content.Append("<section class=\"card\"><h2>Panel móvil</h2><p class=\"muted\">Consulte solamente los equipos de las ubicaciones autorizadas.</p>")
            .Append("<form method=\"get\" action=\"/e/mobile\"><div class=\"inline\"><div><label for=\"q\">Buscar por nombre, código, serial, modelo o ubicación</label>")
            .Append("<input id=\"q\" name=\"q\" value=\"").Append(E(query)).Append("\" maxlength=\"120\"></div><button type=\"submit\">Buscar</button></div></form>");
        if (MobileAccessRules.CanAdminister(role))
        {
            content.Append("<div class=\"nav\">")
                .Append(Link("/e/mobile/equipment/new", "Crear equipo"))
                .Append(Link("/e/mobile/users/new", "Crear usuario"))
                .Append(Link("/e/mobile/locations/new", "Crear ubicación"))
                .Append("</div>");
        }
        content.Append("</section><section class=\"card\"><h2>Equipos (" + result.TotalCount + ")</h2>");
        if (result.Items.Count == 0) content.Append("<p class=\"muted\">No se encontraron equipos.</p>");
        foreach (var item in result.Items)
        {
            content.Append("<div class=\"record\"><strong>").Append(E(item.Name)).Append("</strong><div class=\"muted\">")
                .Append(E(item.InternalCode)).Append(" · ").Append(E(item.Location)).Append(" · ").Append(E(StatusLabel(item.Status)))
                .Append("</div><div class=\"nav\">").Append(Link($"/e/{item.PublicId:D}", "Abrir ficha")).Append("</div></div>");
        }
        content.Append(LogoutForm("/e/mobile/logout", antiforgeryToken)).Append("</section>");
        return Page("BioVic · Panel móvil", content.ToString());
    }

    public string RenderMaintenanceForm(EquipmentDetailsDto equipment, UserRole role, string token, string? error = null) =>
        RecordPage(equipment, role, "Agregar mantenimiento", "maintenance", token, error, MaintenanceFields());

    public string RenderCalibrationForm(EquipmentDetailsDto equipment, UserRole role, string token, string? error = null) =>
        RecordPage(equipment, role, "Agregar calibración", "calibration", token, error, CalibrationFields());

    public string RenderFailureForm(EquipmentDetailsDto equipment, UserRole role, string token, string? error = null) =>
        RecordPage(equipment, role, "Registrar falla", "failure", token, error, FailureFields());

    public string RenderIncidentForm(EquipmentDetailsDto equipment, UserRole role, string token, string? error = null) =>
        RecordPage(equipment, role, "Registrar incidente", "incident", token, error, IncidentFields());

    public string RenderEquipmentForm(EquipmentDetailsDto? equipment, IReadOnlyList<LocationDto> locations,
        string token, string? error = null)
    {
        var isEdit = equipment is not null;
        var action = isEdit ? $"/e/{equipment!.PublicId:D}/edit" : "/e/mobile/equipment/new";
        var content = new StringBuilder(RoleBar(UserRole.MasterAdministrator));
        content.Append(Message(null, error)).Append("<section class=\"card\"><h2>")
            .Append(isEdit ? "Editar equipo" : "Crear equipo").Append("</h2>")
            .Append("<p class=\"notice\">Esta operación queda auditada. La eliminación de equipos no está disponible desde el celular.</p>")
            .Append("<form method=\"post\" action=\"").Append(action).Append("\">").Append(Token(token))
            .Append(Input("internalCode", "Código interno", equipment?.InternalCode, required: true, max: 80))
            .Append(Input("name", "Nombre del equipo", equipment?.Name, required: true, max: 180))
            .Append(Input("equipmentType", "Tipo de equipo", equipment?.EquipmentType, max: 120))
            .Append(Input("category", "Categoría", equipment?.Category, max: 120))
            .Append(Textarea("description", "Descripción", equipment?.Description))
            .Append(Input("manufacturer", "Fabricante", equipment?.Manufacturer, max: 140))
            .Append(Input("brand", "Marca", equipment?.Brand, max: 120))
            .Append(Input("model", "Modelo", equipment?.Model, max: 120))
            .Append(Input("serialNumber", "Serial", equipment?.SerialNumber, max: 120))
            .Append(Input("inventoryNumber", "Número de inventario", equipment?.InventoryNumber, max: 120))
            .Append(Input("assetCode", "Código de activo", equipment?.AssetCode, max: 120))
            .Append(SelectLocations("locationId", "Ubicación", locations, equipment?.LocationId, true))
            .Append(Input("responsibleName", "Responsable", equipment?.ResponsibleName, max: 160))
            .Append(Input("responsiblePosition", "Cargo del responsable", equipment?.ResponsiblePosition, max: 140))
            .Append(SelectEnum("status", "Estado", equipment?.Status ?? EquipmentStatus.Operational, StatusLabel))
            .Append(SelectEnum("criticality", "Criticidad", equipment?.Criticality ?? Criticality.Medium, CriticalityLabel))
            .Append(Check("requiresCalibration", "Requiere calibración", equipment?.RequiresCalibration ?? false))
            .Append(Input("nextMaintenance", "Próximo mantenimiento", DateInput(equipment?.NextMaintenanceUtc), "datetime-local"))
            .Append(Input("nextCalibration", "Próxima calibración", DateInput(equipment?.NextCalibrationUtc), "datetime-local"))
            .Append(Textarea("publicOperationalNotes", "Observaciones operativas", equipment?.PublicOperationalNotes));
        if (isEdit) content.Append(Textarea("changeReason", "Motivo de la modificación", null, true));
        content.Append("<div class=\"nav\"><button type=\"submit\">Guardar</button>")
            .Append(Link(isEdit ? $"/e/{equipment!.PublicId:D}" : "/e/mobile", "Cancelar", true))
            .Append("</div></form></section>");
        return Page(isEdit ? "BioVic · Editar equipo" : "BioVic · Crear equipo", content.ToString());
    }

    public string RenderUserForm(IReadOnlyList<LocationDto> locations, string token, string? error = null)
    {
        var content = new StringBuilder(RoleBar(UserRole.MasterAdministrator));
        content.Append(Message(null, error)).Append("<section class=\"card\"><h2>Crear usuario</h2>")
            .Append("<p class=\"notice\">Desde el celular se pueden crear Consultores y Editores. BioVic mantiene un solo Administrador Maestro.</p>")
            .Append("<form method=\"post\" action=\"/e/mobile/users/new\">").Append(Token(token))
            .Append(Input("firstName", "Nombres", null, required: true, max: 100))
            .Append(Input("lastName", "Apellidos", null, required: true, max: 100))
            .Append(Input("username", "Usuario", null, required: true, max: 80, autocomplete: "username"))
            .Append(Input("password", "Contraseña temporal", null, "password", true, 256, "new-password"))
            .Append(Input("email", "Correo (opcional)", null, "email", false, 180))
            .Append("<label for=\"role\">Rol</label><select id=\"role\" name=\"role\"><option value=\"Searcher\">Consultor</option><option value=\"Editor\">Editor</option></select>")
            .Append(Check("hasAllLocations", "Puede acceder a todas las ubicaciones", false))
            .Append("<fieldset><legend>Ubicaciones autorizadas</legend>");
        foreach (var location in locations)
            content.Append(Check("locationIds", location.DisplayName, false, location.Id.ToString("D")));
        content.Append("</fieldset><div class=\"nav\"><button type=\"submit\">Crear usuario</button>")
            .Append(Link("/e/mobile", "Cancelar", true)).Append("</div></form></section>");
        return Page("BioVic · Crear usuario", content.ToString());
    }

    public string RenderLocationForm(string token, string? error = null)
    {
        var content = new StringBuilder(RoleBar(UserRole.MasterAdministrator));
        content.Append(Message(null, error)).Append("<section class=\"card\"><h2>Crear ubicación</h2>")
            .Append("<form method=\"post\" action=\"/e/mobile/locations/new\">").Append(Token(token))
            .Append(Input("institution", "Institución", null, required: true, max: 180))
            .Append(Input("site", "Sede", null, required: true, max: 160))
            .Append(Input("building", "Edificio", null, max: 120))
            .Append(Input("floor", "Piso", null, max: 60))
            .Append(Input("service", "Servicio", null, required: true, max: 160))
            .Append(Input("area", "Área", null, max: 140))
            .Append(Input("room", "Sala / habitación", null, max: 100))
            .Append(Input("specificLocation", "Ubicación específica", null, max: 180))
            .Append("<div class=\"nav\"><button type=\"submit\">Crear ubicación</button>")
            .Append(Link("/e/mobile", "Cancelar", true)).Append("</div></form></section>");
        return Page("BioVic · Crear ubicación", content.ToString());
    }

    public string RenderLoggedOut() => Page("BioVic · Sesión cerrada",
        "<section class=\"card\"><h2>Sesión cerrada</h2><p>La sesión móvil se cerró correctamente. Escanee nuevamente un código QR de BioVic para volver a ingresar.</p></section>");

    private static string RecordPage(EquipmentDetailsDto equipment, UserRole role, string title, string route, string token,
        string? error, string fields)
    {
        var body = RoleBar(role) + Message(null, error)
            + "<section class=\"card\"><h2>" + E(title) + "</h2><p class=\"muted\">" + E(equipment.Name) + " · " + E(equipment.InternalCode) + "</p>"
            + "<form method=\"post\" action=\"/e/" + equipment.PublicId.ToString("D") + "/" + route + "\">" + Token(token) + fields
            + "<div class=\"nav\"><button type=\"submit\">Guardar registro</button>" + Link($"/e/{equipment.PublicId:D}", "Cancelar", true) + "</div></form></section>";
        return Page("BioVic · " + title, body);
    }

    private static string MaintenanceFields() =>
        SelectEnum("type", "Tipo", MaintenanceType.Preventive, MaintenanceTypeLabel)
        + Input("performedAt", "Fecha y hora realizada", DateInput(DateTimeOffset.UtcNow), "datetime-local", true)
        + Input("performedBy", "Realizado por", null, required: true, max: 160)
        + Input("responsible", "Responsable", null, max: 160)
        + Input("workOrderNumber", "Orden de trabajo", null, max: 100)
        + Textarea("activities", "Actividades realizadas", null, true)
        + Textarea("findings", "Hallazgos", null)
        + Textarea("technicalDiagnosis", "Diagnóstico técnico", null)
        + Textarea("actionsTaken", "Acciones tomadas", null)
        + Textarea("sparePartsSummary", "Repuestos", null)
        + Input("cost", "Costo", null, "number")
        + Input("downtimeMinutes", "Minutos fuera de servicio", null, "number")
        + SelectEnum("result", "Resultado", MaintenanceResult.Successful, MaintenanceResultLabel)
        + SelectNullableStatus("resultingStatus", "Estado resultante")
        + Input("nextMaintenance", "Próximo mantenimiento", null, "datetime-local")
        + Textarea("notes", "Notas", null);

    private static string CalibrationFields() =>
        Input("performedAt", "Fecha y hora realizada", DateInput(DateTimeOffset.UtcNow), "datetime-local", true)
        + Input("laboratory", "Laboratorio o profesional", null, required: true, max: 180)
        + Input("certificateNumber", "Número de certificado", null, max: 120)
        + SelectEnum("result", "Resultado", CalibrationResult.Compliant, CalibrationResultLabel)
        + Textarea("traceability", "Trazabilidad", null)
        + Input("nextCalibration", "Próxima calibración", null, "datetime-local")
        + Textarea("notes", "Notas", null);

    private static string FailureFields() =>
        Input("reportedAt", "Fecha y hora del reporte", DateInput(DateTimeOffset.UtcNow), "datetime-local", true)
        + Input("reportedBy", "Reportado por", null, required: true, max: 160)
        + Input("service", "Servicio", null, max: 160)
        + Textarea("description", "Descripción de la falla", null, true)
        + Textarea("impact", "Impacto", null)
        + Check("equipmentStopped", "El equipo quedó detenido", false)
        + Textarea("immediateAction", "Acción inmediata", null)
        + Textarea("diagnosis", "Diagnóstico", null)
        + Textarea("solution", "Solución", null)
        + Input("downtimeMinutes", "Minutos fuera de servicio", null, "number")
        + Input("closedAt", "Fecha de cierre", null, "datetime-local");

    private static string IncidentFields() =>
        Input("occurredAt", "Fecha y hora del incidente", DateInput(DateTimeOffset.UtcNow), "datetime-local", true)
        + Input("eventType", "Tipo de evento", null, required: true, max: 160)
        + Input("reportedBy", "Reportado por", null, required: true, max: 160)
        + Textarea("description", "Descripción", null, true)
        + Textarea("consequence", "Consecuencia", null)
        + SelectEnum("status", "Estado", IncidentStatus.Open, IncidentStatusLabel)
        + Textarea("actionsTaken", "Acciones tomadas", null)
        + Check("wasReportedExternally", "Fue reportado externamente", false)
        + Input("externalReportNumber", "Número de reporte externo", null, max: 120)
        + Input("externalReportDate", "Fecha de reporte externo", null, "datetime-local")
        + Input("reportedEntity", "Entidad reportada", null, max: 180)
        + Textarea("conclusions", "Conclusiones", null);

    private static string RenderHistory(EquipmentDetailsDto equipment)
    {
        var body = new StringBuilder("<section class=\"card\"><h2>Historial resumido</h2><div class=\"grid\">")
            .Append(CountCard("Mantenimientos", equipment.Maintenances.Count))
            .Append(CountCard("Calibraciones", equipment.Calibrations.Count))
            .Append(CountCard("Fallas", equipment.Failures.Count))
            .Append(CountCard("Incidentes", equipment.Incidents.Count))
            .Append("</div>");
        foreach (var maintenance in equipment.Maintenances.Take(3))
            body.Append("<div class=\"record\"><strong>Mantenimiento · ").Append(E(Date(maintenance.PerformedAtUtc))).Append("</strong><div>").Append(E(maintenance.Activities)).Append("</div></div>");
        foreach (var calibration in equipment.Calibrations.Take(2))
            body.Append("<div class=\"record\"><strong>Calibración · ").Append(E(Date(calibration.PerformedAtUtc))).Append("</strong><div>").Append(E(calibration.Laboratory)).Append("</div></div>");
        foreach (var failure in equipment.Failures.Take(2))
            body.Append("<div class=\"record\"><strong>Falla · ").Append(E(Date(failure.ReportedAtUtc))).Append("</strong><div>").Append(E(failure.Description)).Append("</div></div>");
        foreach (var incident in equipment.Incidents.Take(2))
            body.Append("<div class=\"record\"><strong>Incidente · ").Append(E(Date(incident.OccurredAtUtc))).Append("</strong><div>").Append(E(incident.Description)).Append("</div></div>");
        return body.Append("</section>").ToString();
    }

    private static string Page(string title, string content) => "<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>"
        + E(title) + "</title><style>" + Styles + "</style></head><body><main class=\"shell\"><header><div class=\"mark\">B</div><div><h1>BioVic</h1><div class=\"subtitle\">Acceso móvil seguro</div></div></header>" + content + "</main></body></html>";

    private static string RoleBar(UserRole role) => "<div><span class=\"role\">" + E(RoleLabel(role)) + "</span><span class=\"muted\">Permisos aplicados por usuario y ubicación</span></div>";
    private static string Message(string? message, string? error) =>
        (string.IsNullOrWhiteSpace(message) ? string.Empty : "<div class=\"success\">" + E(message) + "</div>")
        + (string.IsNullOrWhiteSpace(error) ? string.Empty : "<div class=\"error\">" + E(error) + "</div>");
    private static string Row(string label, string? value) => "<dt>" + E(label) + "</dt><dd>" + E(Available(value)) + "</dd>";
    private static string CountCard(string label, int count) => "<div class=\"notice\"><strong>" + count + "</strong><br>" + E(label) + "</div>";
    private static string Link(string href, string label, bool secondary = false) => "<a class=\"button" + (secondary ? " secondary" : string.Empty) + "\" href=\"" + E(href) + "\">" + E(label) + "</a>";
    private static string LogoutForm(string action, string token) => "<form method=\"post\" action=\"" + E(action) + "\">" + Token(token) + "<button class=\"secondary\" type=\"submit\">Cerrar sesión</button></form>";
    private static string Token(string token) => "<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"" + E(token) + "\">";

    private static string Input(string name, string label, string? value, string type = "text", bool required = false,
        int max = 0, string? autocomplete = null) => "<label for=\"" + E(name) + "\">" + E(label) + (required ? " *" : string.Empty) + "</label><input id=\"" + E(name) + "\" name=\"" + E(name) + "\" type=\"" + E(type) + "\" value=\"" + E(value) + "\""
        + (required ? " required" : string.Empty) + (max > 0 ? " maxlength=\"" + max + "\"" : string.Empty)
        + (autocomplete is null ? string.Empty : " autocomplete=\"" + E(autocomplete) + "\"") + (type == "number" ? " min=\"0\" step=\"any\"" : string.Empty) + ">";
    private static string Textarea(string name, string label, string? value, bool required = false) => "<label for=\"" + E(name) + "\">" + E(label) + (required ? " *" : string.Empty) + "</label><textarea id=\"" + E(name) + "\" name=\"" + E(name) + "\" maxlength=\"4000\"" + (required ? " required" : string.Empty) + ">" + E(value) + "</textarea>";
    private static string Check(string name, string label, bool selected, string value = "true") => "<div class=\"check\"><input id=\"" + E(name + value) + "\" name=\"" + E(name) + "\" type=\"checkbox\" value=\"" + E(value) + "\"" + (selected ? " checked" : string.Empty) + "><label for=\"" + E(name + value) + "\">" + E(label) + "</label></div>";

    private static string SelectLocations(string name, string label, IReadOnlyList<LocationDto> locations, Guid? selected, bool required)
    {
        var html = new StringBuilder("<label for=\"").Append(E(name)).Append("\">").Append(E(label)).Append(required ? " *" : string.Empty).Append("</label><select id=\"").Append(E(name)).Append("\" name=\"").Append(E(name)).Append("\"").Append(required ? " required" : string.Empty).Append("><option value=\"\">Seleccione…</option>");
        foreach (var location in locations)
            html.Append("<option value=\"").Append(location.Id.ToString("D")).Append("\"").Append(location.Id == selected ? " selected" : string.Empty).Append(">").Append(E(location.DisplayName)).Append("</option>");
        return html.Append("</select>").ToString();
    }

    private static string SelectEnum<TEnum>(string name, string label, TEnum selected, Func<TEnum, string> labeler) where TEnum : struct, Enum
    {
        var html = new StringBuilder("<label for=\"").Append(E(name)).Append("\">").Append(E(label)).Append("</label><select id=\"").Append(E(name)).Append("\" name=\"").Append(E(name)).Append("\">");
        foreach (var value in Enum.GetValues<TEnum>())
            html.Append("<option value=\"").Append(value).Append("\"").Append(EqualityComparer<TEnum>.Default.Equals(value, selected) ? " selected" : string.Empty).Append(">").Append(E(labeler(value))).Append("</option>");
        return html.Append("</select>").ToString();
    }

    private static string SelectNullableStatus(string name, string label)
    {
        var html = new StringBuilder("<label for=\"").Append(E(name)).Append("\">").Append(E(label)).Append("</label><select id=\"").Append(E(name)).Append("\" name=\"").Append(E(name)).Append("\"><option value=\"\">No cambiar</option>");
        foreach (var value in Enum.GetValues<EquipmentStatus>())
            html.Append("<option value=\"").Append(value).Append("\">").Append(E(StatusLabel(value))).Append("</option>");
        return html.Append("</select>").ToString();
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string Available(string? value) => string.IsNullOrWhiteSpace(value) ? "No disponible" : value;
    private static string Join(string? first, string? second) => string.Join(" · ", new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private static string Date(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(-5)).ToString("dd/MM/yyyy HH:mm") ?? "No registrada";
    private static string DateInput(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(-5)).ToString("yyyy-MM-ddTHH:mm") ?? string.Empty;

    private static string RoleLabel(UserRole role) => role switch { UserRole.Searcher => "Consultor", UserRole.Editor => "Editor", UserRole.MasterAdministrator => "Administrador Maestro", _ => role.ToString() };
    private static string CriticalityLabel(Criticality value) => value switch { Criticality.Low => "Baja", Criticality.Medium => "Media", Criticality.High => "Alta", Criticality.Critical => "Crítica", _ => value.ToString() };
    private static string MaintenanceTypeLabel(MaintenanceType value) => value switch { MaintenanceType.Preventive => "Preventivo", MaintenanceType.Corrective => "Correctivo", MaintenanceType.Predictive => "Predictivo", MaintenanceType.Inspection => "Inspección", MaintenanceType.Verification => "Verificación", MaintenanceType.Other => "Otro", _ => value.ToString() };
    private static string MaintenanceResultLabel(MaintenanceResult value) => value switch { MaintenanceResult.Successful => "Exitoso", MaintenanceResult.SuccessfulWithObservations => "Exitoso con observaciones", MaintenanceResult.Unsuccessful => "No exitoso", MaintenanceResult.Pending => "Pendiente", _ => value.ToString() };
    private static string CalibrationResultLabel(CalibrationResult value) => value switch { CalibrationResult.Compliant => "Conforme", CalibrationResult.NonCompliant => "No conforme", CalibrationResult.Conditional => "Condicional", CalibrationResult.Pending => "Pendiente", _ => value.ToString() };
    private static string IncidentStatusLabel(IncidentStatus value) => value switch { IncidentStatus.Open => "Abierto", IncidentStatus.Investigating => "En investigación", IncidentStatus.Reported => "Reportado", IncidentStatus.Closed => "Cerrado", _ => value.ToString() };
    private static string StatusLabel(EquipmentStatus status) => status switch { EquipmentStatus.Operational => "Operativo", EquipmentStatus.OperationalWithObservations => "Operativo con observaciones", EquipmentStatus.InMaintenance => "En mantenimiento", EquipmentStatus.MaintenancePending => "Pendiente de mantenimiento", EquipmentStatus.OutOfService => "Fuera de servicio", EquipmentStatus.InRepair => "En reparación", EquipmentStatus.AwaitingSparePart => "Pendiente de repuesto", EquipmentStatus.InCalibration => "En calibración", EquipmentStatus.Quarantine => "En cuarentena", EquipmentStatus.Retired => "Retirado", EquipmentStatus.Decommissioned => "Dado de baja", _ => status.ToString() };
}
