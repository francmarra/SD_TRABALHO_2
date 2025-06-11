# Distributed Sensor Data Management System

A comprehensive distributed system built with C# .NET that manages sensor data collection, aggregation, and storage across multiple continents. The system uses MongoDB for data persistence, RabbitMQ for message queuing, and includes an Electron-based graphical management interface for monitoring and controlling all components.

## Overview

This project implements a scalable IoT sensor data management system with modern microservices architecture. The system simulates real-world environmental monitoring across seven continental regions (Europe, North America, South America, Africa, Asia, Oceania, Antarctica), demonstrating distributed computing principles, message-driven architecture, and cloud-native design patterns.

### Key Features
- **🌊 Wavy Sensors**: Simulated IoT devices generating environmental data (temperature, humidity, CO2)
- **🔗 Continental Aggregators**: Data collection hubs that process and forward sensor readings using RabbitMQ
- **🗄️ Continental Servers**: MongoDB-based data repositories with real-time ingestion and query capabilities per continent
- **🖥️ Management Interface**: Electron desktop application for system monitoring and control
- **💾 Database-First Architecture**: Complete migration from file-based to MongoDB storage for enhanced scalability
- **⚡ Async Operations**: Full asynchronous programming model for high-performance data processing
- **🔄 Message Queuing**: RabbitMQ-based communication ensuring reliable data delivery and system decoupling
- **🌍 Continental Architecture**: Hierarchical structure with servers, aggregators, and sensors organized by continent

## System Architecture

The system implements a distributed microservices architecture with continent-based hierarchical organization:

```
┌─────────────────┐    ┌───────────────────┐    ┌─────────────────┐
│   Wavy Sensors  │    │   Agregadores     │    │ Continental     │
│  (Data Sources) │───▶│  (Continental     │───▶│ Servers         │
│                 │    │   Aggregators)    │    │                 │
│ • Temperature   │    │                   │    │ • EU-S, NA-S   │
│ • Humidity      │    │ • EU-Agr01        │    │ • MongoDB       │
│ • CO2 Levels    │    │ • NA-Agr01        │    │ • Data Storage  │
│ • EU-Wavy01     │    │ • RabbitMQ        │    │ • Query API     │
│ • NA-Wavy02     │    │ • Processing      │    │ • Per Continent │
└─────────────────┘    └───────────────────┘    └─────────────────┘
                                 ▲                       ▲
                                 │                       │
┌─────────────────────────────────────────────────────────────────┐
│                     RabbitMQ Message Broker                     │
│  • Async Communication  • Message Persistence  • Load Balance   │
│  • Continent-specific Queues (eu_*, na_*, sa_*, etc.)          │
└─────────────────────────────────────────────────────────────────┘
                                  ▲
                                  │
┌─────────────────────────────────────────────────────────────────┐
│                        MongoDB Database                         │
│    • Sensor Data     • Configuration   • Aggregated Results     │
│    • Document Store  • Indexing        • ACID Transactions      │
│    • Continental Collections (config_server, config_agr, etc.)  │
└─────────────────────────────────────────────────────────────────┘
                                  ▲
                                  │
┌─────────────────────────────────────────────────────────────────┐
│                  Electron Management Interface                  │
│   • Process Control   • Real-time Monitoring   • Continental UI │
│   • EU, NA, SA, AF, AS, OC, AQ Quick Start Buttons             │
└─────────────────────────────────────────────────────────────────┘
```

### Continental Organization
- **EU** (Europe): EU-S, EU-Agr01, EU-Wavy01-XX
- **NA** (North America): NA-S, NA-Agr01, NA-Wavy01-XX  
- **SA** (South America): SA-S, SA-Agr01, SA-Wavy01-XX
- **AF** (Africa): AF-S, AF-Agr01, AF-Wavy01-XX
- **AS** (Asia): AS-S, AS-Agr01, AS-Wavy01-XX
- **OC** (Oceania): OC-S, OC-Agr01, OC-Wavy01-XX
- **AQ** (Antarctica): AQ-S, AQ-Agr01, AQ-Wavy01-XX

### Architectural Principles:
- **Event-Driven Communication**: Components interact via RabbitMQ message queues
- **Database-Centric Design**: MongoDB provides centralized, scalable data storage and configuration management
- **Microservices Pattern**: Each component is independently deployable and horizontally scalable
- **Async-First**: All I/O operations use asynchronous programming patterns
- **Fault-Tolerant**: Components handle failures gracefully with automatic recovery mechanisms
- **Configuration-Driven**: System behavior controlled through MongoDB-stored configuration data

## Project Documentation

- **📖 [README.md](./README.md)** - This file: project overview and architecture
- **🚀 [SETUP.md](./SETUP.md)** - Detailed installation, configuration, and usage tutorial  
- **🏗️ [PROJECT-STRUCTURE.md](./PROJECT-STRUCTURE.md)** - Complete explanation of directories and file purposes

## System Components

The system consists of four main components working together:

### 🌊 Wavy Sensors
Simulated IoT devices that generate environmental data (temperature, humidity, CO2) and send it to regional aggregators via RabbitMQ.

### 🔗 Agregadores  
Regional data collection hubs that receive sensor data, perform aggregation, and forward processed data to the central server.

### 🗄️ Servidor
Central data repository that stores all sensor data in MongoDB and provides real-time processing capabilities.

### 🖥️ Electron Management Interface
Desktop application that provides comprehensive monitoring and control of all system components with one-click deployment options.

## Database Architecture

### MongoDB Collections

**ConfigAgr Collection** - Aggregator configurations (4 documents)
```javascript
{
  "_id": "N_Agr",
  "Region": "North", 
  "Port": 5000,
  "QueueName": "north_aggregator_queue"
}
```

**ConfigWavy Collection** - Sensor configurations (8 documents)
```javascript
{
  "_id": "N_Wavy01",
  "Region": "North",
  "IsActive": true,
  "Interval": 5000
}
```

**readings Collection** - Real-time sensor data
```javascript
{
  "_id": ObjectId("..."),
  "sensorId": "N_Wavy01",
  "region": "North",
  "timestamp": "2025-05-28T10:30:00Z",
  "temperature": 23.5,
  "humidity": 65.2,
  "co2": 410,
  "aggregatorId": "N_Agr",
  "receivedAt": "2025-05-28T10:30:01Z"
}
```

**aggregated Collection** - Processed regional summaries
```javascript
{
  "_id": ObjectId("..."),
  "region": "North",
  "timeWindow": "2025-05-28T10:30:00Z",
  "avgTemperature": 23.2,
  "avgHumidity": 64.8,
  "avgCO2": 405,
  "sensorCount": 2,
  "processedAt": "2025-05-28T10:31:00Z"
}
```

### Migration from File-Based to Database
The system has been completely migrated from CSV file configuration and JSON file logging to MongoDB-based storage:

**✅ Completed Migrations**:
- Configuration management: CSV files → MongoDB ConfigAgr/ConfigWavy collections
- Data storage: Individual `registos_*.json` files → MongoDB readings collection
- Async operations: Synchronous file I/O → Asynchronous database operations
- Service injection: Direct file access → ConfigService dependency injection

## Communication Infrastructure

### RabbitMQ Message Flow
```
Wavy Sensors ── sensor_data ──▶ Regional Queues ──▶ Agregadores
                                                         │
                                                         ▼
Servidor ◀── aggregated_data ─── Aggregator Queues ◀─────┘
    │
    ▼
MongoDB Storage
```

**Queue Configuration**:
- **sensor_data exchange**: Direct exchange for raw sensor readings
- **aggregated_data exchange**: Direct exchange for processed regional data
- **Regional queues**: `north_sensors`, `south_sensors`, `east_sensors`, `west_sensors`
- **Server queue**: `central_server_data`

**Message Patterns**:
- **Publish/Subscribe**: Wavy sensors publish to regional queues
- **Point-to-Point**: Aggregators send processed data to central server
- **Load Balancing**: Multiple aggregators can process the same regional queue

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

Cross-platform desktop application providing comprehensive monitoring and control of all system components with real-time process management and one-click deployment options.

For detailed interface features and usage instructions, see **[SETUP.md](./SETUP.md)** and **[PROJECT-STRUCTURE.md](./PROJECT-STRUCTURE.md)**.

## Quick Start

For complete installation and setup instructions, see **[SETUP.md](./SETUP.md)**.

## Component Configuration

For detailed component configuration, deployment scenarios, and keyboard shortcuts, see **[SETUP.md](./SETUP.md)**.

## Monitoring and Management

For detailed monitoring features, process management, and troubleshooting, see **[SETUP.md](./SETUP.md)**.

## Development and Extension

For detailed project structure and development guidelines, see **[PROJECT-STRUCTURE.md](./PROJECT-STRUCTURE.md)**.

## Project Documentation

- **📖 [README.md](./README.md)** - This file: comprehensive project overview and architecture
- **🚀 [SETUP.md](./SETUP.md)** - Detailed installation, configuration, and usage tutorial
- **🏗️ [PROJECT-STRUCTURE.md](./PROJECT-STRUCTURE.md)** - Complete explanation of directories and file purposes

## Technical Highlights

### Modern C# Development Practices
- **.NET 9.0**: Latest framework with performance improvements
- **Async/Await**: Full asynchronous programming model throughout
- **Dependency Injection**: Clean architecture with service injection
- **Nullable Reference Types**: Enhanced code safety and null checking

### Database-First Architecture
- **MongoDB Integration**: Complete migration from file-based to document storage
- **Async Database Operations**: All I/O operations use asynchronous patterns
- **Configuration Management**: Runtime configuration stored and managed in database
- **Data Models**: Strongly-typed models with MongoDB annotations

### Message-Driven Design
- **RabbitMQ**: Reliable message queuing for component communication
- **Decoupled Architecture**: Components communicate through message contracts
- **Scalability**: Horizontal scaling through queue-based load distribution
- **Fault Tolerance**: Message persistence and retry mechanisms

### Desktop Management Interface
- **Electron Framework**: Cross-platform desktop application
- **Real-time Monitoring**: Live process output streaming and status updates
- **Interactive Control**: Direct command sending to running processes
- **Modern UI**: Terminal-style interface with responsive design

## Regional Deployment

The system supports four geographic regions with complete component sets:

| Region    | Aggregator |        Sensors         |        Configuration         |
|-----------|------------|------------------------|------------------------------|
| **North** |   `N_Agr`  | `N_Wavy01`, `N_Wavy02` | MongoDB ConfigAgr/ConfigWavy |
| **South** |   `S_Agr`  | `S_Wavy01`, `S_Wavy02` | MongoDB ConfigAgr/ConfigWavy |
| **East**  |   `E_Agr`  | `E_Wavy01`, `E_Wavy02` | MongoDB ConfigAgr/ConfigWavy |
| **West**  |   `W_Agr`  | `W_Wavy01`, `W_Wavy02` | MongoDB ConfigAgr/ConfigWavy |

### Scaling Options
- **Horizontal Sensor Scaling**: Add more Wavy instances per region (`N_Wavy03`, `N_Wavy04`, etc.)
- **Aggregator Load Balancing**: Deploy multiple aggregators per region (`N_Agr1`, `N_Agr2`)
- **Multi-Server Deployment**: Run multiple Servidor instances with load balancing
- **Geographic Distribution**: Deploy regions across different physical locations

## Troubleshooting

For troubleshooting guides, performance optimization, and debugging information, see **[SETUP.md](./SETUP.md)**.

## Contributing and Development

For development workflow, architecture principles, and performance considerations, see **[PROJECT-STRUCTURE.md](./PROJECT-STRUCTURE.md)** and **[SETUP.md](./SETUP.md)**.

This distributed sensor data management system demonstrates modern software architecture principles while providing a robust foundation for IoT data processing and monitoring applications.
