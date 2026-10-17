# Incremento 02 · QR protegido en red local

Este incremento corrige tres problemas que hacían que el QR funcionara en el computador, pero no en un teléfono:

1. La API escuchaba únicamente en `127.0.0.1`.
2. La pantalla de BioVic mostraba un enlace distinto del que realmente se codificaba en el QR.
3. Era posible generar un QR aunque su dirección no fuera accesible desde otro dispositivo.

## Configuración inicial

Conecte el computador servidor y el teléfono a la misma red privada. Desde la raíz de BioVic, abra PowerShell como Administrador y ejecute:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-qr-lan.ps1
```

El script detecta la IPv4 LAN, conserva las credenciales existentes de `.env`, configura la escucha de la API y crea una regla de Firewall limitada al perfil **Privado**, al puerto TCP `5180` y a la subred local.

Si la detección elige una interfaz incorrecta, indique la IP manualmente:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-qr-lan.ps1 -LanIpAddress 192.168.1.50
```

Después cierre cualquier instancia de BioVic y ejecute:

```powershell
.\scripts\run-dev.ps1
```

## Prueba técnica

Con BioVic abierto, ejecute en otra consola:

```powershell
.\scripts\test-qr-lan.ps1
```

La respuesta debe indicar `qrRemoteAccess : True`. Abra en el teléfono la dirección `/health` que muestra el script. Solo después de esa prueba genere nuevamente el QR del equipo; los QR antiguos que contengan `127.0.0.1` deben descartarse.

Al escanear el nuevo QR:

1. BioVic solicita usuario y contraseña.
2. Valida que la cuenta esté activa y que tenga permiso para la ubicación del equipo.
3. Muestra la ficha de solo lectura.
4. Permite descargar la hoja de vida PDF.
5. Registra la consulta y la descarga en auditoría.

## Límites de esta modalidad

La configuración `http://IP-LAN:5180` está destinada exclusivamente a una red privada confiable. No publique el puerto `5180` directamente en Internet. Para consultar desde otra ciudad se requiere una instalación central con HTTPS y DNS institucional, o una VPN privada; esa será una etapa de despliegue separada.
