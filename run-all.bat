@echo off
title PRN222 RAG App
echo ====================================================
echo  PRN222 RAG App - Starting All Services
echo ====================================================

echo [0/2] Checking port 5000...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0kill-port.ps1"

echo.
echo [1/2] Better Auth  : http://localhost:5000
echo [2/2] .NET GUI     : http://localhost:5155
echo ====================================================
echo.

cd /d "%~dp0better-auth"
npx concurrently --names "AUTH,.NET" --prefix-colors "cyan,green" --prefix "[{name}]" --kill-others-on-fail "npm run dev" "dotnet run --project ../GUI/GUI.csproj"
