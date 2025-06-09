# Project Structure Documentation

This document provides a detailed explanation of the project structure, describing the purpose and functionality of each directory and important files in the Distributed Sensor Data Management System.

## Root Directory Overview

```
SD_TRABALHO_2/
├── 📄 Core Project Files
│   ├── SD_TRABALHO_2.sln          # Visual Studio solution file
│   ├── README.md                  # Main project documentation
│   ├── SETUP.md                   # Installation and setup guide
│   ├── PROJECT-STRUCTURE.md       # This file - project structure documentation
│   └── LICENSE                    # Project license file
│
├── 🖥️ Electron Management Interface
│   ├── package.json               # Node.js dependencies and scripts
│   ├── package-lock.json          # Locked dependency versions
│   ├── main.js                    # Electron main process
│   ├── renderer.js                # Electron renderer process (UI logic)
│   ├── index.html                 # Management interface HTML
│   └── node_modules/              # Node.js dependencies (auto-generated)
│
├── 🚀 Startup Scripts
│   ├── setup-first-time.bat       # Initial system setup script
│   ├── start-manager.bat          # Launch Electron interface
│   ├── start-manager-with-validation.bat  # Launch with system validation
│   └── validate-system.ps1        # PowerShell validation script
│
├── 🏗️ C# Applications
│   ├── Agregador/                 # Continental aggregator application
│   ├── Servidor/                  # Central server application
│   ├── Wavy/                      # Sensor simulation application
│   ├── ConfigImporter/            # Database migration utility
│   └── Shared/                    # Common libraries and models
│
├── 📁 Configuration & Utilities
│   ├── Config/                    # Legacy CSV configuration files
│   └── Start Files/               # Batch scripts for component startup
│
└── 🔧 Build Artifacts
    └── */bin/, */obj/             # Compiled binaries and build cache
```

## Core Applications

### 1. Agregador/ - Continental Aggregator Application

**Purpose**: Continental data collection and processing hubs that receive sensor data and forward it to the central server.

```
Agregador/
├── Agregador.cs           # Main application logic (renamed from Program.cs)
├── Agregador.csproj       # Project configuration and dependencies
├── Agregador.sln          # Individual solution file
├── bin/Debug/net9.0/      # Compiled executable and dependencies
└── obj/                   # Build cache and intermediate files
```

**Key Functionality**:
- **Data Collection**: Receives sensor readings from multiple Wavy instances via RabbitMQ
- **Continental Processing**: Aggregates data by continent (EU, NA, SA, AF, AS, OC, AQ)
- **Message Forwarding**: Sends processed data to central Servidor via RabbitMQ
- **Database Integration**: Reads configuration from MongoDB using async `ConfigService`
- **Graceful Shutdown**: Responds to `DLG` command for clean termination

**Entry Point**: `dotnet run <AggregatorId>` (e.g., `dotnet run EU-Agr01`)

**Dependencies**:
- Shared library for models and services
- RabbitMQ.Client for message queuing
- MongoDB driver for configuration management

### 2. Servidor/ - Central Server Application

**Purpose**: Central data repository that receives aggregated data and stores it in MongoDB with query capabilities.

```
Servidor/
├── Servidor.cs            # Main server logic (renamed from Program.cs)
├── Servidor.csproj        # Project configuration and dependencies
├── Servidor.sln           # Individual solution file
├── bin/Debug/net9.0/      # Compiled executable and dependencies
└── obj/                   # Build cache and intermediate files
```

**Key Functionality**:
- **Data Ingestion**: Receives aggregated sensor data via RabbitMQ
- **MongoDB Storage**: Persists all sensor readings to `readings` collection
- **Real-time Processing**: Handles continuous data streams asynchronously
- **Query Interface**: Provides data access methods for future API development
- **No File Logging**: Removed file-based storage in favor of MongoDB-only persistence

**Entry Point**: `dotnet run` (no parameters needed)

**Database Collections Used**:
- `readings` - Stores incoming sensor data
- `aggregated` - Stores processed continental summaries

### 3. Wavy/ - Sensor Simulation Application

**Purpose**: Simulates IoT environmental sensors that generate realistic sensor data for the system.

```
Wavy/
├── Wavy.cs                # Main sensor simulation logic (renamed from Program.cs)
├── Wavy.csproj            # Project configuration and dependencies
├── Wavy.sln               # Individual solution file
├── bin/Debug/net9.0/      # Compiled executable and dependencies
└── obj/                   # Build cache and intermediate files
```

**Key Functionality**:
- **Data Generation**: Creates realistic temperature, humidity, and CO2 readings
- **Continental Identification**: Associates data with geographic continents (EU, NA, SA, AF, AS, OC, AQ)
- **RabbitMQ Publishing**: Sends sensor readings to continental aggregators
- **Database Configuration**: Reads sensor parameters from MongoDB async operations
- **Status Management**: Updates sensor status in database (Active/Inactive)

**Entry Point**: `dotnet run <SensorId>` (e.g., `dotnet run EU-Wavy01`)

**Data Generated**:
- Temperature: 15-30°C with realistic variations
- Humidity: 40-80% with weather patterns
- CO2 Levels: 350-1000 ppm based on environmental conditions

### 4. ConfigImporter/ - Database Migration Utility

**Purpose**: Imports CSV configuration files into MongoDB and provides database testing utilities.

```
ConfigImporter/
├── ConfigImporter.cs      # Main import logic (renamed from Program.cs)
├── TestConfigService.cs   # Database connectivity testing utility
├── ConfigImporter.csproj  # Project configuration and dependencies
├── bin/Debug/net9.0/      # Compiled executable and dependencies
└── obj/                   # Build cache and intermediate files
```

**Key Functionality**:
- **CSV Import**: Migrates continent-based configuration files to MongoDB collections
- **Data Validation**: Ensures configuration data integrity during import
- **Database Testing**: Provides connectivity and CRUD operation testing
- **One-time Setup**: Typically run once during initial system setup

**Usage**:
```powershell
# Import configurations
dotnet run

# Test database connectivity
dotnet run TestConfigService
```

**Collections Created**:
- `ConfigAgr` - 7 aggregator configurations (EU-Agr01, NA-Agr01, SA-Agr01, AF-Agr01, AS-Agr01, OC-Agr01, AQ-Agr01)
- `ConfigWavy` - 14 sensor configurations (2 per continent)

## Shared Libraries

### 5. Shared/ - Common Libraries and Models

**Purpose**: Contains shared code, models, and services used across all applications.

```
Shared/
├── Shared.csproj               # Shared library project configuration
├── Models/                     # Data models and DTOs
│   ├── ConfigAgr.cs            # Aggregator configuration model
│   ├── ConfigWavy.cs           # Wavy sensor configuration model
│   ├── WavyMessage.cs          # Sensor data message structure
│   ├── AggregatedData.cs       # Aggregated data structure
│   ├── RpcRequest.cs           # RPC request model
│   └── RpcResponse.cs          # RPC response model
├── MongoDB/                    # Database services and configuration
│   ├── MongoDBConfig.cs        # Database connection settings
│   ├── MongoDBService.cs       # Core database operations
│   └── ConfigService.cs        # Configuration management service
├── RabbitMQ/                   # Message queue services
│   ├── RabbitMQConfig.cs       # RabbitMQ connection settings
│   ├── RabbitMQPublisher.cs    # Message publishing service
│   ├── RabbitMQSubscriber.cs   # Message consumption service
│   ├── RabbitMQRpcClient.cs    # RPC client implementation
│   └── RabbitMQRpcServer.cs    # RPC server implementation
├── bin/Debug/net9.0/           # Compiled shared library
└── obj/                        # Build cache
```

#### Models/ - Data Models

**ConfigAgr.cs**: Aggregator configuration model with MongoDB annotations
```csharp
public class ConfigAgr
{
    [BsonId] public string Id { get; set; }      // e.g., "EU-Agr01"
    public string Continent { get; set; }        // e.g., "Europe"
    public int Port { get; set; }                // Communication port
    public string QueueName { get; set; }        // RabbitMQ queue name
}
```

**ConfigWavy.cs**: Wavy sensor configuration model
```csharp
public class ConfigWavy
{
    [BsonId] public string Id { get; set; }      // e.g., "EU-Wavy01" 
    public string Continent { get; set; }        // e.g., "Europe"
    public bool IsActive { get; set; }           // Sensor status
    public int Interval { get; set; }            // Data generation interval
}
```

**WavyMessage.cs**: Sensor data message format
```csharp
public class WavyMessage
{
    public string SensorId { get; set; }         // Unique sensor identifier
    public DateTime Timestamp { get; set; }      // Reading timestamp
    public double Temperature { get; set; }      // Temperature in Celsius
    public double Humidity { get; set; }         // Humidity percentage
    public double CO2 { get; set; }              // CO2 levels in ppm
    public string ContinentCode { get; set; }    // Geographic continent code
    public string ContinentName { get; set; }    // Geographic continent name
}
```

#### MongoDB/ - Database Services

**MongoDBConfig.cs**: Centralized database configuration
- Connection strings and database names
- Collection name constants
- Environment-specific settings

**MongoDBService.cs**: Core database operations
- Connection management
- Generic CRUD operations
- Collection access methods
- Error handling and logging

**ConfigService.cs**: Configuration-specific database operations
- `GetAgrConfigAsync(string id)` - Retrieve aggregator config
- `GetWavyConfigAsync(string id)` - Retrieve sensor config
- `UpdateWavyStatusAsync(string id, bool isActive)` - Update sensor status
- Async/await pattern for all operations

#### RabbitMQ/ - Message Queue Services

**RabbitMQConfig.cs**: Message broker configuration
- Connection parameters (hostname, port, credentials)
- Queue and exchange naming conventions
- Timeout and retry settings

**RabbitMQPublisher.cs**: Message publishing service
- Asynchronous message sending
- Connection pooling
- Error handling and retries

**RabbitMQSubscriber.cs**: Message consumption service
- Event-driven message processing
- Multiple consumer support
- Graceful shutdown handling

## Configuration and Utilities

### 6. Config/ - Legacy Configuration Files

**Purpose**: Original CSV configuration files, now migrated to MongoDB but retained for reference.

```
Config/
├── config_agr.csv                  # Legacy regional configurations
├── config_wavy.csv                 # Legacy regional configurations  
├── config_agr_continents.csv       # Continental aggregator configurations
├── config_wavy_continents.csv      # Continental sensor configurations
└── config_server_continents.csv    # Continental server configurations
```

**Status**: Legacy files are retained for reference. The system now uses continent-based configurations imported by ConfigImporter into MongoDB.

### 7. Start Files/ - Component Startup Scripts

**Purpose**: Batch scripts for quickly starting system components without the Electron interface.

```
Start Files/
├── startAll.bat           # Starts complete system (all continents)
├── newAgr.bat             # Starts new aggregator instance
└── newWavy.bat            # Starts new sensor instance
```

**Usage Examples**:
```powershell
# Start complete system
.\Start Files\startAll.bat

# Start individual components
.\Start Files\newAgr.bat EU-Agr01
.\Start Files\newWavy.bat SA-Wavy01
```

## Electron Management Interface

### 8. Management Interface Files

**Purpose**: Desktop application for monitoring and controlling all system components with a modern GUI.

**main.js**: Electron main process
- Application lifecycle management
- Window creation and management
- IPC (Inter-Process Communication) handling
- Child process spawning for C# applications

**renderer.js**: Frontend JavaScript logic
- UI event handling
- Real-time output display
- Process status monitoring
- Command input processing

**index.html**: User interface markup
- Modern terminal-style interface
- Component control buttons
- Real-time output panels
- Status indicators

**package.json**: Node.js project configuration
```json
{
  "main": "main.js",
  "scripts": {
    "start": "electron .",
    "dev": "electron . --enable-logging"
  },
  "dependencies": {
    "electron": "^36.3.1"
  }
}
```

## Build System and Dependencies

### Build Artifacts (Generated Directories)

**bin/Debug/net9.0/**: Compiled executables and dependencies
- Contains `.exe` files for each application
- All required DLL dependencies
- Configuration files and runtime settings

**obj/**: Build cache and intermediate files
- NuGet package metadata
- Compilation cache
- Project dependency information

### Project Dependencies

**Common NuGet Packages**:
- `MongoDB.Driver` - MongoDB database connectivity
- `RabbitMQ.Client` - RabbitMQ message queuing
- `Microsoft.Extensions.DependencyInjection` - Dependency injection
- `Microsoft.Extensions.Logging` - Logging infrastructure
- `Newtonsoft.Json` - JSON serialization

## Data Flow Architecture

### Message Flow Diagram
```
┌──────────────┐    RabbitMQ     ┌─────────────┐    RabbitMQ     ┌─────────────┐
│ Wavy Sensors │ ──────────────▶ │ Agregadores │ ──────────────▶ │ Servidor    │
│              │  sensor_data    │             │ aggregated_data │             │
│  N_Wavy01    │     queue       │  N_Agr      │     queue       │  MongoDB    │
│  N_Wavy02    │                 │             │                 │  Storage    │
└──────────────┘                 └─────────────┘                 └─────────────┘
       ▲                               ▲                               ▲
       │                               │                               │
       │ MongoDB Config                │ MongoDB Config                │
       │                               │                               │
       ▼                               ▼                               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              MongoDB Database                               │
│     ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐     │
│     │ ConfigWavy   │ │ ConfigAgr    │ │   readings   │ │  aggregated  │     │
│     │ Collection   │ │ Collection   │ │  Collection  │ │  Collection  │     │
│     └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Configuration Flow
1. **System Startup**: Components read configuration from MongoDB via `ConfigService`
2. **Runtime**: All sensor data flows through RabbitMQ queues
3. **Storage**: Final data persistence in MongoDB `readings` collection
4. **Management**: Electron interface monitors and controls all processes

This project structure enables a scalable, maintainable distributed system with clear separation of concerns and modern development practices.
