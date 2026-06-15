@echo off
title PRN222 RAG App
echo ====================================================
echo  PRN222 RAG App - Starting All Services
echo ====================================================
echo.

echo [*] Killing processes on ports 5000, 5155, 7065...
for %%P in (5000 5155 7065) do (
    for /f "tokens=5" %%i in ('netstat -ano ^| findstr ":%%P " ^| findstr "LISTENING"') do (
        echo     Killing PID %%i on port %%P
        taskkill /PID %%i /F >nul 2>&1
    )
)
echo [*] Done. Starting services...
echo.

echo [1/2] Better Auth  : http://localhost:5000
echo [2/2] .NET GUI     : https://localhost:7065
echo ====================================================
echo.

cd /d "%~dp0better-auth"
npx concurrently --names "AUTH,.NET" --prefix-colors "cyan,green" --prefix "[{name}]" --kill-others-on-fail "npm run dev" "dotnet run --project ../GUI/GUI.csproj"
