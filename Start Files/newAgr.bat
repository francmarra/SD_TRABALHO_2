@echo off
echo Starting New Continental Aggregator...
echo Usage: newAgr.bat [AggregatorId]
echo Example: newAgr.bat EU-Agr01
echo Available: EU-Agr01, NA-Agr01, SA-Agr01, AF-Agr01, AS-Agr01, OC-Agr01, AQ-Agr01

if "%1"=="" (
    echo Default: Starting EU-Agr01
    start cmd /k "cd %~dp0\..\Agregador && dotnet run EU-Agr01"
) else (
    echo Starting %1
    start cmd /k "cd %~dp0\..\Agregador && dotnet run %1"
)