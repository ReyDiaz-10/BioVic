# Seguridad

## Matriz de permisos

| Acción | Sin sesión | Consulta | Editor | Administrador Maestro |
|---|:---:|:---:|:---:|:---:|
| Buscar y consultar equipos autorizados | No | Sí | Sí | Sí |
| Iniciar sesión desde el portal QR | Sí | Sí | Sí | Sí |
| Ver equipo/PDF desde QR después de autenticar | No | Según ubicación | Según ubicación | Sí |
| Crear equipo en una ubicación activa | No | No | Sí | Sí |
| Agregar mantenimiento, calibración o tecnovigilancia | No | No | Según ubicación | Sí |
| Modificar o dar de baja un equipo | No | No | No | Sí |
| Modificar silenciosamente historial | No | No | No | No |
| Crear, editar o eliminar ubicaciones | No | No | No | Sí |
| Crear usuarios y asignar ubicaciones | No | No | No | Sí |
| Crear otro Administrador Maestro | No | No | No | No |
| Consultar bandeja, dashboard y auditoría completa | No | No | No | Sí |
| Verificar integridad de auditoría | No | No | No | Sí |
| Borrar AuditLog desde la interfaz | No | No | No | No |

La autorización se aplica en la API. Ocultar un botón en WPF es solo una medida de experiencia de usuario, no un control de seguridad.

## Contraseñas

- Se usa `Microsoft.AspNetCore.Identity.PasswordHasher<User>`; no se guardan contraseñas en texto plano.
- Política inicial: mínimo 10 caracteres, mayúscula, minúscula, número y símbolo.
- Cinco fallos consecutivos generan un bloqueo temporal configurable.
- El inicio correcto restablece el contador; un hash antiguo se actualiza si el proveedor lo recomienda.

## Sesiones

- El cliente recibe un token opaco aleatorio de 256 bits.
- La base de datos guarda únicamente SHA-256 del token.
- Cada sesión tiene expiración, revocación, equipo e IP.
- El cliente WPF mantiene el token solo en memoria durante esta fase; no se escribe en disco.
- El portal QR mantiene el token en una cookie `HttpOnly`, `SameSite=Strict` y con expiración; bajo HTTPS también se marca `Secure`.

## Restricción por ubicación

- La API aplica el alcance de ubicaciones en el servidor; ocultar filas en la interfaz no es el control de seguridad.
- Consulta y Editor solo ven equipos de sus ubicaciones autorizadas, salvo que tengan **Todas las ubicaciones**.
- Un Editor puede registrar un equipo en cualquier ubicación activa. Si no tiene acceso a ella, el equipo deja de ser visible para ese Editor inmediatamente después de guardarlo.
- El Administrador Maestro mantiene acceso global, incluidos equipos que quedaron temporalmente sin ubicación.

## Auditoría resistente a alteraciones

Cada registro calcula:

```text
RecordHash = SHA256(CanonicalRecordData + PreviousHash)
```

`GET /api/admin/audit/integrity` recorre la cadena en orden y reporta el primer registro inconsistente. La cadena ayuda a detectar cambios directos, pero no sustituye controles de acceso al archivo SQLite, backups protegidos ni firmas externas.

## Información excluida de logs

No se registran contraseñas, tokens de sesión, API keys ni secretos de IA. Los errores técnicos van a `logs/biovic-AAAA-MM-DD.log`; la interfaz recibe mensajes humanos.

## Despliegue en red

- Mantener la API en `127.0.0.1` para uso de un solo PC.
- Para LAN, usar una IP fija o DNS interno, reglas de firewall limitadas y HTTPS con certificado institucional.
- No publicar SQLite como carpeta compartida. Solo la API debe acceder a la base.
- El portal QR no muestra datos antes de autenticar y vuelve a comprobar el permiso de ubicación para cada equipo.

## Controles de la Etapa 1

- `BIOVIC_ENVIRONMENT=Production` activa validaciones que rechazan HTTP público, hosts globales y Kestrel expuesto directamente.
- BioVic solo confía en los proxies declarados en `BIOVIC_TRUSTED_PROXY_IPS`.
- El inicio de sesión se limita por IP y conserva el bloqueo por cuenta.
- Los formularios QR incluyen protección antifalsificación.
- Las cookies son `HttpOnly`, `SameSite=Strict` y `Secure` en producción.
- Se aplican HSTS, CSP, protección contra marcos, `nosniff`, política de referente y prohibición de caché.
- `/health` oculta PID y diagnósticos internos en producción.
- La creación inicial del Administrador Maestro queda cerrada por defecto y exige token temporal si se habilita.
- Kestrel limita el cuerpo y encabezados de las solicitudes y no expone el encabezado `Server`.

Consulte `PRODUCTION_STAGE1.md` para la configuración y las pruebas.

## Recomendaciones operativas adicionales

1. Mantener el certificado HTTPS y sus renovaciones bajo control institucional.
2. Proteger con ACL de Windows las carpetas `data`, `documents`, `photos`, `backups` y `logs`.
3. Replicar las copias verificadas a un destino cifrado y probar restauraciones.
4. Realizar revisión de seguridad, análisis de dependencias y pruebas de penetración.
5. Definir política institucional de acceso, retención y protección de datos.
