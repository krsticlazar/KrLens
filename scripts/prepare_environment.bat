@echo off
setlocal

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo Pokrecem prepare_environment.bat sa administratorskim pravima...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "SERVER_ROOT=%ROOT%\src\KrLensServer"
set "CLIENT_ROOT=%ROOT%\src\KrLensClient"
set "PATH=%ProgramFiles%\dotnet;%ProgramFiles%\nodejs;%PATH%"

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
  where winget >nul 2>&1
  if %errorlevel% neq 0 (
    echo Dotnet SDK nije pronadjen, a winget nije dostupan za automatsku instalaciju.
    exit /b 1
  )

  echo Instaliram .NET 8 SDK...
  winget install --id Microsoft.DotNet.SDK.8 --source winget --accept-source-agreements --accept-package-agreements
)

where npm >nul 2>&1
if %errorlevel% neq 0 (
  where winget >nul 2>&1
  if %errorlevel% neq 0 (
    echo Node.js i npm nisu pronadjeni, a winget nije dostupan za automatsku instalaciju.
    exit /b 1
  )

  echo Instaliram Node.js LTS...
  winget install --id OpenJS.NodeJS.LTS --source winget --accept-source-agreements --accept-package-agreements
)

echo.
echo Obnavljam backend zavisnosti...
pushd "%SERVER_ROOT%"
dotnet restore "KrLensServer.sln"
if %errorlevel% neq 0 (
  popd
  exit /b %errorlevel%
)
popd

echo.
echo Instaliram frontend pakete...
pushd "%CLIENT_ROOT%"
call npm install
if %errorlevel% neq 0 (
  popd
  exit /b %errorlevel%
)
popd

echo.
echo Okruzenje je spremno.
exit /b 0
