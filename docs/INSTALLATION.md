# Instalación y ejecución

## Requisitos para desarrollo

- Windows 10 u 11 x64.
- SDK .NET 10.
- Visual Studio 2026 recomendado, con **Desarrollo de escritorio con .NET** y **ASP.NET y desarrollo web**.
- Acceso a NuGet durante la primera restauración de dependencias.

## Configuración

Desde la raíz del proyecto:

```powershell
Copy-Item .env.example .env
```

Para uso en un solo PC no es necesario modificar los valores predeterminados. La base se creará en `src\BioVic.Api\bin\...\data\biovic.db` cuando se ejecuta desde Visual Studio, o junto a la API publicada.

## Compilar y probar

```powershell
dotnet restore BioVic.sln
dotnet build BioVic.sln -c Debug
dotnet test BioVic.sln -c Debug
```

## Ejecutar en desarrollo

```powershell
.\scripts\run-dev.ps1
```

El script inicia la API, espera `http://127.0.0.1:5180/health` y abre el cliente WPF.

## Primer inicio

1. BioVic detecta que no existe Administrador Maestro.
2. Complete nombre, apellidos, usuario, correo opcional y contraseña.
3. El sistema crea la única cuenta maestra e inicia sesión.
4. Cree las sedes y ubicaciones desde **Administrador Maestro > Ubicaciones**.
5. Al crear usuarios, seleccione las ubicaciones que podrán consultar o active **Todas las ubicaciones**.
6. Seleccione **Nuevo equipo** y registre el inventario. La ubicación es obligatoria.

## Publicar Windows x64

```powershell
.\scripts\publish-windows.ps1
```

El resultado queda en `artifacts\windows-x64` con carpetas `api` y `desktop` y un iniciador `Start-BioVic.cmd`. Los datos viven bajo `api\data` y no deben eliminarse durante una actualización.

## Portal QR en la red local

Para permitir acceso desde teléfonos de la misma LAN, abra PowerShell como Administrador y ejecute:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-qr-lan.ps1
.\scripts\run-dev.ps1
.\scripts\test-qr-lan.ps1
```

El configurador conserva las demás variables de `.env` y crea una regla de firewall limitada al perfil de red privada y a la subred local. No use `localhost` en el QR: desde un teléfono significaría el propio teléfono. Consulte `docs/INCREMENTO_02_QR_RED_LOCAL.md` para la prueba completa. En producción institucional use DNS interno y HTTPS.

El portal QR exige el mismo usuario y contraseña de BioVic. Después de autenticarse, el servidor valida que la cuenta tenga permiso para la ubicación actual del equipo antes de mostrar la ficha o permitir la descarga del PDF.

## Migración futura a varios PC

Para publicar BioVic con un dominio accesible desde otras clínicas, HTTPS e IIS, siga `docs/PRODUCTION_STAGE1.md`. En ese modelo el puerto 5180 permanece privado y los clientes usan el dominio seguro.

1. Publique `BioVic.Api` en el servidor.
2. Mueva la base y documentos al almacenamiento protegido del servidor.
3. Configure HTTPS y el firewall.
4. Instale solo `BioVic.Desktop` en cada estación.
5. Defina `BIOVIC_API_URL=https://servidor-biovic` en cada cliente.
6. Mantenga un único proceso responsable de migraciones y backups.

## Solución de problemas

- **No fue posible conectar con la API:** confirme que BioVic.Api está activo y que la URL coincide.
- **SQLite unable to open database file:** verifique permisos de escritura en la carpeta `data`.
- **La sesión expiró:** inicie sesión nuevamente.
- **El teléfono no abre el QR:** verifique conectividad LAN, firewall y `BIOVIC_QR_BASE_URL`.
- **Ollama no disponible:** no afecta inventario; la IA aún no está habilitada en este MVP.
