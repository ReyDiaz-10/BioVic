# Etapa 1 · Archivos modificados y reemplazo

## Garantías de alcance

- No se modificó ningún archivo `.xaml`.
- No se cambiaron colores, tipografías, tamaños, textos visibles ni distribución de ventanas WPF.
- No se incluye `.env`, base de datos, registros, copias, `bin` ni `obj`.
- El QR, PDF, permisos por ubicación, auditoría y hotfix de arranque existentes se conservaron.

## Archivos existentes modificados

```text
.gitignore
README.md
global.json
docs/ARCHITECTURE.md
docs/DATABASE.md
docs/IMPLEMENTATION_STATUS.md
docs/INSTALLATION.md
docs/SECURITY.md
scripts/publish-windows.ps1
src/BioVic.Api/Endpoints/ApiEndpoints.cs
src/BioVic.Api/Middleware/ExceptionHandlingMiddleware.cs
src/BioVic.Api/Program.cs
src/BioVic.Desktop/App.xaml.cs
src/BioVic.Desktop/BioVic.Desktop.csproj
src/BioVic.Desktop/Services/BioVicApiClient.cs
src/BioVic.Infrastructure/Data/DatabaseInitializer.cs
src/BioVic.Infrastructure/InfrastructureServiceCollectionExtensions.cs
src/BioVic.Infrastructure/Options/BioVicOptions.cs
src/BioVic.Infrastructure/Services/QrAccessService.cs
src/BioVic.QrPortal/QrPortalRenderer.cs
tests/BioVic.Tests/QrAccessServiceTests.cs
```

## Archivos nuevos

```text
.env.example
.env.production.example
docs/ETAPA1_ARCHIVOS_MODIFICADOS.md
docs/PRODUCTION_STAGE1.md
scripts/publish-server-windows.ps1
scripts/test-production-readiness.ps1
scripts/verify-stage1.ps1
src/BioVic.Api/Auth/SetupSecurity.cs
src/BioVic.Api/Security/ApiSecurityPolicies.cs
src/BioVic.Api/appsettings.Production.json
src/BioVic.Desktop/Services/ClientConfiguration.cs
src/BioVic.Desktop/biovic.client.example.json
src/BioVic.Infrastructure/Options/ProductionConfigurationValidator.cs
src/BioVic.Infrastructure/Services/SqliteBackupHostedService.cs
tests/BioVic.Tests/ProductionConfigurationValidatorTests.cs
```

## Forma segura de reemplazo

1. Cierre BioVic y BioVic.Api.
2. Haga una copia de la carpeta actual del proyecto y de la base de datos.
3. Extraiga el paquete de archivos modificados sobre la raíz donde se encuentra `BioVic.sln`, conservando las rutas.
4. No reemplace ni elimine su `.env` actual.
5. No reemplace las carpetas `data`, `backups`, `logs`, `documents` ni `photos`.
6. Instale un SDK estable de .NET 10 si `dotnet --version` todavía muestra una versión preliminar.
7. Ejecute:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\verify-stage1.ps1
   ```

8. Para seguir probando localmente, mantenga `BIOVIC_ENVIRONMENT=Development` en su `.env` y ejecute `scripts\run-dev.ps1`.

La plantilla `.env.production.example` se utilizará más adelante en el servidor. No debe copiarse todavía sobre el `.env` de desarrollo.
