# Manual de usuario

## Acceso y modo Consulta

BioVic exige usuario y contraseña antes de abrir el inventario. Después de iniciar sesión, escriba nombre, serial, modelo, código, fabricante o ubicación y presione **Buscar**. Si no hay coincidencia exacta, BioVic puede sugerir una corrección como “Monitor multiparamétrico”.

Seleccione **Ver hoja de vida** para consultar identificación, ubicación, responsable, estado, próximos mantenimientos y el historial permitido. Solo aparecen equipos pertenecientes a las ubicaciones autorizadas para la cuenta y cada consulta queda auditada con el usuario, fecha y hora.

## Iniciar sesión

Seleccione **Iniciar sesión**, escriba usuario y contraseña. Después de varios intentos fallidos la cuenta se bloquea temporalmente. El menú y los botones cambian según el rol.

## Registrar un equipo

Disponible para Editor y Administrador Maestro:

1. Seleccione **Nuevo equipo**.
2. Complete código interno, nombre y seleccione obligatoriamente una ubicación de la lista.
3. Registre serial, marca, modelo, responsable, estado y criticidad cuando estén disponibles.
4. Si el equipo requiere calibración, active la casilla y registre la fecha.
5. Seleccione **Guardar equipo**.

BioVic evita códigos duplicados y, si la configuración lo exige, seriales duplicados.

Un Editor puede seleccionar cualquier ubicación activa al registrar el equipo. Si esa ubicación no está dentro de sus permisos de consulta, podrá completar el registro, pero ya no verá el equipo después de guardarlo.

## Registrar mantenimiento

1. Abra la hoja de vida.
2. Seleccione **Agregar mantenimiento**.
3. Complete tipo, fecha, persona/empresa y actividades.
4. Registre resultado, próximo mantenimiento y, si corresponde, estado posterior.
5. Guarde.

El mantenimiento no reemplaza registros anteriores. Si cambia el estado, BioVic crea también un evento en la línea de tiempo.

## Portal QR

La ficha genera un enlace basado en `PublicId`. Al escanearlo, el portal solicita un usuario y contraseña de BioVic antes de mostrar cualquier dato. La API vuelve a validar que esa cuenta tenga acceso a la ubicación del equipo.

La hoja de vida incluye **Generar QR**, **Imprimir QR** y **Descargar PDF**. El portal permite descargar la hoja de vida en PDF únicamente durante una sesión autenticada y autorizada. El envío directo por correo fue retirado; el usuario puede adjuntar manualmente el PDF descargado si lo necesita.

## Calibraciones y tecnovigilancia

Calibraciones registra laboratorio o profesional, certificado, resultado, trazabilidad, observaciones y próxima fecha. Tecnovigilancia separa las fallas técnicas de los incidentes o eventos, conservando impacto, acciones, investigación, reporte externo y conclusiones.

## BioVic IA

En la pestaña BioVic IA puede realizar preguntas breves sobre el equipo seleccionado. Las alertas objetivas se calculan primero mediante reglas. La respuesta es orientativa y debe confirmarse con el manual del fabricante y personal biomédico competente.

## Administrador Maestro

El panel permite crear usuarios Consulta o Editor, habilitarlos, suspenderlos o eliminarlos lógicamente, y seleccionar una o varias ubicaciones o **Todas las ubicaciones**. También permite crear, modificar y eliminar ubicaciones, consultar indicadores, revisar auditoría y verificar su integridad.

La pestaña **Bandeja BioVic IA** avisa sobre mantenimientos/calibraciones próximos o vencidos y sobre equipos sin ubicación. Los mensajes se pueden posponer. Un aviso de ubicación solo se resuelve cuando se edita el equipo y se le asigna una ubicación activa.

Si se elimina una ubicación, los equipos vinculados conservan su historia y muestran **Ubicación no disponible** hasta ser reasignados. No se destruye el inventario ni su auditoría.

## Estados vacíos y errores

Si no hay equipos, la búsqueda indica que todavía no existen resultados. BioVic muestra mensajes comprensibles; el detalle técnico se guarda en `logs` sin contraseñas ni tokens.
