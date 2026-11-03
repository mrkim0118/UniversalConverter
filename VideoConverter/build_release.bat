@echo off
setlocal

set APP_NAME=VideoConverter
set EXE_NAME=TechArchive Video Converter
set APP_DISPLAY=TechArchive Video Converter
set VERSION=1.0.5
set PROJECT_DIR=%~dp0
set PUBLISH_DIR=%PROJECT_DIR%publish\win-x64
set RELEASES_DIR=%PROJECT_DIR%releases
set INSTALLER_DIR=%PROJECT_DIR%installer

echo ============================================================
echo   %APP_DISPLAY% v%VERSION% - Release Build
echo ============================================================
echo.

echo [1/4] Cleaning previous build...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%INSTALLER_DIR%" rmdir /s /q "%INSTALLER_DIR%"
if not exist "%RELEASES_DIR%" mkdir "%RELEASES_DIR%"
mkdir "%INSTALLER_DIR%"
echo     Done.
echo.

echo [2/4] Publishing app (framework-dependent win-x64)...
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
echo     Done.
echo.

echo [3/4] Checking Velopack CLI...
where vpk >nul 2>nul
if errorlevel 1 (
  dotnet tool install -g vpk --version 0.0.1298
)

if errorlevel 1 (
  echo.
  echo [ERROR] Velopack CLI install failed.
  pause
  exit /b 1
)
echo     Done.
echo.

echo [4/4] Building Velopack installer and releases...
vpk pack ^
  --packId %APP_NAME% ^
  --packVersion %VERSION% ^
  --packDir "%PUBLISH_DIR%" ^
  --mainExe "%EXE_NAME%.exe" ^
  --packTitle "%APP_DISPLAY%" ^
  --framework net8-x64-desktop ^
  --icon "%PROJECT_DIR%Assets\AppIcon.ico" ^
  --outputDir "%RELEASES_DIR%"

if errorlevel 1 (
  echo.
  echo [ERROR] Velopack packaging failed.
  pause
  exit /b 1
)

copy /y "%RELEASES_DIR%\%APP_NAME%-win-Setup.exe" "%INSTALLER_DIR%\%APP_NAME%_Setup_v%VERSION%.exe" >nul
if errorlevel 1 (
  echo.
  echo [ERROR] Installer copy failed.
  pause
  exit /b 1
)

echo.
echo ============================================================
echo   Build complete!
echo   Installer: %INSTALLER_DIR%\VideoConverter_Setup_v%VERSION%.exe
echo   Releases:  %RELEASES_DIR%
echo ============================================================
echo.
pause
