$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
dotnet restore (Join-Path $projectRoot "BioVic.sln")
dotnet build (Join-Path $projectRoot "BioVic.sln") -c Debug --no-restore
dotnet test (Join-Path $projectRoot "BioVic.sln") -c Debug --no-build
