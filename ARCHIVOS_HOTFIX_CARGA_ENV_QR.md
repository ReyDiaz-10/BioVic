# Hotfix · Carga de `.env` para QR LAN

Copie el contenido del ZIP sobre la raíz de BioVic y reemplace únicamente estos archivos:

- `scripts/run-dev.ps1`
- `scripts/test-qr-lan.ps1`
- `src/BioVic.Api/Endpoints/ApiEndpoints.cs`
- `src/BioVic.Api/Program.cs`
- `src/BioVic.Infrastructure/Options/EnvironmentFileLoader.cs`

Archivo de prueba nuevo:

- `tests/BioVic.Tests/EnvironmentFileLoaderTests.cs`

Este parche no contiene ni modifica directamente `.env`, credenciales o la base de datos. Corrige la localización del `.env` cuando `dotnet run` cambia el directorio de trabajo y hace que `run-dev.ps1` aplique de forma determinista las tres direcciones de red definidas en el `.env` de la raíz.
