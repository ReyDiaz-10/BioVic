# Prueba de aceptación web/QR de BioVic

Use un equipo de prueba y tres usuarios que no sean compartidos:

- un Consultor con acceso a una sola ubicación;
- un Editor con acceso a una sola ubicación;
- el Administrador Maestro.

No use equipos clínicos reales para esta prueba.

## Preparación

1. Publique el incremento y confirme que `/health` devuelve HTTP 200.
2. Abra la aplicación de escritorio y genere el QR del equipo de prueba.
3. Confirme que la URL empieza por `https://` y termina en `/e/{identificador}`.
4. Use una ventana privada diferente para cada rol o cierre sesión entre pruebas.

## Consultor

| Paso | Resultado esperado |
| --- | --- |
| Escanear QR sin sesión | Aparece el inicio de sesión de BioVic; no se muestran datos del equipo. |
| Ingresar como Consultor autorizado | Aparece la ficha y el rol “Consultor”. |
| Descargar PDF | Se descarga la hoja de vida. |
| Revisar acciones | No aparecen mantenimiento, calibración, falla, incidente, edición, creación ni eliminación. |
| Abrir manualmente `/e/{id}/maintenance/new` | El servidor responde 403. |
| Consultar un QR de una ubicación no autorizada | El servidor niega el acceso; no expone la ficha. |
| Cerrar sesión | Regresa al inicio de sesión del mismo QR. Retroceder no debe revelar la ficha. |

## Editor

| Paso | Resultado esperado |
| --- | --- |
| Ingresar como Editor autorizado | Aparece la ficha y el rol “Editor”. |
| Agregar mantenimiento | El formulario guarda y la ficha aumenta el contador. |
| Agregar calibración | El formulario guarda y la ficha aumenta el contador. |
| Registrar falla | El formulario guarda y la ficha aumenta el contador. |
| Registrar incidente | El formulario guarda y la ficha aumenta el contador. |
| Revisar acciones | No aparecen editar ficha, crear equipo, crear usuario, crear ubicación ni eliminar. |
| Abrir manualmente `/e/{id}/edit` o `/e/mobile/users/new` | El servidor responde 403. |
| Cerrar sesión | Regresa al inicio de sesión del mismo QR. |

## Administrador Maestro

| Paso | Resultado esperado |
| --- | --- |
| Abrir `https://DOMINIO/e/login` | Puede iniciar sesión y llega al panel móvil. |
| Crear ubicación de prueba | Se crea y queda auditada. |
| Crear Consultor o Editor | Exige rol y todas las ubicaciones o al menos una ubicación. No permite otro Maestro. |
| Crear equipo | Exige ubicación y abre la ficha creada. |
| Editar equipo | Puede cambiar estado, ubicación, responsable, cargo y demás datos; exige motivo. |
| Agregar los cuatro registros | Puede crear mantenimiento, calibración, falla e incidente. |
| Revisar acciones | No aparece ninguna acción de borrar equipo, usuario, ubicación ni registro. |
| Cerrar sesión desde una ficha QR | Regresa al inicio de sesión del mismo QR. |

## Comprobaciones de seguridad

- Verifique en el navegador que el candado HTTPS esté activo y que no haya advertencias de certificado.
- Después de cerrar sesión, copie la URL de una ficha en otra pestaña: debe pedir credenciales.
- Envíe un formulario dos veces después de cerrar sesión: debe fallar, no duplicar datos.
- Pruebe cinco contraseñas incorrectas con una cuenta de prueba y confirme el bloqueo temporal configurado.
- Revise que la auditoría identifique usuario, acción, equipo, fecha y ubicación.
- Confirme que no existen botones de correo ni variables SMTP requeridas.

## Registro del resultado

Anote por cada caso: fecha, usuario de prueba, teléfono/navegador, resultado esperado, resultado observado y captura. Si un caso falla, no continúe con datos reales; registre el error exacto y el evento de auditoría asociado.

