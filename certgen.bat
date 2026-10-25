@REM translated from certgen.sh by chatgpt
@REM certgen.bat password
@REM Generate a development certificate from a temporary docker container at .\cert\WebApp.pfx. 
@echo off
setlocal

if "%~1"=="" (
    echo Usage: %~nx0 ^<certificate-password^>
    exit /b 1
)

if not exist "cert\" mkdir "cert"

docker run --name certgen -v "%cd%\cert:C:\app_cert" mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2025 powershell -Command "dotnet dev-certs https -ep C:\app_cert\WebApp.pfx -p '%~1'"

set "RESULT=%ERRORLEVEL%"

docker rm -f certgen >nul 2>&1

exit /b %RESULT%
