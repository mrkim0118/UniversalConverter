@echo off
setlocal
cd /d "%~dp0"
dotnet restore Converters.sln
if errorlevel 1 exit /b 1
dotnet build Converters.sln --no-restore -m:1
pause
