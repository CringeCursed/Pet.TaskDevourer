@echo off
setlocal ENABLEDELAYEDEXPANSION

set CFG=%1
if "%CFG%"=="" set CFG=Debug
if /I not "%CFG%"=="Debug" if /I not "%CFG%"=="Release" (
  echo Invalid configuration "%CFG%". Use Debug or Release.
  exit /b 1
)

echo Configuration: %CFG%

set API_EXE=Server\Pet.TaskDevourer.Api\bin\%CFG%\net8.0\Pet.TaskDevourer.Api.exe
set APP_EXE=Pet.TaskDevourer\bin\%CFG%\net8.0-windows\Pet.TaskDevourer.exe

if not exist "%API_EXE%" ( set NEED_BUILD=1 )
if not exist "%APP_EXE%" ( set NEED_BUILD=1 )

if defined NEED_BUILD (
  echo Binaries not found. Building solution (%CFG%)...
  dotnet build -c %CFG% > build.log
  if errorlevel 1 (
    echo Build failed. See build.log
    exit /b 1
  ) else (
    echo Build succeeded.
  )
)

if not exist "%API_EXE%" (
  echo API executable still not found: %API_EXE%
  exit /b 1
)
if not exist "%APP_EXE%" (
  echo App executable still not found: %APP_EXE%
  exit /b 1
)

powershell -NoLogo -NoProfile -Command "try { (New-Object Net.Sockets.TcpClient).Connect('localhost',5005); 'API UP' } catch { 'API DOWN' }" | find "API UP" >nul
if %errorlevel%==0 (
  echo API already running on port 5005.
) else (
  echo Starting API: %API_EXE%
  start "API" "%API_EXE%"
  timeout /t 1 >nul
)

echo Starting WPF client: %APP_EXE%
start "APP" "%APP_EXE%"

echo Started. Close windows to exit.
endlocal
