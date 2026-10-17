# BioVic

BioVic es un Sistema de Gestión de Equipos Médicos y Biomédicos para Windows. Esta primera entrega implementa una base funcional y extensible con WPF, MVVM, ASP.NET Core, Entity Framework Core y SQLite.

## Funciones incluidas

- Búsqueda autenticada de equipos por nombre, serial, modelo, código, fabricante, responsable o ubicación, limitada a las ubicaciones autorizadas para cada usuario.
- Búsqueda tolerante a tildes, mayúsculas, coincidencias parciales y errores ortográficos mediante Damerau-Levenshtein.
- Configuración inicial de un único Administrador Maestro.
- Inicio y cierre de sesión, contraseñas con `PasswordHasher`, bloqueo temporal y sesiones con expiración.
- Roles Consulta, Editor y Administrador Maestro, verificados en la API y en la interfaz.
- Registro de equipos con ubicación obligatoria, catálogo administrable de sedes, permisos por ubicación, mantenimientos y cambios de estado.
- Hoja de vida inicial con resumen, mantenimientos, calibraciones y línea de tiempo de estados.
- Dashboard administrativo mediante API.
- Portal web QR protegido por usuario y contraseña usando un `PublicId` aleatorio.
- Auditoría encadenada SHA-256 con verificación de integridad.
- Soft delete preparado para entidades críticas.
- Migración inicial de SQLite, índices y modo WAL.
- Pruebas automatizadas del buscador, motor de reglas y hash de auditoría.

## Arquitectura

```text
BioVic.Desktop (WPF/MVVM)
          │ HTTP local o LAN
          ▼
BioVic.Api (autenticación y autorización)
          │
          ▼
BioVic.Application ── BioVic.Domain
          ▲
          │
BioVic.Infrastructure (EF Core) ── SQLite
          │
          └── BioVic.QrPortal
```


También puede abrir `BioVic.sln` y configurar varios proyectos de inicio: primero `BioVic.Api` y después `BioVic.Desktop`.

## 🚀 Acceso de Demostración (Demo)

Para facilitar la evaluación técnica del sistema sin necesidad de realizar configuraciones locales, la base de datos se encuentra desplegada y operativa en **Azure**. 

Puedes iniciar sesión en la aplicación de escritorio utilizando las siguientes credenciales de prueba. 

> **Nota Importante:** Este usuario cuenta con un rol **Informativo (Solo Lectura)**. Podrás navegar libremente por todos los módulos del sistema y visualizar la arquitectura de los datos, pero las acciones de creación, edición y eliminación (CRUD) se encuentran deshabilitadas para este perfil con el fin de proteger la integridad del entorno de demostración.

* **Usuario:** demobiovic 
* **Contraseña:** DemoBioVic2026*+
* **Portal / API en Azure:** https://biovic-api-prod-2026-crckevccada8hsdb.canadacentral-01.azurewebsites.net/e/login


## Comandos

```powershell
dotnet restore BioVic.sln
dotnet build BioVic.sln -c Debug
dotnet test BioVic.sln -c Debug
```

Publicación Windows x64:

```powershell
.\scripts\publish-windows.ps1
```

## Documentación

- [Arquitectura](docs/ARCHITECTURE.md)
- [Base de datos](docs/DATABASE.md)
- [Instalación](docs/INSTALLATION.md)
- [Seguridad](docs/SECURITY.md)
- [Inteligencia artificial](docs/AI.md)
- [Manual de usuario](docs/USER_MANUAL.md)
- [Estado de implementación](docs/IMPLEMENTATION_STATUS.md)

## Importante

Esta es la primera construcción funcional, Documentos, QR imprimible, reportes PDF/Excel, backup/restauración, RAG e instalador se mantienen en el plan y están delimitados en `IMPLEMENTATION_STATUS.md`.
