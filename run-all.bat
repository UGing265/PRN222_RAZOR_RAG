@echo off
echo ====================================================
echo Starting PRN222 RAG App Services...
echo ====================================================

:: Free up Port 5000 if it is already in use
echo Checking if port 5000 is occupied...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5000 ^| findstr LISTENING') do (
    echo Freeing up port 5000, killing PID %%a...
    taskkill /f /pid %%a >nul 2>&1
)

:: Start Better Auth Hono Service in the background (same console window)
echo [1/2] Starting Better Auth Hono Server (Port 5000) in same window...
start /B cmd /c "cd better-auth && npm run dev"

:: Start .NET GUI in the current console window
echo [2/2] Starting .NET Razor Pages GUI in current window...
dotnet run --project GUI/GUI.csproj

pause
