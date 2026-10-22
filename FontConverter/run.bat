@echo off
chcp 65001 > nul
cd /d "%~dp0"
dotnet run --project FontConverter.csproj
pause
