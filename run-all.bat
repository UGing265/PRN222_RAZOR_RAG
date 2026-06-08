@echo off
title PRN222 RAG App Launcher
echo ====================================================
echo Starting PRN222 RAG App Services...
echo ====================================================

:: Free up Port 5000 if it is already in use
echo Checking if port 5000 is occupied...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5000 ^| findstr LISTENING') do (
    echo Freeing up port 5000, killing PID %%a...
    taskkill /f /pid %%a >nul 2>&1
)

:: Start Better Auth Hono Service in a separate, dedicated terminal window
echo [1/2] Starting Better Auth Hono Server on Port 5000 (in a new window)...
start "Better Auth Server (Port 5000)" cmd /k "cd better-auth && npm run dev"

:: Start .NET GUI in the current console window
echo [2/2] Starting .NET Razor Pages GUI (in this window)...
echo.
dotnet run --project GUI/GUI.csproj

pause
