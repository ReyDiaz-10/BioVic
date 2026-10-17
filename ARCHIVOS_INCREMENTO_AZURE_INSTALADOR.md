# Incremento Azure + instalador Windows

Este paquete se aplica encima de `BioVic(3).zip`. No reemplace el proyecto completo.

## Archivos existentes que se reemplazan

1. `Directory.Packages.props`
2. `global.json`
3. `src/BioVic.Api/Program.cs`
4. `src/BioVic.Desktop/App.xaml.cs`
5. `src/BioVic.Desktop/BioVic.Desktop.csproj`
6. `src/BioVic.Desktop/ViewModels/MainWindowViewModel.cs`
7. `src/BioVic.Infrastructure/BioVic.Infrastructure.csproj`
8. `src/BioVic.Infrastructure/InfrastructureServiceCollectionExtensions.cs`
9. `src/BioVic.Infrastructure/Data/BioVicDbContext.cs`
10. `src/BioVic.Infrastructure/Data/DatabaseInitializer.cs`
11. `src/BioVic.Infrastructure/Migrations/20260820022610_InitialCreate.cs`
12. `src/BioVic.Infrastructure/Migrations/20260820022610_InitialCreate.Designer.cs`
13. `src/BioVic.Infrastructure/Migrations/BioVicDbContextModelSnapshot.cs`
14. `src/BioVic.Infrastructure/Options/BioVicOptions.cs`
15. `src/BioVic.Infrastructure/Options/ProductionConfigurationValidator.cs`
16. `tests/BioVic.Tests/ProductionConfigurationValidatorTests.cs`

## Archivos nuevos que se agregan

1. `docs/AZURE_STUDENT_DEPLOYMENT.md`
2. `installers/BioVic.iss`
3. `scripts/publish-api-azure.ps1`
4. `scripts/deploy-api-azure.ps1`
5. `scripts/publish-client-windows.ps1`

## Elementos que no se deben reemplazar ni compartir

- `.env`
- `data/`
- `backups/`
- `logs/`
- cualquier `bin/` u `obj/`
- bases `*.db`, `*.db-wal` y `*.db-shm`
- contrasenas, tokens, claves SMTP o cadenas de conexion

No se modificaron XAML, colores, fuentes, tamanos, distribucion visual ni ventanas funcionales.

## Primera validacion en Windows

El proyecto ahora usa el SDK estable .NET 10.0.400. Compruebe:

```powershell
dotnet --version
```

Luego, desde la raiz de BioVic:

```powershell
dotnet build-server shutdown
dotnet restore BioVic.sln
dotnet build BioVic.sln -c Debug --no-incremental
dotnet test BioVic.sln -c Debug --no-build
```

No publique en Azure hasta que la compilacion y las pruebas terminen sin errores. Las advertencias de analizadores se revisaran aparte y no deben ocultar errores de compilacion.

Despues continue con `docs/AZURE_STUDENT_DEPLOYMENT.md`.
