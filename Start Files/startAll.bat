@echo off
REM Run this to start all continent-based components simultaneously

echo Starting Continent-Based System...
echo Available Continents: EU, NA, SA, AF, AS, OC, AQ

start cmd /k "cd %~dp0\..\Servidor && echo Starting Central Server... && dotnet run EU-S"

timeout /t 2 /nobreak > nul

start cmd /k "cd %~dp0\..\Agregador && echo Starting Europe Aggregator... && dotnet run EU-Agr01"

timeout /t 2 /nobreak > nul

start cmd /k "cd %~dp0\..\Wavy && echo Starting Europe Wavy01... && dotnet run EU-Wavy01"

start cmd /k "cd %~dp0\..\Wavy && echo Starting Europe Wavy02... && dotnet run EU-Wavy02"

echo All components started!
echo Use 'DLG' command in any terminal to shutdown gracefully.
pause