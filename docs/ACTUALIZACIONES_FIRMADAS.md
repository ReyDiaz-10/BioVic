# Actualizaciones firmadas de BioVic

BioVic verifica dos firmas antes de actualizar: Authenticode en el instalador de Windows y una firma RSA del manifiesto. Además compara tamaño y SHA‑256. Si cualquiera falla, la actualización se bloquea.

## Requisito externo

Para distribución real necesita un certificado de firma de código RSA confiable instalado, con clave privada, en `Cert:\CurrentUser\My`. Una compilación `-DevelopmentUnsigned` sirve solamente para pruebas locales y deshabilita las actualizaciones automáticas.

## Crear una versión real

```powershell
pwsh -NoProfile -File .\scripts\publish-client-windows.ps1 `
  -ApiUrl "https://SU-APP.azurewebsites.net" `
  -Version "1.1.0" `
  -SigningCertificateThumbprint "HUELLA_DEL_CERTIFICADO" `
  -ReleaseNotes "Documentos versionados, recuperación y portal móvil seguro."
```

El número debe aumentar en cada lanzamiento. Después publique `manifest.json` y el instalador sin modificar ninguno:

```powershell
pwsh -NoProfile -File .\scripts\deploy-signed-update-kudu.ps1 `
  -PublishSettingsPath "C:\ruta\biovic-api-prod-2026.PublishSettings"
```

Los archivos se guardan fuera de `wwwroot`, en `/home/data/biovic-updates`, para que un despliegue del API no los elimine. El cliente consulta el manifiesto al iniciar, verifica la firma incrustada en su configuración y sólo entonces ofrece instalar la versión superior.

Conserve la misma clave privada para firmar las versiones siguientes. Si debe cambiar de clave, publique primero una versión de transición firmada con la clave anterior que incorpore la nueva clave pública; de lo contrario, los clientes existentes bloquearán correctamente el cambio.
