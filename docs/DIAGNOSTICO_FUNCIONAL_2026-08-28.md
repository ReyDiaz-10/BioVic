# Diagnóstico funcional de BioVic

Fecha de revisión: 28 de agosto de 2026

## Alcance

Esta revisión cubre el código fuente disponible, los resultados de compilación y pruebas comunicados por el propietario, y el incremento de portal web/móvil protegido. La validación definitiva en Azure exige compilar, publicar y ejecutar la prueba de aceptación incluida en este paquete.

## Dictamen

BioVic está en estado **funcional para un piloto controlado**, condicionado a que el incremento compile, se publique y complete la prueba manual por roles. Todavía no debe declararse producto terminado ni operación crítica sin supervisión porque faltan pruebas de integración extremo a extremo y varias funciones documentales.

## Matriz de capacidades

| Área | Estado | Evidencia o límite |
| --- | --- | --- |
| Inventario y búsqueda | Funcional | Alta, consulta, modificación, búsqueda tolerante y ubicación del equipo. |
| Inicio de sesión | Funcional | Contraseñas con hash ASP.NET Identity; sesiones aleatorias almacenadas como SHA-256; bloqueo por intentos fallidos y vencimiento. |
| Acceso QR y web | Implementado en este incremento | HTTPS obligatorio en producción, cookies `Secure`, `HttpOnly` y `SameSite=Strict`, antifalsificación, límites de solicitudes y cabeceras de seguridad. |
| Consultor móvil | Implementado | Consulta ficha, historial resumido y descarga PDF; sólo ve ubicaciones autorizadas. |
| Editor móvil | Implementado | Lo anterior más mantenimiento, calibración, falla e incidente. No puede editar ficha maestra, crear usuarios/ubicaciones ni borrar. |
| Administrador Maestro móvil | Implementado | Lo anterior más crear equipos, usuarios y ubicaciones y editar ficha, estado, ubicación, responsable y datos del equipo. No puede borrar desde móvil. |
| Cierre de sesión QR | Implementado | Revoca la sesión, elimina la cookie y vuelve a la pantalla de inicio de sesión del mismo QR. |
| Permisos por ubicación | Corregido | Consultas, ficha pública autenticada, PDF, lista de ubicaciones y creación de equipo validan el alcance del usuario. |
| Auditoría | Funcional con límite | Registra consultas, descargas, altas y cambios con cadena hash. El bloqueo de escritura de auditoría es local al proceso; antes de escalar a varias instancias debe usarse coordinación de base de datos. |
| Historial del responsable | Corregido | Al cambiar nombre o cargo se crea el registro histórico correspondiente. |
| Mantenimiento, calibración y tecnovigilancia | Funcional | Altas auditadas; los registros son acumulativos. No existe todavía un flujo formal de corrección/anulación de registros erróneos. |
| PDF y QR | Funcional | Generación de QR protegido y hoja de vida PDF autenticada. |
| Correo electrónico | Retirado por decisión del propietario | No forma parte del alcance funcional. BioVic no depende de SMTP ni de contraseñas de aplicaciones de Google. |
| Base de datos Azure SQL | Funcional según despliegue comunicado | Conexión exige cifrado y rechaza `TrustServerCertificate=True`; el proyecto real conserva sus migraciones SQL Server. |
| Instalador Windows | Funcional según compilación comunicada | Autocontenido, sin necesidad de .NET en el equipo usuario. No se verificó firma digital del instalador. |
| Actualización automática | Pendiente | Es necesario volver a compilar y distribuir un instalador versionado. No hay servicio de actualización automática ni firma de paquetes. |
| Documentos del equipo | Parcial | Existen estructuras de datos, pero no un flujo completo de carga, versión, consulta y descarga para manuales, certificados, fotos y soportes. |
| Restauración de eliminados | Pendiente | Las eliminaciones de escritorio son lógicas, pero no existe una acción de restauración administrada. |
| IA | Parcial | Puede responder con reglas/proveedor configurado; no reemplaza validación técnica y no incluye todavía búsqueda documental completa. |

## Seguridad aplicada al portal web y QR

1. En producción BioVic no inicia si `BIOVIC_REQUIRE_HTTPS` no es `true` o si la URL QR no es HTTPS.
2. La contraseña nunca se guarda ni se transmite como texto visible: el navegador la envía dentro del canal TLS y el servidor conserva solamente un hash con sal.
3. La sesión usa un token aleatorio; en la base de datos sólo se almacena su hash SHA-256.
4. La cookie móvil es `Secure`, `HttpOnly`, `SameSite=Strict`, tiene ruta `/e` y fecha de vencimiento.
5. Todo formulario de escritura y el cierre de sesión validan un token antifalsificación.
6. El servidor limita intentos de inicio de sesión, lecturas y escrituras; también bloquea temporalmente cuentas tras fallos repetidos.
7. La autorización se verifica en el servidor por rol y ubicación. Cambiar el HTML o fabricar una URL no concede permisos.
8. El portal envía HSTS, CSP, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, política de referencia restrictiva y evita caché e indexación.

La expresión correcta es **hash de contraseña**, no “cifrado reversible de contraseña”. Una contraseña cifrada que pudiera descifrarse sería menos segura.

## Riesgos que aún impiden llamarlo “terminado”

- Falta ejecutar pruebas de integración contra Azure SQL y navegación real en teléfonos Android/iOS.
- Los 20 tests existentes son principalmente unitarios; no demuestran por sí solos que cookies, formularios, proxy Azure y autorización funcionen juntos.
- El instalador no tiene actualización automática ni firma de editor verificada.
- Falta implementar el ciclo documental completo y la restauración de eliminaciones lógicas.
- Antes de usar más de una instancia de App Service debe reforzarse la serialización de la cadena de auditoría.

## Criterio de aprobación

El incremento se aprueba únicamente cuando:

1. `dotnet build` y `dotnet test` terminan sin errores.
2. El despliegue Kudu llega a estado 4 y `/health` devuelve 200.
3. Se ejecuta la matriz de `PRUEBA_ACEPTACION_MOVIL.md` con tres cuentas separadas.
4. Se confirma en auditoría cada consulta, descarga, alta y modificación.
5. Se confirma que ningún rol dispone de eliminación desde el celular y que las rutas fabricadas reciben 401/403/405.

