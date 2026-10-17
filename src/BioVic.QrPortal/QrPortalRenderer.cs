using System.Net;
using BioVic.Application.DTOs;
using BioVic.Domain.Enums;

namespace BioVic.QrPortal;

public sealed class QrPortalRenderer
{
    public string RenderWebLogin(string antiforgeryToken, string? error = null)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        var errorHtml = string.IsNullOrWhiteSpace(error) ? string.Empty : $"<div class=\"error\">{Encode(error)}</div>";
        return $$"""
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>BioVic · Acceso web seguro</title>
            <style>
              :root{font-family:Inter,"Segoe UI",sans-serif;background:#f3f7f8;color:#15323a}*{box-sizing:border-box}body{margin:0;padding:24px}
              .card{max-width:430px;margin:7vh auto;background:white;border:1px solid #dfeaec;border-radius:20px;padding:28px;box-shadow:0 12px 32px #15323a12}
              .brand{display:flex;gap:12px;align-items:center}.mark{width:44px;height:44px;border-radius:14px;background:#087e8b;color:white;display:grid;place-items:center;font-size:24px;font-weight:800}
              h1{font-size:23px;margin:0}p{color:#64808a;line-height:1.45}label{display:block;font-weight:650;margin:16px 0 6px}input{width:100%;padding:12px;border:1px solid #cadadd;border-radius:10px;font-size:16px}
              button{width:100%;margin-top:20px;padding:12px;border:0;border-radius:10px;background:#087e8b;color:white;font-weight:750;font-size:15px}.error{color:#a52a2a;background:#fff0f0;padding:10px;border-radius:9px}
            </style></head><body><main class="card">
              <div class="brand"><div class="mark">B</div><div><h1>BioVic</h1><div>Acceso web seguro</div></div></div>
              <p>Ingrese con su cuenta de BioVic. La conexión utiliza HTTPS y las funciones disponibles dependerán de su rol y ubicaciones autorizadas.</p>
              {{errorHtml}}
              <form method="post" action="/e/login">
                <input type="hidden" name="__RequestVerificationToken" value="{{Encode(antiforgeryToken)}}">
                <label for="username">Usuario</label><input id="username" name="username" autocomplete="username" maxlength="80" required>
                <label for="password">Contraseña</label><input id="password" name="password" type="password" autocomplete="current-password" maxlength="256" required>
                <button type="submit">Ingresar a BioVic</button>
              </form>
            </main></body></html>
            """;
    }

    public string RenderLogin(Guid publicId, string antiforgeryToken, string? error = null)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        var errorHtml = string.IsNullOrWhiteSpace(error) ? string.Empty : $"<div class=\"error\">{Encode(error)}</div>";
        return $$"""
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>BioVic · Acceso protegido</title>
            <style>
              :root{font-family:Inter,"Segoe UI",sans-serif;background:#f3f7f8;color:#15323a}*{box-sizing:border-box}body{margin:0;padding:24px}
              .card{max-width:430px;margin:7vh auto;background:white;border:1px solid #dfeaec;border-radius:20px;padding:28px;box-shadow:0 12px 32px #15323a12}
              .brand{display:flex;gap:12px;align-items:center}.mark{width:44px;height:44px;border-radius:14px;background:#087e8b;color:white;display:grid;place-items:center;font-size:24px;font-weight:800}
              h1{font-size:23px;margin:0}p{color:#64808a;line-height:1.45}label{display:block;font-weight:650;margin:16px 0 6px}input{width:100%;padding:12px;border:1px solid #cadadd;border-radius:10px;font-size:16px}
              button{width:100%;margin-top:20px;padding:12px;border:0;border-radius:10px;background:#087e8b;color:white;font-weight:750;font-size:15px}.error{color:#a52a2a;background:#fff0f0;padding:10px;border-radius:9px}
            </style></head><body><main class="card">
              <div class="brand"><div class="mark">B</div><div><h1>BioVic</h1><div>Acceso protegido</div></div></div>
              <p>Ingrese su usuario y contraseña. La conexión está protegida con HTTPS y BioVic aplicará los permisos de su rol y de sus ubicaciones autorizadas.</p>
              {{errorHtml}}
              <form method="post" action="/e/{{publicId:D}}/login">
                <input type="hidden" name="__RequestVerificationToken" value="{{Encode(antiforgeryToken)}}">
                <label for="username">Usuario</label><input id="username" name="username" autocomplete="username" maxlength="80" required>
                <label for="password">Contraseña</label><input id="password" name="password" type="password" autocomplete="current-password" maxlength="256" required>
                <button type="submit">Ingresar y ver hoja de vida</button>
              </form>
            </main></body></html>
            """;
    }

    public string Render(PublicEquipmentDto equipment, string antiforgeryToken)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "No disponible" : value);
        static string Date(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(-5)).ToString("dd/MM/yyyy") ?? "No registrada";
        var status = StatusLabel(equipment.Status);
        return $$"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>BioVic · {{Encode(equipment.Name)}}</title>
              <style>
                :root{color-scheme:light;font-family:Inter,"Segoe UI",sans-serif;background:#f3f7f8;color:#15323a}
                *{box-sizing:border-box}body{margin:0;padding:24px}.shell{max-width:760px;margin:auto}
                header{display:flex;align-items:center;gap:12px;margin:8px 0 24px}.mark{width:44px;height:44px;border-radius:14px;background:#087e8b;color:white;display:grid;place-items:center;font-size:24px;font-weight:800}
                h1{margin:0;font-size:24px}.subtitle{color:#64808a}.card{background:#fff;border:1px solid #dfeaec;border-radius:20px;padding:24px;box-shadow:0 12px 32px #15323a12}
                .status{display:inline-flex;padding:8px 12px;border-radius:999px;background:#e6f6f1;color:#12644f;font-weight:700;margin:8px 0 20px}
                dl{display:grid;grid-template-columns:minmax(150px,220px) 1fr;gap:0;margin:0}dt,dd{padding:12px 0;border-bottom:1px solid #edf2f3}dt{font-weight:650;color:#52717a}dd{margin:0}
                .notice{font-size:13px;color:#64808a;margin-top:18px;line-height:1.5}.pdf{display:inline-block;margin-top:18px;background:#087e8b;color:white;text-decoration:none;padding:11px 16px;border-radius:10px;font-weight:700}
                @media(max-width:560px){body{padding:14px}.card{padding:18px}dl{grid-template-columns:1fr}dt{border-bottom:0;padding-bottom:3px}dd{padding-top:0} }
              </style>
            </head>
            <body><main class="shell">
              <header><div class="mark">B</div><div><h1>BioVic</h1><div class="subtitle">Sistema de Gestión de Equipos Médicos y Biomédicos</div></div></header>
              <section class="card">
                <div class="subtitle">HOJA DE VIDA DEL EQUIPO</div><h2>{{Encode(equipment.Name)}}</h2><div class="status">● {{Encode(status)}}</div>
                <dl>
                  <dt>Marca / modelo</dt><dd>{{Encode(equipment.Brand)}} · {{Encode(equipment.Model)}}</dd>
                  <dt>Serial</dt><dd>{{Encode(equipment.SerialNumber)}}</dd>
                  <dt>Código interno</dt><dd>{{Encode(equipment.InternalCode)}}</dd>
                  <dt>Ubicación</dt><dd>{{Encode(equipment.Location)}}</dd>
                  <dt>Último mantenimiento</dt><dd>{{Date(equipment.LastMaintenanceUtc)}}</dd>
                  <dt>Próximo mantenimiento</dt><dd>{{Date(equipment.NextMaintenanceUtc)}}</dd>
                  <dt>Última calibración</dt><dd>{{Date(equipment.LastCalibrationUtc)}}</dd>
                  <dt>Servicio responsable</dt><dd>{{Encode(equipment.ResponsibleService)}}</dd>
                  <dt>Observaciones operativas</dt><dd>{{Encode(equipment.PublicOperationalNotes)}}</dd>
                </dl>
                <a class="pdf" href="/e/{{equipment.PublicId:D}}/hoja-de-vida.pdf">Descargar hoja de vida en PDF</a>
                <form method="post" action="/e/logout"><input type="hidden" name="__RequestVerificationToken" value="{{Encode(antiforgeryToken)}}"><button class="pdf" type="submit">Cerrar sesión</button></form>
                <p class="notice">Información protegida y de solo lectura. El acceso y la descarga quedan registrados en auditoría.</p>
              </section>
            </main></body></html>
            """;
    }

    private static string StatusLabel(EquipmentStatus status) => status switch
    {
        EquipmentStatus.Operational => "Operativo",
        EquipmentStatus.OperationalWithObservations => "Operativo con observaciones",
        EquipmentStatus.InMaintenance => "En mantenimiento",
        EquipmentStatus.MaintenancePending => "Pendiente de mantenimiento",
        EquipmentStatus.OutOfService => "Fuera de servicio",
        EquipmentStatus.InRepair => "En reparación",
        EquipmentStatus.AwaitingSparePart => "Pendiente de repuesto",
        EquipmentStatus.InCalibration => "En calibración",
        EquipmentStatus.Quarantine => "En cuarentena",
        EquipmentStatus.Retired => "Retirado",
        EquipmentStatus.Decommissioned => "Dado de baja",
        _ => status.ToString()
    };
}
