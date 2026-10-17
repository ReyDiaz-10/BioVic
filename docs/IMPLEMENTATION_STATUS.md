# Estado de implementación

Fecha del incremento: 19 de agosto de 2026.

| Fase | Estado | Alcance |
|---|---|---|
| 1. Arquitectura, BD y migraciones | Completa para núcleo | Solución por capas, modelo, SQLite, migración e índices. |
| 2. Usuarios, roles y autenticación | Operativo | Master único, login obligatorio, hash, bloqueo, sesiones, administración de usuarios y permisos por ubicación. |
| 3. Inventario | Operativo | Crear, consultar, editar y dar de baja lógicamente; ubicación obligatoria y alcance por usuario. |
| 4. Búsqueda normal y fuzzy | Completa para MVP | Normalización, parciales, ranking y Damerau-Levenshtein. |
| 5. Hoja de vida | Parcial funcional | Resumen, mantenimiento, calibración e historial. Faltan todas las pestañas editables. |
| 6. Mantenimiento, calibración y auditorías | Parcial | Mantenimiento funcional; modelos de calibración/fallas/incidentes listos. |
| 7. Trazabilidad y AuditLog | MVP completo | Cadena SHA-256, antes/después, actor y verificación de integridad. |
| 8. Dashboard | Operativo | Indicadores, auditoría y bandeja administrativa disponibles en la interfaz. |
| 9. QR y portal web | Operativo | QR PNG descargable/imprimible y portal protegido por login y permiso de ubicación. |
| 10. Documentos | Modelo preparado | Falta carga, versionado físico y verificación periódica de hash. |
| 11. BioVic IA y Ollama | Parcial funcional | Reglas, bandeja de alertas, proveedor Ollama y consulta por equipo; falta RAG documental. |
| 12. RAG con manuales | Diseñada | Falta extracción, fragmentación, índice y citas. |
| 13. Reportes | Parcial funcional | Hoja de vida PDF completa; faltan Excel/CSV e informes consolidados. |
| 14. Backups y restauración | Parcial funcional | SQLite crea copias automáticas con `integrity_check`; falta réplica cifrada externa, restauración guiada y documentos/fotos. |
| 15. Pruebas | Iniciada | Reglas, búsqueda y hash; faltan integración API/SQLite y autorización. |
| 16. Instalador | Parcial | Hay publicación separada para servidor IIS y cliente Windows; falta instalador firmado y actualización. |

En ambiente `Development`, un Administrador Maestro puede llamar `POST /api/admin/demo/seed` para generar 20 equipos y cuatro ubicaciones claramente ficticias. Solo funciona con el inventario vacío.

## Próximo incremento recomendado

Completar la fase 5 y 6: formularios de calibración, auditorías, fallas, traslados, responsables y tecnovigilancia; después añadir pantalla administrativa de usuarios y auditoría. Esto cierra el núcleo operativo antes de QR imprimible, documentos e IA.

## Incremento de navegación y administración

Se añadió navegación modal desde el menú lateral, inventario ordenable y el panel de Administrador Maestro para indicadores, usuarios, permisos, suspensión/eliminación lógica y auditoría. Consulte `INCREMENTO_ADMIN_UI.md` para el alcance y los siguientes pasos.

## Incremento operativo integral

- Ventanas separadas para Inventario, Mantenimientos, Calibraciones y Tecnovigilancia.
- Edición administrativa y baja lógica de equipos con motivo y auditoría.
- Mantenimientos detallados con hallazgos, diagnóstico, acciones, repuestos y pendientes.
- Calibraciones con laboratorio, certificado, resultado, trazabilidad y próxima fecha.
- Fallas e incidentes de tecnovigilancia como registros independientes.
- QR PNG descargable e imprimible; portal QR y PDF protegidos por usuario, contraseña y permiso de ubicación.
- Hoja de vida completa en PDF para Editor y Administrador Maestro.
- Descarga local de la hoja de vida PDF. El envío SMTP fue retirado del producto.
- BioVic IA con reglas determinísticas y conexión configurable con Ollama/proveedor compatible.
- Etiquetas de estados, criticidad, resultados y acciones visibles en español latino.
- Catálogo de ubicaciones administrable, selección obligatoria al crear equipos y permisos por usuario.
- Bandeja persistente y posponible para fechas próximas y equipos que quedaron sin ubicación.
