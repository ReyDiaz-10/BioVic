# Correccion de migraciones para Azure SQL

## Causa

BioVic admite SQLite en desarrollo y SQL Server en Azure. Entity Framework Core
necesita un conjunto de migraciones y un `ModelSnapshot` independientes para
cada proveedor. Las migraciones existentes pertenecen a SQLite, por lo que no
deben utilizarse para inicializar Azure SQL.

## Solucion incluida

- SQLite conserva `BioVic.Infrastructure/Migrations`.
- SQL Server utiliza `BioVic.Infrastructure.SqlServerMigrations/Migrations`.
- El proyecto de la API incluye el ensamblado de migraciones SQL Server.
- `dotnet-ef` queda fijado en la version 10.0.11.
- La fabrica de tiempo de diseno no contiene credenciales ni abre conexiones.

## Generar la migracion inicial

Desde la raiz del proyecto, con BioVic y Visual Studio cerrados:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\create-sqlserver-migration.ps1
```

El resultado correcto termina con:

```text
No changes have been made to the model since the last migration.
MIGRACION SQL SERVER CREADA Y VALIDADA
```

El script crea tres archivos dentro de:

```text
src\BioVic.Infrastructure.SqlServerMigrations\Migrations
```

No ejecute el script una segunda vez. Las migraciones creadas deben conservarse
en el proyecto y formar parte de todas las publicaciones futuras.

## Publicar despues de validar

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-api-azure.ps1
```

La base de datos Azure SQL debe estar vacia en este primer despliegue. No se
suprime `PendingModelChangesWarning`: la correccion mantiene la validacion de
seguridad de Entity Framework Core.
