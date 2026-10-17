# Pruebas del incremento de seguridad y ubicaciones

## 1. Respaldo previo

No elimine ni sustituya `.env`, la base `biovic.db`, `documents`, `photos` ni `backups`.

Desde la raíz del proyecto, localice la base activa y copie cada resultado a una carpeta segura:

```powershell
Get-ChildItem . -Filter biovic.db -Recurse | Select-Object FullName, Length, LastWriteTime
```

Detenga BioVic.Api antes de copiar la base. El primer inicio posterior al reemplazo ejecutará automáticamente la migración `LocationAccessAndNotifications`. Los usuarios existentes conservarán inicialmente acceso a todas las ubicaciones.

## 2. Compilación

```powershell
dotnet restore BioVic.sln
dotnet build BioVic.sln -c Debug
dotnet test BioVic.sln -c Debug
```

Después ejecute:

```powershell
.\scripts\run-dev.ps1
```

Resultado esperado: la aplicación debe exigir inicio de sesión antes de mostrar el inventario.

## 3. Ubicaciones y permisos

1. Ingrese como Administrador Maestro.
2. Abra **Administrador Maestro > Ubicaciones**.
3. Cree dos ubicaciones de prueba: `Sede A · UCI` y `Sede B · Urgencias`.
4. Modifique una ubicación e indique el motivo.
5. Cree un usuario Consulta autorizado solo para Sede A.
6. Cree un usuario Editor autorizado solo para Sede A.
7. Cierre sesión e ingrese con cada cuenta. Ambos deben ver únicamente equipos de Sede A.
8. Como Editor, cree un equipo seleccionando Sede B. Guardar debe funcionar, pero el equipo no debe aparecer luego en su inventario.
9. Como Administrador Maestro, confirme que el equipo sí existe y que la creación quedó registrada en Auditoría.

## 4. Eliminación de una ubicación

1. Cree o seleccione una ubicación que tenga por lo menos un equipo.
2. Elimínela desde el panel maestro e indique el motivo.
3. Confirme el mensaje preventivo.
4. Abra **Bandeja BioVic IA**: debe aparecer un mensaje por cada equipo afectado.
5. Posponer un mensaje debe ocultarlo hasta la fecha seleccionada sin eliminarlo.
6. **Marcar atendido** debe ser rechazado mientras el equipo siga sin ubicación.
7. Pulse **Abrir / corregir equipo**, seleccione una ubicación activa y guarde con motivo.
8. El mensaje debe resolverse y el equipo debe volver a mostrar su nueva ubicación.

La eliminación es lógica: la ubicación, las relaciones históricas y la auditoría se conservan.

## 5. QR protegido

Para probar desde otro dispositivo de la red, configure una dirección alcanzable; `localhost` no funciona desde el teléfono:

```env
BIOVIC_API_URLS=http://0.0.0.0:5180
BIOVIC_QR_BASE_URL=http://IP-DEL-PC:5180/e
```

1. Reinicie completamente la API.
2. Genere el QR de un equipo con una cuenta Editor o Administrador Maestro.
3. Escanéelo en una ventana privada del navegador.
4. Antes de iniciar sesión solo debe aparecer el formulario de acceso, sin información del equipo.
5. Una cuenta sin permiso para esa ubicación debe recibir el aviso de acceso denegado.
6. Una cuenta autorizada debe ver la ficha y descargar el PDF.
7. Compruebe en Auditoría los ingresos, la consulta QR y la descarga.

En producción use HTTPS; no exponga el puerto de BioVic directamente a Internet.

## 6. PDF

1. Abra un equipo con registros de mantenimiento, calibración, fallas e incidentes.
2. Pulse **Descargar PDF**.
3. Verifique que el documento abra y contenga identificación, ubicación, datos técnicos disponibles, mantenimientos, calibraciones, tecnovigilancia e historial de estados.
4. Repita la descarga desde el portal QR con una cuenta autorizada.

## 7. Correo

Confirme que la interfaz no muestre la opción **Enviar por correo** y que la ruta `POST /api/equipment/{id}/email` no exista. La descarga del PDF debe continuar funcionando.

## 8. Criterio final

El incremento se considera aprobado cuando compila, las pruebas automatizadas terminan sin errores y se cumplen los casos de acceso, ubicación, QR, PDF y auditoría anteriores.
