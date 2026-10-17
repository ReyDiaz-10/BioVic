# Diagnóstico funcional de BioVic — incremento integral

Fecha: 29 de agosto de 2026

## Conclusión

Después de aplicar este incremento, generar las migraciones, desplegar el API y superar la aceptación física, BioVic queda completo para el alcance funcional acordado: inventario biomédico, hoja de vida, trazabilidad, acceso por ubicación, portal QR con roles, documentos versionados, recuperación de eliminaciones lógicas y actualizaciones firmadas. Esto no convierte por sí solo el producto en un dispositivo médico ni sustituye una validación regulatoria, una prueba de penetración independiente o el plan operativo de copias de seguridad de la institución.

## Matriz del alcance

| Capacidad | Estado en la fuente | Validación necesaria después del despliegue |
|---|---|---|
| Inventario y hoja de vida | Implementado | Prueba de aceptación con datos ficticios |
| Mantenimiento, calibración, fallas e incidentes | Implementado en escritorio y móvil según rol | Confirmar escritura en SQL Server y auditoría |
| QR cifrado | Implementado mediante HTTPS, cookie `Secure`/`HttpOnly`/`SameSite`, antifalsificación, CSP y no-caché | Usar únicamente dominio HTTPS de Azure y probar en teléfono |
| Permisos por rol y ubicación | Implementado en API, no sólo ocultado en interfaz | Probar usuario sin acceso y confirmar HTTP 403 |
| Documentos, fotos y certificados | Implementado: tipos permitidos, firma mágica, máximo configurable, SHA‑256, versión vigente e historial | Crear migración, desplegar, cargar PDF/JPG y descargarlo |
| Eliminación lógica y restauración | Implementado para equipos, usuarios, ubicaciones y documentos | Sólo Administrador Maestro en escritorio; comprobar auditoría |
| Eliminación desde celular | Prohibida para todos los roles | La prueba automatizada falla si aparece una ruta destructiva |
| Actualización automática | Implementado al inicio del cliente | Requiere certificado real y publicar manifiesto/instalador firmados |
| Envío SMTP | Retirado por decisión del proyecto | Descargar PDF y compartirlo fuera de BioVic |
| Pruebas multiusuario/multiteléfono | Script E2E de tres sesiones y guía física incluidos | Ejecutar en al menos Android y iPhone reales |

## Permisos móviles finales

| Rol | Permitido desde el celular |
|---|---|
| Consultor | Consultar ficha, documentos vigentes y descargar PDF |
| Editor | Lo anterior; agregar mantenimientos, calibraciones, fallas e incidentes |
| Administrador Maestro | Lo anterior; crear equipos, usuarios y ubicaciones; editar ficha, estado, ubicación y responsable |
| Todos | Cerrar sesión y regresar al ingreso inicial; nunca borrar ni restaurar desde el teléfono |

## Controles documentales

- Los binarios se guardan fuera de `wwwroot`; no son enlaces públicos directos.
- Se admiten PDF, PNG, JPG/JPEG y WEBP, verificando que el contenido coincida con la extensión.
- El tamaño predeterminado máximo es 20 MiB, configurable con `BIOVIC_DOCUMENT_MAX_BYTES` hasta 50 MiB.
- Cada descarga vuelve a calcular SHA‑256; una alteración física bloquea el archivo.
- Una nueva versión conserva la anterior y marca una sola versión vigente.
- Archivar no borra el archivo; restaurar exige motivo y queda auditado.

## Aspectos externos que todavía determinan una salida a producción

1. Comprar o disponer de un certificado de firma de código RSA confiable. Sin él puede probarse la aplicación, pero no debe llamarse “actualización firmada de producción”.
2. Ejecutar la prueba física de dos teléfonos y tres roles incluida en `docs/PRUEBA_INTEGRAL_TELEFONOS.md`.
3. Confirmar restauración de SQL Azure mediante un ensayo controlado y documentar RPO/RTO institucionales.
4. Configurar monitorización de disponibilidad, presupuesto y alertas de Azure.
5. Realizar revisión de privacidad, retención documental y seguridad independiente antes de almacenar información real sensible.

La compilación automatizada y las pruebas de fuente demuestran consistencia técnica; los cinco puntos anteriores son controles operativos y externos que un ZIP no puede certificar por sí solo.
