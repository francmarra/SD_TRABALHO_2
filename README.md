# Distributed Sensor Data Management System

A comprehensive distributed system built with C# .NET that manages sensor data collection, aggregation, and storage across multiple regions. The system includes an Electron-based graphical management interface for monitoring and controlling all components.

## System Overview

This distributed system simulates a real-world IoT sensor network where:
- **Wavy sensors** generate environmental data (temperature, humidity, CO2)
- **Regional Aggregators** collect and process data from multiple sensors
- **Central Server** stores all aggregated data with persistence and querying capabilities
- **Management Interface** provides real-time monitoring and control

### Architecture Diagram

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ Wavy Sensors    │    │   Agregadores    │    │    Servidor     │
│ (Data Sources)  │───▶│  (Regional       │───▶│  (Central       │
│                 │    │   Aggregators)   │    │   Server)       │
│ • Temperature   │    │                  │    │                 │
│ • Humidity      │    │ • Data Collection│    │ • Data Storage  │
│ • CO2 Levels    │    │ • Processing     │    │ • Query API     │
│ • Timestamps    │    │ • Forwarding     │    │ • Persistence   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## System Components

### 1. **Wavy (Sensor Nodes)**
**Location**: `./Wavy/`  
**Technology**: C# .NET Console Application

**Purpose**: Simulates IoT sensors that generate environmental data
- **Data Generated**:
  - Temperature (°C)
  - Humidity (%)
  - CO2 levels (ppm)
  - Timestamp
  - Sensor ID and location

**Communication**:
- **Protocol**: RabbitMQ Message Queue
- **Pattern**: Publisher/Producer
- **Message Format**: JSON serialized sensor readings
- **Frequency**: Configurable intervals (default: every 5 seconds)

**Key Features**:
- Graceful shutdown with `DLG` command
- Configurable sensor parameters
- Regional identification (N, S, E, W)
- Automatic reconnection on communication failures

### 2. **Agregador (Regional Aggregators)**
**Location**: `./Agregador/`  
**Technology**: C# .NET Console Application

**Purpose**: Regional data collection and aggregation hubs
- **Data Processing**:
  - Receives data from multiple Wavy sensors
  - Performs basic aggregation (averages, min/max)
  - Adds regional metadata
  - Forwards processed data to central server

**Communication**:
- **Inbound**: RabbitMQ Consumer (from Wavy sensors)
- **Outbound**: RabbitMQ Publisher (to Server)
- **Pattern**: Message queue middleware with fanout/direct exchanges

**Key Features**:
- Multi-sensor data correlation
- Regional data buffering
- Load balancing across multiple aggregators
- Fault tolerance and data persistence

### 3. **Servidor (Central Server)**
**Location**: `./Servidor/`  
**Technology**: C# .NET Web API + Console Application

**Purpose**: Central data repository and management system
- **Data Storage**: MongoDB document database
- **API Services**: RESTful web API for data queries
- **Real-time Processing**: Live data ingestion and storage

**Communication**:
- **Inbound**: RabbitMQ Consumer (from Aggregators)
- **Database**: MongoDB for persistent storage
- **API**: HTTP/HTTPS REST endpoints
- **Management**: WebSocket for real-time monitoring

**Database Schema (MongoDB)**:
```json
{
  "_id": "ObjectId",
  "sensorId": "N_Wavy01",
  "region": "North",
  "timestamp": "2025-05-28T10:30:00Z",
  "data": {
    "temperature": 23.5,
    "humidity": 65.2,
    "co2": 410
  },
  "aggregatorId": "N_Agr",
  "receivedAt": "2025-05-28T10:30:01Z"
}
```

## Communication Infrastructure

### **RabbitMQ Message Queuing**
- **Version**: RabbitMQ 3.x
- **Exchanges**: 
  - `sensor_data` (Direct exchange for sensor readings)
  - `aggregated_data` (Direct exchange for processed data)
- **Queues**:
  - Regional queues: `north_sensors`, `south_sensors`, etc.
  - Aggregator queues: `aggregator_data`
  - Server queue: `server_data`

**Message Flow**:
```
Wavy → [sensor_data exchange] → [regional_queue] → Agregador
Agregador → [aggregated_data exchange] → [server_queue] → Servidor
```

### **MongoDB Database**
- **Version**: MongoDB 5.x+
- **Database**: `SensorDataDB`
- **Collections**:
  - `readings` - Raw sensor data
  - `aggregated` - Processed regional data
  - `sensors` - Sensor metadata and status
  - `alerts` - System alerts and notifications

**Indexing Strategy**:
- Compound index on `timestamp` + `region`
- Index on `sensorId` for fast lookups
- TTL index for data retention policies

### **Configuration Management**
Each component uses configuration files:
- **RabbitMQ**: Connection strings, queue names, exchange settings
- **MongoDB**: Database connections, collection names
- **Logging**: NLog/Serilog configuration for centralized logging
- **Regional Settings**: Component IDs, geographic regions

## Regional Architecture

### **Multi-Region Support**
The system supports four geographic regions:
- **North (N)**: `N_Agr`, `N_Wavy01`, `N_Wavy02`, etc.
- **South (S)**: `S_Agr`, `S_Wavy01`, `S_Wavy02`, etc.
- **East (E)**: `E_Agr`, `E_Wavy01`, `E_Wavy02`, etc.
- **West (W)**: `W_Agr`, `W_Wavy01`, `W_Wavy02`, etc.

### **Scaling and Load Distribution**
- Each region can have multiple Aggregators for load balancing
- Wavy sensors can be dynamically added/removed
- Horizontal scaling through additional server instances
- Data partitioning by region and time

## Electron Management Interface

### **Technology Stack**
- **Framework**: Electron (Node.js + Chromium)
- **UI**: HTML5, CSS3, JavaScript ES6+
- **Process Management**: Node.js child_process
- **IPC**: Electron inter-process communication

### **Features**
🖥️ **Modern Terminal Interface** - Black terminal aesthetic with real-time output  
⚡ **Process Lifecycle Management** - Start, stop, restart all components  
📊 **Live Monitoring** - Real-time output streaming from C# processes  
🚀 **Quick Deployment** - Predefined regional and full-system setups  
⌨️ **Keyboard Shortcuts** - Efficient management hotkeys  
🎯 **Interactive Control** - Send commands directly to running processes  

### **Auto-Start Features**
- **Server Auto-Start**: Central server launches automatically
- **Regional Quick Start**: Deploy complete regions with one click
- **Full System Deployment**: Start all components simultaneously
- **Health Monitoring**: Automatic process health checks and restart capabilities

## Quick Start Guide

### **Prerequisites**
```bash
# Required Software
- .NET 8.0+ SDK
- Node.js 18+ 
- RabbitMQ Server
- MongoDB Server
- Visual Studio Code (recommended)
```

### **Installation**
1. **Clone Repository**:
   ```bash
   git clone <repository-url>
   cd SD_TRABALHO_2
   ```

2. **Setup Electron Manager**:
   ```bash
   npm install
   ```

3. **Build C# Components**:
   ```bash
   dotnet build ./Servidor/
   dotnet build ./Agregador/
   dotnet build ./Wavy/
   ```

4. **Start Infrastructure**:
   ```bash
   # Start RabbitMQ
   rabbitmq-server
   
   # Start MongoDB
   mongod
   ```

### **Running the System**

#### **Option 1: Electron Manager (Recommended)**
```bash
npm start
# Or double-click: start-manager.bat
```

#### **Option 2: Manual Component Start**
```bash
# Terminal 1: Server
cd Servidor && dotnet run

# Terminal 2: Aggregator  
cd Agregador && dotnet run N_Agr

# Terminal 3: Wavy Sensor
cd Wavy && dotnet run N_Wavy01
```

## Component Configuration

### **Component IDs and Naming**
- **Aggregators**: `<Region>_Agr` (e.g., `N_Agr`, `S_Agr`)
- **Wavy Sensors**: `<Region>_Wavy<NN>` (e.g., `N_Wavy01`, `S_Wavy02`)
- **Regions**: North (N), South (S), East (E), West (W)

### **Keyboard Shortcuts**
| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+S` | Start Central Server |
| `Ctrl+Shift+A` | Start All Components |
| `Ctrl+Shift+X` | Stop All Processes |
| `Ctrl+\`` | Focus Terminal Input |

### **Quick Deployment Scenarios**

#### **Regional Deployment**
- **North Region**: Click "North Region" → Starts `N_Agr` + `N_Wavy01` + `N_Wavy02`
- **South Region**: Click "South Region" → Starts `S_Agr` + `S_Wavy01` + `S_Wavy02`
- **East Region**: Click "East Region" → Starts `E_Agr` + `E_Wavy01` + `E_Wavy02`
- **West Region**: Click "West Region" → Starts `W_Agr` + `W_Wavy01` + `W_Wavy02`

#### **Full System**
- **Start All**: Deploys complete 4-region system with all components

## Monitoring and Management

### **Process Status Indicators**
- 🟢 **Green**: Component running and healthy
- 🔴 **Red**: Component stopped or failed
- 🟡 **Yellow**: Component starting or shutting down

### **Real-time Monitoring**
- Live output streaming from all components
- Interactive terminal for sending commands
- System health dashboard
- Performance metrics and logging

### **Graceful Shutdown**
- Components respond to `DLG` command for graceful shutdown
- Automatic cleanup of resources and connections
- Data persistence before shutdown
- Configurable shutdown timeouts

## Development and Extension

### **Project Structure**
```
SD_TRABALHO_2/
├── Servidor/           # Central server (C# .NET)
├── Agregador/          # Regional aggregators (C# .NET)  
├── Wavy/              # Sensor nodes (C# .NET)
├── main.js            # Electron main process
├── renderer.js        # Frontend JavaScript
├── index.html         # Management interface UI
├── package.json       # Node.js dependencies
└── README.md          # This documentation
```

### **Adding New Components**
1. Create new C# project following existing patterns
2. Implement RabbitMQ communication
3. Add component to Electron manager interface
4. Update configuration and deployment scripts

### **Customizing Sensor Data**
- Modify `Wavy` data generation algorithms
- Add new sensor types (air quality, pressure, etc.)
- Implement custom aggregation logic in `Agregador`
- Extend database schema in `Servidor`

## Troubleshooting

### **Common Issues**
- **RabbitMQ Connection Refused**: Ensure RabbitMQ server is running
- **MongoDB Connection Failed**: Verify MongoDB service status
- **Process Won't Start**: Check .NET runtime installation
- **Port Conflicts**: Verify no other services using required ports

### **Debugging**
- Enable detailed logging in component configuration
- Use Electron DevTools for interface debugging
- Monitor RabbitMQ management interface
- Check MongoDB logs for database issues

### **Performance Optimization**
- Adjust message batching in RabbitMQ
- Configure MongoDB indexing for query performance
- Implement data retention policies
- Monitor system resource usage

This distributed system provides a robust, scalable foundation for IoT sensor data management with modern tooling and comprehensive monitoring capabilities.
