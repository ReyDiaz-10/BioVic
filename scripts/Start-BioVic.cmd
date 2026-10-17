@echo off
if not exist "%~dp0api\.env" copy "%~dp0api\.env.example" "%~dp0api\.env" >nul
start "BioVic API" "%~dp0api\BioVic.Api.exe"
timeout /t 3 /nobreak >nul
start "BioVic" "%~dp0desktop\BioVic.exe"
