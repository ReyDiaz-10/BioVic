# BioVic en Azure Students y cliente instalable

## Resultado esperado

- Azure App Service ejecuta BioVic.Api y publica HTTPS.
- Azure SQL Database conserva usuarios, equipos, auditoria y transacciones.
- Los codigos QR apuntan al dominio `azurewebsites.net` y funcionan fuera de la red local.
- Cada usuario instala solamente el cliente BioVic para Windows e inicia sesion.
- El cliente incluye .NET; no requiere `.env`, SQLite, ngrok, Ollama ni scripts.

La interfaz WPF no se modifica. La URL de Azure se guarda en `biovic.client.json`, que no contiene secretos.

## Limite del piloto gratuito

El plan F1 de App Service es apropiado para desarrollo y pruebas: tiene cuotas y puede suspender la aplicacion. Para uso clinico continuo se necesitara un plan con disponibilidad y soporte. Azure SQL Free es preferible a SQLite porque App Service usa almacenamiento compartido y SQLite depende de bloqueos de archivo.

## 1. Crear Azure SQL Database Free

En Azure Portal cree una base de datos SQL con la oferta gratuita:

- Grupo de recursos: por ejemplo `rg-biovic-piloto`.
- Base de datos: por ejemplo `biovic`.
- Servidor SQL: nombre global unico, misma region de la Web App.
- Autenticacion: cree un administrador SQL y una contrasena fuerte. No los comparta ni los guarde en el proyecto.
- Oferta gratuita: 100 000 segundos de nucleo virtual y 32 GB.
- Al alcanzar el limite gratuito: seleccione la opcion que detiene la base de datos para evitar cargos.
- Red: para el piloto, permita que servicios de Azure accedan al servidor y agregue la IP de administracion del propietario.

Copie la cadena de conexion ADO.NET y reemplace solamente el marcador de contrasena dentro de Azure App Service.

## 2. Crear Azure App Service

- Publicar: Codigo.
- Sistema operativo: Linux.
- Pila: .NET 10.
- Region: la misma de Azure SQL.
- Plan inicial: F1 gratuito si aparece disponible.
- Nombre: unico, por ejemplo `biovic-piloto-xxxxx`.

La URL resultante sera `https://NOMBRE.azurewebsites.net`.

## 3. Configurar variables de la Web App

En **Settings > Environment variables** agregue estas variables. Sustituya `NOMBRE` y la cadena SQL por sus valores reales.

| Nombre | Valor |
|---|---|
| `BIOVIC_ENVIRONMENT` | `Production` |
| `BIOVIC_HOSTING_PROVIDER` | `azure-app-service` |
| `BIOVIC_API_URLS` | `http://0.0.0.0:8080` |
| `BIOVIC_ALLOWED_HOSTS` | `NOMBRE.azurewebsites.net` |
| `BIOVIC_QR_BASE_URL` | `https://NOMBRE.azurewebsites.net/e` |
| `BIOVIC_REQUIRE_HTTPS` | `true` |
| `BIOVIC_REVERSE_PROXY_ENABLED` | `true` |
| `BIOVIC_DB_PROVIDER` | `sqlserver` |
| `BIOVIC_DB_CONNECTION` | Cadena ADO.NET de Azure SQL con `Encrypt=True;TrustServerCertificate=False` |
| `BIOVIC_BACKUP_ENABLED` | `false` |
| `BIOVIC_LOG_PATH` | `/home/biovic/logs` |
| `BIOVIC_DATA_PROTECTION_PATH` | `/home/biovic/keys` |
| `BIOVIC_SETUP_ENABLED` | `true` solo durante la creacion del primer administrador |
| `BIOVIC_SETUP_TOKEN` | Secreto aleatorio de 48 caracteres o mas |
| `BIOVIC_SESSION_HOURS` | `8` |
| `BIOVIC_FAILED_LOGIN_LIMIT` | `5` |
| `BIOVIC_LOCKOUT_MINUTES` | `15` |
| `BIOVIC_AI_ENABLED` | `false` durante el primer despliegue |
| `WEBSITES_ENABLE_APP_SERVICE_STORAGE` | `true` |
| `WEBSITES_PORT` | `8080` |

Marque `BIOVIC_DB_CONNECTION` y `BIOVIC_SETUP_TOKEN` como configuraciones de implementacion cuando Azure muestre esa opcion. Nunca cree un `.env` dentro del paquete publicado.

## 4. Crear el paquete y publicarlo

El computador que construye BioVic debe tener un SDK estable de .NET 10. Compruebelo con `dotnet --version`: el resultado debe comenzar por `10.0.` y no debe contener `preview`. `global.json` permite usar la banda estable más reciente de .NET 10 instalada. Esto solo se instala en el computador del desarrollador, no en los PC de los usuarios.

Desde la raiz del proyecto en PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-api-azure.ps1
```

El ZIP queda en:

```text
artifacts\azure-api-linux-x64\BioVic.Api-linux-x64.zip
```

Con Azure CLI instalado y la sesion iniciada:

```powershell
az login
powershell -ExecutionPolicy Bypass -File .\scripts\deploy-api-azure.ps1 `
  -ResourceGroup "rg-biovic-piloto" `
  -AppName "NOMBRE"
```

La implementacion ZIP coloca los archivos de la aplicacion en la raiz correcta de App Service. El ZIP no contiene secretos ni datos locales.

## 5. Primera cuenta y cierre del modo de configuracion

El token inicial no debe incluirse en el instalador. En el computador del propietario, abra PowerShell y ejecute temporalmente:

```powershell
$env:BIOVIC_SETUP_TOKEN = "EL-MISMO-SECRETO-CONFIGURADO-EN-AZURE"
& ".\artifacts\client-win-x64\app\BioVic.exe"
Remove-Item Env:BIOVIC_SETUP_TOKEN
```

Cree el Administrador Maestro. Luego, en Azure:

1. Cambie `BIOVIC_SETUP_ENABLED` a `false`.
2. Elimine `BIOVIC_SETUP_TOKEN`.
3. Guarde los cambios y reinicie la Web App.

## 6. Generar el instalador para otros computadores

Inno Setup se instala solo en el computador que construye el instalador:

```powershell
winget install --exact --id JRSoftware.InnoSetup.7
```

Genere el cliente autocontenido y el instalador:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-client-windows.ps1 `
  -ApiUrl "https://NOMBRE.azurewebsites.net" `
  -Version "1.0.0"
```

El instalador final queda en:

```text
artifacts\client-win-x64\installer\BioVic-Setup-win-x64.exe
```

La otra persona solo debe descargar ese `.exe`, abrirlo, completar la instalacion y usar el acceso directo BioVic. No debe recibir codigo fuente, `.env`, claves SMTP, cadena SQL ni token de configuracion.

## 7. Prueba en un segundo PC

1. Use un PC Windows 10/11 x64 sin SDK de .NET.
2. Descargue y ejecute `BioVic-Setup-win-x64.exe`.
3. Compruebe que aparece el acceso directo BioVic.
4. Abra BioVic e inicie sesion con un usuario de prueba, no con el Administrador Maestro.
5. Busque un equipo permitido por ubicacion.
6. Descargue su PDF.
7. Escanee su QR usando datos moviles y autentiquese.
8. Confirme en Auditoria el inicio de sesion, consulta y descarga.

Mientras el instalador no tenga firma de codigo, Windows SmartScreen puede mostrar `Editor desconocido`. Eso no significa que falte una dependencia. Para distribucion institucional se debe firmar el instalador con un certificado de firma de codigo.

## IA

- El envío SMTP fue retirado. Las variables `BIOVIC_SMTP_*` no son necesarias.
- Ollama en `localhost` no es accesible desde Azure. Para conservar una instalacion sencilla en los otros PC, BioVic IA necesitara un proveedor HTTPS o un servicio de IA separado. No se instala Ollama en cada cliente.
