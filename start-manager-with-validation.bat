@echo off
title Distributed System Manager Launcher
color 0a

echo.
echo ========================================
echo   🖥️  DISTRIBUTED SYSTEM MANAGER  🖥️
echo ========================================
echo.

echo 🔍 Running system validation...
echo.

powershell -ExecutionPolicy Bypass -File validate-system.ps1

echo.
echo ⚡ Starting Electron Manager...
echo   Press Ctrl+C to stop
echo.

cd /d "%~dp0"
npm start

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ Error starting manager. Check the output above.
    echo.
    echo 🔧 Try these solutions:
    echo    1. Run: npm install
    echo    2. Run: npm run build-cs
    echo    3. Check that MongoDB and RabbitMQ are running
    echo.
)

pause
