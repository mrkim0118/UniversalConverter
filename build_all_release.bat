@echo off
setlocal

for %%D in (ImageConverter AudioConverter VideoConverter DocumentConverter FontConverter IntegratedConverter) do (
  echo ============================================================
  echo Building %%D
  echo ============================================================
  call "%%D\\build_release.bat"
  if errorlevel 1 exit /b 1
)

echo.
echo All installers completed successfully.
pause
