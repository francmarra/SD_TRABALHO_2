# Distributed System Manager

An Electron-based graphical interface for managing your distributed system components (Servidor, Agregador, and Wavy).

## Features

🖥️ **Modern Terminal-like Interface** - Black terminal aesthetic with green text  
⚡ **Process Management** - Start, stop, and monitor all system components  
📊 **Real-time Output** - View live output from each component  
🚀 **Quick Start Options** - Predefined setups for North/South regions  
⌨️ **Keyboard Shortcuts** - Efficient management with hotkeys  
🎯 **Interactive Terminal** - Send commands directly to running processes  

## Quick Start

1. **Install Dependencies** (if not already done):
   ```bash
   npm install
   ```

2. **Start the Manager**:
   ```bash
   npm start
   ```
   Or double-click `start-manager.bat`

3. **Basic Usage**:
   - Click "Start Server" to initialize the central server
   - Add Aggregators using IDs like `N_Agr`, `S_Agr`
   - Add Wavy sensors using IDs like `N_Wavy01`, `S_Wavy01`
   - Click on any component to view its terminal output
   - Use terminal input to send commands to selected processes

## System Architecture

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│    Wavy     │───▶│  Agregador  │───▶│   Servidor  │
│ (Sensors)   │    │(Aggregator) │    │  (Server)   │
└─────────────┘    └─────────────┘    └─────────────┘
```

- **Wavy**: Generates sensor data (temperature, humidity)
- **Agregador**: Collects and aggregates data from multiple Wavys
- **Servidor**: Central server that stores all aggregated data

## Component IDs

### Aggregators
- Format: `<Region>_Agr`
- Examples: `N_Agr`, `S_Agr`, `E_Agr`, `W_Agr`

### Wavy Sensors
- Format: `<Region>_Wavy<Number>`
- Examples: `N_Wavy01`, `S_Wavy02`, `E_Wavy03`

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+S` | Start Server |
| `Ctrl+Shift+A` | Start All Components |
| `Ctrl+Shift+X` | Stop All Processes |
| `Ctrl+\`` | Focus Terminal Input |

## Quick Start Scenarios

### Single Region Setup
1. Start Server
2. Click "North Region" or "South Region" quick start
3. This will automatically start:
   - One Aggregator for the region
   - Two Wavy sensors for the region

### Full System Setup
1. Click "Start All" - starts Server + N_Agr + S_Agr + N_Wavy01 + S_Wavy01
2. Add more Wavy sensors as needed

### Manual Setup
1. Start Server
2. Add Aggregators manually: `N_Agr`, `S_Agr`
3. Add Wavy sensors: `N_Wavy01`, `N_Wavy02`, `S_Wavy01`, etc.

## Process Management

- **Green status** = Running
- **Red status** = Stopped
- Click on any component to view its output
- Use the terminal input to send commands (like `DLG` to stop Wavy/Agregador)
- Stop buttons send graceful shutdown commands first, then force-kill if needed

## Troubleshooting

### Process Won't Start
- Ensure .NET is installed (`dotnet --version`)
- Check that all projects are built (`dotnet build` in each folder)
- Verify RabbitMQ is running if using message queues

### Terminal Not Responsive
- Use `Ctrl+\`` to focus the terminal input
- Make sure you've selected a running component first

### Process Won't Stop
- Try using the terminal input to send `DLG` command
- Use "Stop All" to force terminate all processes
- Manager will automatically force-kill after 5 seconds

## Development

The manager consists of:
- `main.js` - Electron main process (Node.js backend)
- `renderer.js` - Frontend JavaScript 
- `index.html` - User interface
- `package.json` - Dependencies and scripts

To modify or extend the manager, edit these files and restart with `npm start`.

## File Structure

```
├── main.js              # Electron main process
├── renderer.js          # Frontend logic
├── index.html           # User interface
├── package.json         # Dependencies
├── start-manager.bat    # Windows startup script
└── [C# Projects]        # Your distributed system components
```
