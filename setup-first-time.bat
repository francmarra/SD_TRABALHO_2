@echo off
title First-Time Setup - Distributed System Manager
color 0b

echo.
echo ========================================
echo      🚀 FIRST-TIME SETUP WIZARD 🚀
echo ========================================
echo.
echo This script will prepare your system for the
echo Distributed System Manager.
echo.

pause

echo 📦 Step 1: Installing NPM dependencies...
echo.
npm install
if %ERRORLEVEL% NEQ 0 (
    echo ❌ NPM install failed!
    pause
    exit /b 1
)

echo.
echo 🏗️  Step 2: Building C# projects...
echo.
dotnet build SD_TRABALHO_2.sln
if %ERRORLEVEL% NEQ 0 (
    echo ❌ C# build failed!
    echo Please ensure .NET 9.0 is installed.
    pause
    exit /b 1
)

echo.
echo 🔧 Step 3: Configuration check...
echo.

echo 📝 Please ensure the following are configured:
echo.
echo    🍃 MongoDB:
echo       • Update CONNECTION_STRING in Shared\MongoDB\MongoDBConfig.cs
echo       • Start MongoDB service
echo.
echo    🐰 RabbitMQ:
echo       • Install RabbitMQ Server
echo       • Start RabbitMQ service (default: localhost:5672)
echo.

echo 🔍 Running validation...
echo.
powershell -ExecutionPolicy Bypass -File validate-system.ps1

echo.
echo ✅ Setup completed!
echo.
echo 🎯 Next steps:
echo    1. Configure MongoDB connection string
echo    2. Start MongoDB and RabbitMQ services
echo    3. Run: start-manager.bat
echo.
echo 📖 For detailed instructions, see: MANAGER_README.md
echo.

pause
