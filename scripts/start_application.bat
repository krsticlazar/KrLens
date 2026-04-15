@echo off
setlocal

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo Pokrecem start_application.bat sa administratorskim pravima...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "SERVER_DIR=%ROOT%\src\KrLensServer\src\KrLensServer.API"
set "CLIENT_DIR=%ROOT%\src\KrLensClient"
set "PATH=%ProgramFiles%\dotnet;%ProgramFiles%\nodejs;%PATH%"

start "KrLens Server" cmd /k "cd /d ""%SERVER_DIR%"" && dotnet run"
start "KrLens Client" cmd /k "cd /d ""%CLIENT_DIR%"" && npm run dev"

exit /b 0
