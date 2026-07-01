@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

title QueenZone News Agent smoke test

echo.
echo QueenZone News Agent — OpenRouter smoke test
echo ============================================
echo.
echo This run will:
echo   1. Seed discovery sources (if needed)
echo   2. Fetch recent items from configured feeds
echo   3. Triage candidates with OpenRouter (small budget limits apply)
echo.
echo One-time setup: copy
echo   src\QueenZone.NewsAgent.Worker\appsettings.Local.json.example
echo to
echo   src\QueenZone.NewsAgent.Worker\appsettings.Local.json
echo and set OpenRouter:ApiKey. Optional: ConnectionStrings:QueenZoneLegacy for a real DB.
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet SDK not found. Install from https://dotnet.microsoft.com/download
    goto :fail
)

set "LOCAL_SETTINGS=src\QueenZone.NewsAgent.Worker\appsettings.Local.json"
set "EXAMPLE=src\QueenZone.NewsAgent.Worker\appsettings.Local.json.example"

if not exist "%LOCAL_SETTINGS%" (
    if "%OPENROUTER_API_KEY%"=="" (
        echo ERROR: %LOCAL_SETTINGS% not found and OPENROUTER_API_KEY is not set.
        echo Copy %EXAMPLE% to %LOCAL_SETTINGS% and set your API key once.
        goto :fail
    )
)

if exist "%LOCAL_SETTINGS%" (
    findstr /C:"sk-or-v1-..." "%LOCAL_SETTINGS%" >nul 2>&1
    if not errorlevel 1 (
        if "%OPENROUTER_API_KEY%"=="" (
            echo ERROR: OpenRouter API key still looks like the example placeholder.
            echo Edit %LOCAL_SETTINGS% and set OpenRouter:ApiKey, or set OPENROUTER_API_KEY.
            goto :fail
        )
    )
)

echo Starting worker...
echo.

dotnet run --project src\QueenZone.NewsAgent.Worker -- discover-news --seed-sources --triage
set EXITCODE=%ERRORLEVEL%

echo.
if %EXITCODE% equ 0 (
    echo Smoke test finished. Check the log above for triage counts and any OpenRouter errors.
    echo If you see "No discovered candidates require triage", feeds had nothing new to classify.
) else (
    echo Smoke test failed with exit code %EXITCODE%.
)

goto :end

:fail
set EXITCODE=1

:end
echo.
pause
exit /b %EXITCODE%
