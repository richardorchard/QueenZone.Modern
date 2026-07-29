@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

title QueenZone links checker

echo.
echo QueenZone links checker
echo =======================
echo.
echo This checks legacy Queen-related links and updates QueenLinkChecks.
echo The public /links page hides only links confirmed dead after repeated hard failures.
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet SDK not found. Install from https://dotnet.microsoft.com/download
    goto :fail
)

where powershell >nul 2>&1
if errorlevel 1 (
    echo ERROR: Windows PowerShell was not found.
    goto :fail
)

set "LOCAL_SETTINGS=src\QueenZone.Web\appsettings.Local.json"

if "%ConnectionStrings__QueenZoneLegacy%"=="" (
    if not exist "%LOCAL_SETTINGS%" (
        echo ERROR: No database connection string was found.
        echo.
        echo Provide one of:
        echo   1. Set ConnectionStrings__QueenZoneLegacy
        echo   2. Create %LOCAL_SETTINGS% with ConnectionStrings:QueenZoneLegacy
        echo   3. Pass -ConnectionString to scripts\Check-QueenLinks.ps1
        echo.
        echo Example:
        echo   powershell -File .\scripts\Check-QueenLinks.ps1 -ConnectionString "..."
        goto :fail
    )
)

echo Running link checker...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Check-QueenLinks.ps1" %*
set EXITCODE=%ERRORLEVEL%

echo.
if %EXITCODE% equ 0 (
    echo Link check completed.
) else (
    echo Link check failed with exit code %EXITCODE%.
)

goto :end

:fail
set EXITCODE=1

:end
echo.
pause
exit /b %EXITCODE%
