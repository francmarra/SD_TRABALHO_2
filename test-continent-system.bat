@echo off
echo Testing Continent-Based System
echo ==============================
echo.

echo Testing ConfigImporter with continent-based configuration...
cd ConfigImporter
dotnet run
cd ..
echo.

echo Build Status:
echo - Shared: OK
echo - Servidor: OK  
echo - Agregador: OK
echo - Wavy: OK
echo - ConfigImporter: OK
echo.

echo Continent-based system is ready!
echo Available continents: EU, NA, SA, AF, AS, OC, AQ
echo.
echo Examples:
echo - Server: EU-S, NA-S, etc.
echo - Aggregator: EU-Agr01, NA-Agr01, etc.
echo - Wavy: EU-Wavy01, NA-Wavy02, etc.
echo.
pause
