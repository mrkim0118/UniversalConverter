@echo off
setlocal

set APP_NAME=UniversalConverter
set APP_DISPLAY=Universal Converter
set VERSION=1.0.2
set PROJECT_DIR=%~dp0
set PUBLISH_DIR=%PROJECT_DIR%publish\win-x64
set INSTALLER_DIR=%PROJECT_DIR%installer
set ISS_FILE=%PROJECT_DIR%setup\installer.iss
set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo ============================================================
echo   %APP_DISPLAY% v%VERSION% - Release Build
echo ============================================================
echo.

echo [1/3] Cleaning previous build...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%INSTALLER_DIR%" rmdir /s /q "%INSTALLER_DIR%"
mkdir "%INSTALLER_DIR%"
echo     Done.
echo.

echo [2/3] Publishing app (framework-dependent win-x64)...
dotnet publish "%PROJECT_DIR%%APP_NAME%.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained false ^
  -p:DebugType=none ^
  -p:DebugSymbols=false ^
  -p:CopyDebugSymbolFilesFromPackages=false ^
  -p:ReadyToRun=true ^
  -o "%PUBLISH_DIR%"

if errorlevel 1 (
  echo.
  echo [ERROR] dotnet publish failed.
  pause
  exit /b 1
)
for /r "%PUBLISH_DIR%" %%F in (*.pdb) do del /q "%%F"
for /r "%PUBLISH_DIR%" %%F in (*.pdb) do if exist "%%F" (
  echo [ERROR] Debug symbol remains in publish output: %%F
  exit /b 1
)
echo     Done.
echo.

echo [3/3] Building installer...
if not exist %ISCC% (
  echo.
  echo [SKIP] Inno Setup 6 not found.
  echo        Download: https://jrsoftware.org/isdl.php
  echo.
  echo   Portable ZIP: %PUBLISH_DIR%
  echo.
  pause
  exit /b 0
)

%ISCC% "%ISS_FILE%"
if errorlevel 1 (
  echo.
  echo [ERROR] Inno Setup compile failed.
  pause
  exit /b 1
)

echo.
echo ============================================================
echo   Build complete!
echo   Installer: %INSTALLER_DIR%\UniversalConverter_Setup_v%VERSION%.exe
echo ============================================================
echo.
pause
