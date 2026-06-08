@echo off
title PRN222 RAG App
echo ====================================================
echo  PRN222 RAG App - Starting All Services
echo ====================================================

:: Free up Port 5000 if occupied
echo [0/2] Checking port 5000...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5000 ^| findstr LISTENING 2^>nul') do (
    echo       Killing stale process on port 5000 (PID %%a)...
    taskkill /f /pid %%a >nul 2>&1
)

echo.
echo [1/2] Better Auth  ^| http://localhost:5000
echo [2/2] .NET GUI     ^| http://localhost:5155
echo ====================================================
echo.

:: Run both services concurrently with labeled, colorized output
cd better-auth
npx concurrently ^
  --names "AUTH,.NET" ^
  --prefix-colors "cyan,green" ^
  --prefix "[{name}]" ^
  --kill-others-on-fail ^
  "npm run dev" ^
  "dotnet run --project ../GUI/GUI.csproj"
