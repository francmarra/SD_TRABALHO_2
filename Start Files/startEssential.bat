@echo off
REM Arquivo limpo para iniciar apenas os componentes essenciais
echo ========================================
echo    Sistema Distribuído - RabbitMQ/MongoDB
echo ========================================
echo.

echo [1/3] Iniciando Servidor...
cd /d "%~dp0..\Servidor"
start "Servidor" cmd /k "dotnet run"
timeout /t 3 /nobreak >nul

echo [2/3] Iniciando Agregador...
cd /d "%~dp0..\Agregador"
start "Agregador" cmd /k "dotnet run"
timeout /t 3 /nobreak >nul

echo [3/3] Iniciando Wavy...
cd /d "%~dp0..\Wavy"
start "Wavy" cmd /k "dotnet run"

echo.
echo ✅ Todos os componentes principais iniciados!
echo.
pause
