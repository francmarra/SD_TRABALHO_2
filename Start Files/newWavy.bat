@echo off
echo Starting New Continental Wavy Sensor...
echo Usage: newWavy.bat [WavyId]
echo Example: newWavy.bat EU-Wavy01
echo Available: EU-Wavy01, EU-Wavy02, NA-Wavy01, SA-Wavy02, etc.

if "%1"=="" (
    echo Default: Starting EU-Wavy01
    start cmd /k "cd %~dp0\..\Wavy && dotnet run EU-Wavy01"
) else (
    echo Starting %1
    start cmd /k "cd %~dp0\..\Wavy && dotnet run %1"
)