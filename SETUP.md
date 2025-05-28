# Setup and Installation Guide

This guide provides step-by-step instructions for setting up and running the Distributed Sensor Data Management System on Windows.

## Prerequisites

### Required Software

1. **Microsoft .NET 9.0 SDK or later**
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **Node.js 18.0 or later**
   - Download from: https://nodejs.org/
   - Verify installation: `node --version` and `npm --version`

3. **MongoDB Server 5.0 or later**
   - Download from: https://www.mongodb.com/try/download/community
   - Default installation creates MongoDB service
   - Verify: MongoDB Compass or `mongo` command

4. **RabbitMQ Server 3.12 or later**
   - Download from: https://www.rabbitmq.com/download.html
   - Install with default settings
   - Management plugin is automatically enabled
   - Verify: Access http://localhost:15672 (guest/guest)

5. **Git for Windows** (optional but recommended)
   - Download from: https://git-scm.com/download/win

### System Requirements
- **OS**: Windows 10/11 (64-bit)
- **RAM**: Minimum 4GB, Recommended 8GB+
- **Storage**: 2GB free space
- **Network**: Internet connection for initial setup

## Installation Steps

### 1. Clone the Repository

```powershell
# Clone the repository
git clone <repository-url>
cd SD_TRABALHO_2

# Or download and extract ZIP file if not using Git
```

### 2. Install Dependencies

#### Install Electron Dependencies
```powershell
# Install Node.js dependencies for the management interface
npm install
```

#### Build C# Projects
```powershell
# Build all C# projects
dotnet build SD_TRABALHO_2.sln

# Or build individual projects
dotnet build .\Agregador\Agregador.csproj
dotnet build .\Wavy\Wavy.csproj
dotnet build .\Servidor\Servidor.csproj
dotnet build .\Shared\Shared.csproj
dotnet build .\ConfigImporter\ConfigImporter.csproj
```

### 3. Configure Database Connection

#### MongoDB Configuration
1. **Open MongoDB configuration file**:
   ```powershell
   notepad .\Shared\MongoDB\MongoDBConfig.cs
   ```

2. **Update connection settings** (if needed):
   ```csharp
   public static class MongoDBConfig
   {
       // Update these if using non-default MongoDB settings
       public const string CONNECTION_STRING = "mongodb://localhost:27017";
       public const string DATABASE_NAME = "SensorDataDB";
       
       // Collection names (already configured)
       public const string READINGS_COLLECTION = "readings";
       public const string AGGREGATED_COLLECTION = "aggregated";
       public const string CONFIG_AGR_COLLECTION = "ConfigAgr";
       public const string CONFIG_WAVY_COLLECTION = "ConfigWavy";
   }
   ```

#### RabbitMQ Configuration
1. **Verify RabbitMQ is running**:
   - Open browser: http://localhost:15672
   - Login: username `guest`, password `guest`
   - You should see the RabbitMQ Management interface

2. **RabbitMQ settings** (default configuration):
   ```csharp
   // Located in: .\Shared\RabbitMQ\RabbitMQConfig.cs
   public const string HOSTNAME = "localhost";
   public const int PORT = 5672;
   public const string USERNAME = "guest";
   public const string PASSWORD = "guest";
   ```

### 4. Import Configuration Data

The system requires configuration data to be imported into MongoDB:

```powershell
# Navigate to ConfigImporter directory
cd .\ConfigImporter

# Run the configuration importer
dotnet run

# You should see output confirming successful import:
# "✅ Imported 4 aggregator configurations"
# "✅ Imported 8 wavy configurations"
```

### 5. Verify Installation

#### Quick System Validation
```powershell
# Run the system validation script
.\validate-system.ps1
```

#### Manual Verification Steps

1. **Test MongoDB Connection**:
   ```powershell
   cd .\ConfigImporter
   dotnet run TestConfigService
   ```

2. **Test C# Project Builds**:
   ```powershell
   # Each should build without errors
   dotnet build .\Servidor\Servidor.csproj
   dotnet build .\Agregador\Agregador.csproj
   dotnet build .\Wavy\Wavy.csproj
   ```

3. **Test Electron Interface**:
   ```powershell
   npm start
   ```

## Running the System

### Option 1: Electron Management Interface (Recommended)

#### Start the Management Interface
```powershell
# Launch the Electron management interface
npm start

# Alternative startup scripts
.\start-manager.bat
.\start-manager-with-validation.bat
```

#### Using the Interface
1. **Start Central Server** - Click "Start Server" or use `Ctrl+Shift+S`
2. **Deploy Regions** - Click individual region buttons (North, South, East, West)
3. **Full System** - Click "Start All" to deploy complete 4-region system
4. **Monitor Processes** - Watch real-time output in the terminal panels
5. **Send Commands** - Use the command input to send `DLG` for graceful shutdown

### Option 2: Manual Component Startup

#### Start Infrastructure Services
```powershell
# Ensure MongoDB is running (usually auto-starts as Windows service)
# Check: Services.msc → "MongoDB Server"

# Ensure RabbitMQ is running (usually auto-starts as Windows service)
# Check: Services.msc → "RabbitMQ"
```

#### Start Core Components

1. **Central Server** (Terminal 1):
   ```powershell
   cd .\Servidor
   dotnet run
   ```

2. **Regional Aggregator** (Terminal 2):
   ```powershell
   cd .\Agregador
   dotnet run N_Agr  # North region aggregator
   ```

3. **Wavy Sensors** (Terminal 3 & 4):
   ```powershell
   cd .\Wavy
   dotnet run N_Wavy01  # North region sensor 1
   
   # In another terminal
   cd .\Wavy
   dotnet run N_Wavy02  # North region sensor 2
   ```

### Option 3: Batch File Quick Start

#### Use Provided Batch Files
```powershell
# Start all components automatically
.\Start Files\startAll.bat

# Start new aggregator
.\Start Files\newAgr.bat

# Start new wavy sensor
.\Start Files\newWavy.bat
```

## System Configuration

### Regional Setup

The system supports four geographic regions:

- **North Region**: `N_Agr`, `N_Wavy01`, `N_Wavy02`
- **South Region**: `S_Agr`, `S_Wavy01`, `S_Wavy02`
- **East Region**: `E_Agr`, `E_Wavy01`, `E_Wavy02`
- **West Region**: `W_Agr`, `W_Wavy01`, `W_Wavy02`

### Component Naming Convention

#### Aggregators
- Format: `<Region>_Agr`
- Examples: `N_Agr`, `S_Agr`, `E_Agr`, `W_Agr`

#### Wavy Sensors
- Format: `<Region>_Wavy<NN>`
- Examples: `N_Wavy01`, `S_Wavy02`, `E_Wavy01`, `W_Wavy02`

### Database Collections

After running ConfigImporter, MongoDB will contain:

1. **ConfigAgr Collection** - Aggregator configurations (4 documents)
2. **ConfigWavy Collection** - Wavy sensor configurations (8 documents)
3. **readings Collection** - Real-time sensor data (auto-created)
4. **aggregated Collection** - Processed regional data (auto-created)

## Usage Tutorial

### Basic Operations

#### 1. Starting a Complete Region
```powershell
# Using Electron Interface:
# 1. Click "Start Server" (if not already running)
# 2. Click "North Region" button
# 3. Watch terminal output for successful startup
# 4. Verify green status indicators

# Manual approach:
# Terminal 1: dotnet run (in Servidor directory)
# Terminal 2: dotnet run N_Agr (in Agregador directory)
# Terminal 3: dotnet run N_Wavy01 (in Wavy directory)
# Terminal 4: dotnet run N_Wavy02 (in Wavy directory)
```

#### 2. Monitoring System Health
- **Process Status**: Green = Running, Red = Stopped, Yellow = Starting
- **Real-time Output**: Monitor data flow and messages
- **RabbitMQ Web UI**: http://localhost:15672 for queue monitoring
- **MongoDB Compass**: Connect to view database contents

#### 3. Graceful Shutdown
```powershell
# Send DLG command to any component for graceful shutdown
# In Electron interface: Type "DLG" in command input and press Enter
# Manual: Type "DLG" in any component's console window
```

### Advanced Configuration

#### Adding New Regions
1. **Create configurations** in MongoDB:
   ```javascript
   // In MongoDB Compass or mongo shell
   db.ConfigAgr.insertOne({
     "_id": "newRegion_Agr",
     "Region": "NewRegion", 
     "Port": 5000,
     "QueueName": "newregion_queue"
   });
   ```

2. **Update component code** to recognize new region identifiers

#### Scaling Components
- **Multiple Aggregators**: Run `dotnet run N_Agr1`, `dotnet run N_Agr2` for load balancing
- **Additional Sensors**: Add more Wavy instances with sequential numbering
- **Server Clustering**: Deploy multiple Servidor instances with load balancing

### Monitoring and Debugging

#### Log Locations
- **Component Logs**: Console output visible in Electron interface
- **MongoDB Logs**: Check MongoDB log files in installation directory
- **RabbitMQ Logs**: Check RabbitMQ log files in installation directory

#### Common Monitoring Tasks
1. **Check Data Flow**: Monitor RabbitMQ queues for message activity
2. **Database Growth**: Monitor MongoDB collections for data accumulation
3. **Component Health**: Watch for error messages or connection failures
4. **Performance**: Monitor CPU and memory usage of components

## Troubleshooting

### Common Issues

#### MongoDB Connection Problems
```powershell
# Check MongoDB service status
Get-Service -Name "MongoDB*"

# Start MongoDB service if stopped
Start-Service -Name "MongoDB"

# Test connection
mongo --eval "db.adminCommand('ismaster')"
```

#### RabbitMQ Connection Issues
```powershell
# Check RabbitMQ service status
Get-Service -Name "RabbitMQ*"

# Start RabbitMQ service if stopped
Start-Service -Name "RabbitMQ"

# Access management interface
Start-Process "http://localhost:15672"
```

#### Build Errors
```powershell
# Clean and rebuild all projects
dotnet clean SD_TRABALHO_2.sln
dotnet restore SD_TRABALHO_2.sln
dotnet build SD_TRABALHO_2.sln
```

#### Port Conflicts
- **Default Ports Used**:
  - MongoDB: 27017
  - RabbitMQ: 5672 (AMQP), 15672 (Management)
  - Servidor: Various (check configuration)

```powershell
# Check what's using specific ports
netstat -ano | findstr ":27017"
netstat -ano | findstr ":5672"
```

### Performance Optimization

#### For High Data Volumes
1. **Increase RabbitMQ buffer sizes**
2. **Configure MongoDB indexing** for timestamp queries
3. **Implement data retention policies**
4. **Use MongoDB sharding** for large datasets

#### For Multiple Regions
1. **Deploy regional MongoDB instances**
2. **Use RabbitMQ clustering**
3. **Implement load balancers** for Servidor instances
4. **Configure regional failover**

## Development Environment

### IDE Recommendations
- **Visual Studio 2022** - Full IDE with debugging support
- **Visual Studio Code** - Lightweight with C# extension
- **JetBrains Rider** - Professional cross-platform IDE

### Useful Extensions
- **C# Dev Kit** (VS Code)
- **MongoDB for VS Code**
- **RabbitMQ Management** (Web interface)

### Development Workflow
1. **Make changes** to C# code
2. **Build project**: `dotnet build`
3. **Test locally** using Electron interface
4. **Debug issues** using IDE debugging tools
5. **Commit changes** using Git

This completes the comprehensive setup and usage guide for the Distributed Sensor Data Management System.
