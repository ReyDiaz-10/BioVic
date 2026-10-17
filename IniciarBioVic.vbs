Set WshShell = CreateObject("WScript.Shell")
Dim currentPath
currentPath = CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName)

' 1. Iniciar la API de forma 100% oculta (El '0' oculta la ventana)
WshShell.Run "dotnet run --project """ & currentPath & "\src\BioVic.Api""", 0, False

' 2. Esperar 3 segundos a que la base de datos y la API inicien en el puerto 5180
WScript.Sleep 3000

' 3. Iniciar la aplicación de escritorio también de forma oculta (El '0' evita la consola de dotnet run, mostrando solo tu interfaz WPF)
WshShell.Run "dotnet run --project """ & currentPath & "\src\BioVic.Desktop""", 0, False