@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

title QueenZone News Agent smoke test

echo.
echo QueenZone News Agent — OpenRouter smoke test
echo ============================================
echo.
echo This run will:
echo   1. Seed discovery sources (if needed) and fetch feeds
echo   2. Triage candidates with OpenRouter (small budget limits apply)
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
set "WORKER=dotnet run --project src\QueenZone.NewsAgent.Worker -- discover-news"

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

echo Step 1/2: fetch discovery sources...
echo.
%WORKER% --seed-sources
set FETCH_EXIT=%ERRORLEVEL%

echo.
echo Step 2/2: triage with OpenRouter...
echo.
%WORKER% --triage-only
set TRIAGE_EXIT=%ERRORLEVEL%

echo.
if %TRIAGE_EXIT% equ 0 (
    echo OpenRouter smoke test passed.
    echo Look for "OpenRouter completed" and "Triage finished" lines above.
    echo Rejecting all candidates as NotRelevant is normal for off-topic feed items.
    if %FETCH_EXIT% neq 0 (
        echo.
        echo Note: discovery reported source fetch errors ^(exit %FETCH_EXIT%^).
        echo Scroll up for "Discovery error" lines. Triage still ran successfully.
    )
    set EXITCODE=0
) else (
    echo OpenRouter smoke test failed with triage exit code %TRIAGE_EXIT%.
    set EXITCODE=%TRIAGE_EXIT%
)

goto :end

:fail
set EXITCODE=1

:end
echo.
pause
exit /b %EXITCODE%
