# Incremento: navegación y Administración Maestra

## Implementado

- Cada opción de gestión del menú lateral abre una ventana modal contextual.
- Inventario modal con filtro y orden por nombre A–Z/Z–A, modelo y próxima fecha de mantenimiento.
- Acceso desde cada módulo a la hoja de vida del equipo seleccionado.
- Panel del Administrador Maestro con resumen operativo, usuarios y auditoría.
- Creación de usuarios con rol Consulta o Editor.
- Cambio de rol, habilitación y suspensión de cuentas.
- Eliminación lógica de usuarios con motivo obligatorio y revocación de sesiones activas.
- Visualización de movimientos y búsquedas autenticadas con fecha/hora, usuario, equipo e IP.
- Verificación de integridad de la cadena de auditoría desde la interfaz.

## Criterios de seguridad

- El Administrador Maestro único no puede eliminarse, suspenderse ni cambiar de rol.
- No se permite asignar el rol Administrador Maestro desde la administración de usuarios.
- Los usuarios eliminados permanecen en la auditoría y no se destruye su historial.
- Tecnovigilancia se reserva para fallas, incidentes y acciones de seguimiento; la edición de la ficha maestra corresponde a Inventario/Hoja de Vida.

## Siguiente incremento

1. Edición versionada y baja lógica de equipos desde su hoja de vida.
2. Formularios completos de calibración, fallas e incidentes de tecnovigilancia.
3. QR descargable/imprimible y hoja de vida PDF protegida por autenticación y ubicación.
4. Generación y descarga de la hoja de vida PDF.
5. BioVic IA local de solo lectura, reglas y RAG con documentos aprobados.

El envío directo por correo fue retirado. Los usuarios descargan el PDF y lo adjuntan desde su proveedor habitual.
