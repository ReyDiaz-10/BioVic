# Prueba integral de BioVic en teléfonos

## Preparación

Use un equipo real ya creado y tres cuentas asignadas a su ubicación: Consultor, Editor y Administrador Maestro. Pruebe con datos ficticios y conserve el `PublicId` del enlace QR. No comparta contraseñas por mensajes ni las escriba dentro de scripts.

Ejecute primero la prueba automatizada desde PowerShell 7:

```powershell
cd "C:\Users\neilm\OneDrive\Desktop\BioVic\BioVic"
pwsh -NoProfile -File .\scripts\test-mobile-e2e.ps1 `
  -ApiUrl "https://SU-APP.azurewebsites.net" `
  -EquipmentPublicId "GUID-DEL-EQUIPO" `
  -SearcherUsername "usuario_consultor" `
  -EditorUsername "usuario_editor" `
  -MasterUsername "usuario_maestro"
```

Las contraseñas se solicitan de forma oculta. Esta prueba crea tres sesiones aisladas que simulan Android/Chrome, iPhone/Safari y Samsung Internet; valida HTTPS, permisos, PDF y cierre de sesión.

## Prueba física obligatoria

Realice la matriz en al menos dos teléfonos físicos y, si es posible, con Wi‑Fi y datos móviles.

| Caso | Consultor | Editor | Administrador Maestro |
|---|---:|---:|---:|
| Escanear QR y autenticar | Sí | Sí | Sí |
| Ver ficha y documentos | Sí | Sí | Sí |
| Descargar hoja de vida PDF | Sí | Sí | Sí |
| Agregar mantenimiento/calibración/falla/incidente | No | Sí | Sí |
| Crear equipo, usuario y ubicación | No | No | Sí |
| Editar ficha/estado/ubicación/responsable | No | No | Sí |
| Eliminar o restaurar desde teléfono | No | No | No |
| Cerrar sesión y volver al acceso inicial del QR | Sí | Sí | Sí |

Después de cada escritura móvil, abra BioVic en el computador y compruebe que el cambio aparece en la misma ficha y en auditoría. Verifique también que un usuario sin acceso a la ubicación reciba `403` y no vea el equipo.

## Criterio de aprobación

La versión móvil se aprueba cuando todos los casos anteriores funcionan en ambos teléfonos, no aparece contenido después de cerrar sesión y las operaciones quedan registradas en SQL Server y en auditoría. Una simulación de agente de navegador ayuda, pero no sustituye esta prueba física de cámara QR, descarga, teclado, cookies y red móvil.
