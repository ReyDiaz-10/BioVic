# Incremento 01 · Ejecución coherente y diagnóstico PDF

Este incremento evita que BioVic Desktop se conecte accidentalmente a una API antigua y comprueba la capacidad de generar PDF antes de declarar saludable el servidor.

## Protecciones incorporadas

1. `run-dev.ps1` detiene el inicio si el puerto 5180 ya está ocupado y muestra el PID y el comando responsable.
2. La solución completa se compila con `--no-incremental` antes de iniciar los procesos.
3. La API se ejecuta con `--no-build`, por lo que utiliza exactamente los binarios que acaban de compilarse.
4. Cada ejecución recibe un identificador aleatorio. El script solo abre BioVic Desktop si `/health` devuelve ese mismo identificador.
5. La API crea en memoria un PDF de diagnóstico al arrancar. La prueba valida resolución de fuentes, alineación `TopLeft` y serialización `%PDF-`.
6. Si el diagnóstico PDF falla, la API no inicia y escribe la excepción en su registro técnico.

## Instalación del parche

1. Cierre BioVic Desktop y toda consola que esté ejecutando BioVic.Api.
2. Reemplace únicamente los archivos entregados en el parche.
3. Conserve su archivo `.env`; el parche solo incorpora `.env.example` como plantilla segura.
4. Ejecute desde la raíz del proyecto:

```powershell
dotnet clean BioVic.sln
dotnet build BioVic.sln -c Debug --no-incremental
dotnet test BioVic.sln -c Debug --no-build
.\scripts\run-dev.ps1
```

## Resultado esperado

Mientras BioVic esté abierto, en otra consola:

```powershell
Invoke-RestMethod http://127.0.0.1:5180/health
```

La respuesta debe indicar:

- `status`: `healthy`
- `pdf`: `ready`
- `startupInstance`: un identificador distinto de `manual`
- `processId`: PID de la API activa

Después pruebe **Descargar PDF** desde la hoja de vida. El registro no debe contener nuevos errores de fuente Arial ni de `XLineAlignment.BaseLine`.

## Mensaje de puerto ocupado

Si el script informa que el puerto 5180 está ocupado, no continúe contra esa instancia. Revise el PID mostrado, cierre la ejecución anterior de BioVic.Api y vuelva a ejecutar el script. Esta detención es intencional y evita reutilizar DLL antiguas.

## Registros

Los errores de arranque quedan en:

```text
src\BioVic.Api\bin\Debug\net10.0\logs\biovic-AAAA-MM-DD.log
```

Las carpetas `bin`, `obj`, `logs`, `data` y el archivo `.env` no deben incluirse en paquetes de actualización de código fuente.
