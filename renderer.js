const { ipcRenderer } = require('electron');

let currentComponent = null;
let componentOutputs = new Map();
let runningProcesses = new Set();

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    updateProcessList();
    setupEventListeners();
    setInterval(updateProcessList, 2000); // Update every 2 seconds
});

function setupEventListeners() {
    // Terminal input
    const commandInput = document.getElementById('command-input');
    commandInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            sendCommand();
        }
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        // Ctrl+` to focus terminal input
        if (e.ctrlKey && e.key === '`') {
            e.preventDefault();
            if (currentComponent && runningProcesses.has(currentComponent)) {
                commandInput.focus();
            }
        }
        
        // Ctrl+Shift+S to start server
        if (e.ctrlKey && e.shiftKey && e.key === 'S') {
            e.preventDefault();
            startServer();
        }
        
        // Ctrl+Shift+A to start all
        if (e.ctrlKey && e.shiftKey && e.key === 'A') {
            e.preventDefault();
            startAllComponents();
        }
        
        // Ctrl+Shift+X to stop all
        if (e.ctrlKey && e.shiftKey && e.key === 'X') {
            e.preventDefault();
            stopAllProcesses();
        }
    });    // IPC listeners
    ipcRenderer.on('process-output', (event, processId, output) => {
        if (!componentOutputs.has(processId)) {
            componentOutputs.set(processId, '');
        }
        
        // Clean up output and add timestamp for manager messages
        let cleanOutput = output;
        if (cleanOutput.includes('[MANAGER]')) {
            const timestamp = new Date().toLocaleTimeString();
            cleanOutput = cleanOutput.replace('[MANAGER]', `[MANAGER ${timestamp}]`);
        }
        
        componentOutputs.set(processId, componentOutputs.get(processId) + cleanOutput);
        
        if (currentComponent === processId) {
            updateTerminalContent();
        }
        
        // Auto-scroll notification for background processes
        if (currentComponent !== processId && runningProcesses.has(processId)) {
            showNotification(processId, 'New output available');
        }
    });

    ipcRenderer.on('process-closed', (event, processId) => {
        runningProcesses.delete(processId);
        updateComponentStatus(processId, false);
        
        // Add closure message to output
        addToOutput(processId, `\n[MANAGER] Process ${processId} has stopped.\n`);
        
        // Show notification
        showNotification(processId, 'Process stopped');
    });

    // Handle auto-started server
    ipcRenderer.on('server-auto-started', () => {
        runningProcesses.add('server');
        updateComponentStatus('server', true);
        addToOutput('server', '[MANAGER] Server auto-started on application launch.\n');
        selectComponent('server');
          // Show welcome message with auto-start info
        const terminalContent = document.getElementById('terminal-content');
        terminalContent.innerHTML = `
            <div class="welcome-message">
                🚀 <strong>Server Auto-Started!</strong> 🚀<br><br>
                <span class="success-text">✅ Servidor is now running automatically</span><br><br>
                You can now start Aggregators and Wavy components.<br>
                Use Quick Start buttons for easy continent setup!<br><br>
                <span style="color: #ffaa00;">Click on "server" in the sidebar to view server output.</span>
            </div>
        `;
    });
}

async function startServer() {
    const result = await ipcRenderer.invoke('start-server');
    if (result.success) {
        runningProcesses.add('server');
        updateComponentStatus('server', true);
        addToOutput('server', '[MANAGER] Starting Servidor...\n');
        selectComponent('server');
    } else {
        alert(`Failed to start server: ${result.message}`);
    }
}

async function startAggregator() {
    const aggregatorId = document.getElementById('aggregator-id').value.trim();
    if (!aggregatorId) {
        alert('Please enter an Aggregator ID (e.g., N_Agr)');
        return;
    }

    const processId = `aggregator-${aggregatorId}`;
    const result = await ipcRenderer.invoke('start-aggregator', aggregatorId);
    
    if (result.success) {
        runningProcesses.add(processId);
        addAggregatorToList(aggregatorId);
        addToOutput(processId, `[MANAGER] Starting Agregador ${aggregatorId}...\n`);
        selectComponent(processId);
        document.getElementById('aggregator-id').value = '';
    } else {
        alert(`Failed to start aggregator: ${result.message}`);
    }
}

async function startWavy() {
    const wavyId = document.getElementById('wavy-id').value.trim();
    if (!wavyId) {
        alert('Please enter a Wavy ID (e.g., N_Wavy01)');
        return;
    }

    const processId = `wavy-${wavyId}`;
    const result = await ipcRenderer.invoke('start-wavy', wavyId);
    
    if (result.success) {
        runningProcesses.add(processId);
        addWavyToList(wavyId);
        addToOutput(processId, `[MANAGER] Starting Wavy ${wavyId}...\n`);
        selectComponent(processId);
        document.getElementById('wavy-id').value = '';
    } else {
        alert(`Failed to start wavy: ${result.message}`);
    }
}

async function stopProcess(processId) {
    const result = await ipcRenderer.invoke('stop-process', processId);
    if (result.success) {
        addToOutput(processId, '[MANAGER] Stopping process...\n');
    } else {
        alert(`Failed to stop process: ${result.message}`);
    }
}

async function stopAllProcesses() {
    const processes = await ipcRenderer.invoke('get-processes');
    for (const process of processes) {
        if (process.alive) {
            await stopProcess(process.id);
        }
    }
    
    // Clear UI
    runningProcesses.clear();
    document.getElementById('aggregator-list').innerHTML = '';
    document.getElementById('wavy-list').innerHTML = '';
    updateComponentStatus('server', false);
}

async function startAllComponents() {
    // Start server if not already running
    if (!runningProcesses.has('server')) {
        await startServer();
        // Wait for server to initialize
        await new Promise(resolve => setTimeout(resolve, 3000));
    } else {
        console.log('Server already running, skipping...');
    }
    
    // Start aggregators for all 7 continents
    const continents = ['EU', 'NA', 'SA', 'AF', 'AS', 'OC', 'AQ'];
    for (const continent of continents) {
        document.getElementById('aggregator-id').value = `${continent}-Agr01`;
        await startAggregator();
        await new Promise(resolve => setTimeout(resolve, 1500));
    }
    
    // Wait for aggregators to initialize
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // Start wavy sensors for each continent
    for (const continent of continents) {
        document.getElementById('wavy-id').value = `${continent}-Wavy01`;
        await startWavy();
        await new Promise(resolve => setTimeout(resolve, 1000));
    }
    
    // Show completion message
    addToOutput('server', '\n[MANAGER] Full system startup completed! All continents (EU, NA, SA, AF, AS, OC, AQ) are now active.\n');
}

async function quickStartRegion(continent) {
    // Start aggregator for continent
    document.getElementById('aggregator-id').value = `${continent}-Agr01`;
    await startAggregator();
    
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // Start a couple of wavys for the continent
    document.getElementById('wavy-id').value = `${continent}-Wavy01`;
    await startWavy();
    
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    document.getElementById('wavy-id').value = `${continent}-Wavy02`;
    await startWavy();
}

function addAggregatorToList(aggregatorId) {
    const list = document.getElementById('aggregator-list');
    const processId = `aggregator-${aggregatorId}`;
    
    const item = document.createElement('div');
    item.className = 'component-item';
    item.setAttribute('data-component', processId);
    item.onclick = () => selectComponent(processId);
    
    item.innerHTML = `
        <div class="component-name">${aggregatorId}</div>
        <div class="component-status running" id="status-${processId}">Running</div>
        <button class="btn stop" onclick="event.stopPropagation(); stopProcess('${processId}')" style="margin-top: 5px; font-size: 10px;">Stop</button>
    `;
    
    list.appendChild(item);
}

function addWavyToList(wavyId) {
    const list = document.getElementById('wavy-list');
    const processId = `wavy-${wavyId}`;
    
    const item = document.createElement('div');
    item.className = 'component-item';
    item.setAttribute('data-component', processId);
    item.onclick = () => selectComponent(processId);
    
    item.innerHTML = `
        <div class="component-name">${wavyId}</div>
        <div class="component-status running" id="status-${processId}">Running</div>
        <button class="btn stop" onclick="event.stopPropagation(); stopProcess('${processId}')" style="margin-top: 5px; font-size: 10px;">Stop</button>
    `;
    
    list.appendChild(item);
}

function selectComponent(componentId) {
    // Remove active class from all components
    document.querySelectorAll('.component-item').forEach(item => {
        item.classList.remove('active');
    });
    
    // Add active class to selected component
    const selectedElement = document.querySelector(`[data-component="${componentId}"]`);
    if (selectedElement) {
        selectedElement.classList.add('active');
    }
    
    currentComponent = componentId;
    updateTerminalContent();
    
    // Show terminal input for this component
    const terminalInput = document.getElementById('terminal-input');
    if (runningProcesses.has(componentId)) {
        terminalInput.style.display = 'block';
    } else {
        terminalInput.style.display = 'none';
    }
}

// Terminal control features
let tailMode = true;
const MAX_OUTPUT_LENGTH = 5000;
const TAIL_MODE_LINES = 100;

function clearTerminal() {
    if (currentComponent && componentOutputs.has(currentComponent)) {
        componentOutputs.set(currentComponent, '');
        updateTerminalContent();
    }
}

function toggleTailMode() {
    tailMode = !tailMode;
    document.getElementById('tail-mode-status').textContent = tailMode ? 'ON' : 'OFF';
    updateTerminalContent();
}

function addToOutput(componentId, text) {
    if (!componentOutputs.has(componentId)) {
        componentOutputs.set(componentId, '');
    }
    
    let output = componentOutputs.get(componentId) + text;
    
    // Limit overall output size to prevent memory issues
    if (output.length > MAX_OUTPUT_LENGTH) {
        output = output.slice(-MAX_OUTPUT_LENGTH);
    }
    
    componentOutputs.set(componentId, output);
    
    if (currentComponent === componentId) {
        updateTerminalContent();
    }
    
    // Update message count
    updateMessageCount(componentId);
}

function updateMessageCount(componentId) {
    if (currentComponent === componentId) {
        const output = componentOutputs.get(componentId) || '';
        const messageCount = (output.match(/Data received successfully/g) || []).length;
        document.getElementById('message-count').textContent = `${messageCount} messages`;
    }
}

function updateTerminalContent() {
    const terminalContent = document.getElementById('terminal-content');
    
    if (currentComponent && componentOutputs.has(currentComponent)) {
        let output = componentOutputs.get(currentComponent);
        const isRunning = runningProcesses.has(currentComponent);
        const status = isRunning ? 'RUNNING' : 'STOPPED';
        const statusColor = isRunning ? '#00ff00' : '#ff6666';
        
        // In tail mode, only show the last portion of the output
        if (tailMode && output.length > 0) {
            const lines = output.split('\n');
            if (lines.length > TAIL_MODE_LINES) {
                output = lines.slice(-TAIL_MODE_LINES).join('\n');
                output = `[...${lines.length - TAIL_MODE_LINES} earlier messages hidden...]\n\n` + output;
            }
        }
        
        terminalContent.innerHTML = `
            <div style="color: #00aaaa; margin-bottom: 10px; border-bottom: 1px solid #333; padding-bottom: 5px;">
                [${currentComponent.toUpperCase()}] - <span style="color: ${statusColor}">${status}</span>
            </div>` + 
            formatOutput(output);
            
        // Update message count
        updateMessageCount(currentComponent);
    } else if (currentComponent) {
        terminalContent.innerHTML = `
            <div style="color: #00aaaa; margin-bottom: 10px;">
                [${currentComponent.toUpperCase()}] Waiting for output...
            </div>`;
    }
    
    // Auto-scroll to bottom
    terminalContent.scrollTop = terminalContent.scrollHeight;
}

function formatOutput(text) {
    // Escape HTML and format special messages
    const escaped = escapeHtml(text);
    
    return escaped
        .replace(/\[MANAGER[^\]]*\]/g, '<span style="color: #00aaaa; font-weight: bold;">$&</span>')
        .replace(/\[ERROR\]/g, '<span style="color: #ff6666; font-weight: bold;">[ERROR]</span>')
        .replace(/\[.*?\]/g, '<span style="color: #ffaa00;">$&</span>') // Other brackets in orange
        .replace(/ID da Wavy:|ID do Agregador:/g, '<span style="color: #00ff00; font-weight: bold;">$&</span>')
        .replace(/Handshake estabelecido|conexão estabelecida|sucesso/gi, '<span style="color: #66ff66;">$&</span>')
        .replace(/erro|falha|failed|error/gi, '<span style="color: #ff6666;">$&</span>');
}

function showNotification(processId, message) {
    // Simple visual notification - could be expanded
    const componentElement = document.querySelector(`[data-component="${processId}"]`);
    if (componentElement) {
        componentElement.style.border = '2px solid #ffaa00';
        setTimeout(() => {
            componentElement.style.border = '';
        }, 2000);
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function updateSystemStatus() {
    const indicator = document.getElementById('status-indicator');
    const statusText = document.getElementById('status-text');
    const serverRunning = runningProcesses.has('server');
    const totalProcesses = runningProcesses.size;

    if (serverRunning && totalProcesses > 1) {
        indicator.className = 'status-indicator';
        indicator.style.color = '#00ff00';
        statusText.textContent = `System Active (${totalProcesses} processes)`;
    } else if (serverRunning) {
        indicator.className = 'status-indicator warning';
        indicator.style.color = '#ffaa00';
        statusText.textContent = 'Server Ready';
    } else if (totalProcesses > 0) {
        indicator.className = 'status-indicator warning';
        indicator.style.color = '#ffaa00';
        statusText.textContent = `${totalProcesses} processes (no server)`;
    } else {
        indicator.className = 'status-indicator error';
        indicator.style.color = '#ff6666';
        statusText.textContent = 'System Idle';
    }
}

async function sendCommand() {
    const input = document.getElementById('command-input');
    const command = input.value.trim();
    
    if (command && currentComponent) {
        const success = await ipcRenderer.invoke('send-input', currentComponent, command);
        if (success) {
            addToOutput(currentComponent, `> ${command}\n`);
        }
    }
    
    input.value = '';
}

async function updateProcessList() {
    const processes = await ipcRenderer.invoke('get-processes');
    
    // Update running processes set
    runningProcesses.clear();
    processes.forEach(proc => {
        if (proc.alive) {
            runningProcesses.add(proc.id);
        }
    });
    
    // Update UI status indicators
    processes.forEach(proc => {
        updateComponentStatus(proc.id, proc.alive);
    });
    
    // Update server button
    const serverBtn = document.getElementById('btn-server');
    if (runningProcesses.has('server')) {
        serverBtn.textContent = 'Stop Server';
        serverBtn.onclick = () => stopProcess('server');
        serverBtn.classList.add('stop');
    } else {
        serverBtn.textContent = 'Start Server';
        serverBtn.onclick = startServer;
        serverBtn.classList.remove('stop');
    }
    
    // Update system status
    updateSystemStatus();
}

function updateComponentStatus(processId, isRunning) {
    const statusElement = document.getElementById(`status-${processId}`);
    if (statusElement) {
        statusElement.textContent = isRunning ? 'Running' : 'Stopped';
        statusElement.className = `component-status ${isRunning ? 'running' : 'stopped'}`;
    }
}

function highlightErrors() {
    if (currentComponent && componentOutputs.has(currentComponent)) {
        const terminalContent = document.getElementById('terminal-content');
        const errorElements = terminalContent.querySelectorAll('span[style*="color: #ff6666"]');
        
        if (errorElements.length > 0) {
            // Flash all error elements
            errorElements.forEach(el => {
                el.style.backgroundColor = '#440000';
                
                // Scroll to the first error
                if (el === errorElements[0]) {
                    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            });
            
            // Remove highlighting after a few seconds
            setTimeout(() => {
                errorElements.forEach(el => {
                    el.style.backgroundColor = 'transparent';
                });
            }, 3000);
        } else {
            // If no errors found, add a temporary message
            const message = document.createElement('div');
            message.textContent = 'No errors found in current output';
            message.style.color = '#66ff66';
            message.style.textAlign = 'center';
            message.style.padding = '10px';
            message.style.margin = '10px 0';
            message.style.backgroundColor = '#003300';
            message.style.borderRadius = '5px';
            
            terminalContent.insertBefore(message, terminalContent.firstChild);
            
            setTimeout(() => {
                message.remove();
            }, 3000);
        }
    }
}

// Accordion functionality
function toggleAccordion(contentId) {
    const content = document.getElementById(contentId);
    const arrow = document.getElementById(contentId.replace('-content', '-arrow'));
    
    if (content.classList.contains('collapsed')) {
        content.classList.remove('collapsed');
        arrow.classList.remove('rotated');
        arrow.textContent = '▼';
    } else {
        content.classList.add('collapsed');
        arrow.classList.add('rotated');
        arrow.textContent = '▶';
    }
}

// Map tab functionality
let map = null;
let mapInitialized = false;

function openMapTab() {
    const tabContainer = document.getElementById('map-tab-container');
    tabContainer.style.display = 'block';
    
    // Initialize map if not already done
    if (!mapInitialized) {
        setTimeout(() => {
            initializeMap();
            mapInitialized = true;
        }, 100);
    } else {
        // Invalidate size to fix display issues
        setTimeout(() => {
            if (map) {
                map.invalidateSize();
            }
        }, 100);
    }
    
    appendOutput('system', '🗺️ System map opened in new tab');
}

function closeMapTab() {
    const tabContainer = document.getElementById('map-tab-container');
    tabContainer.style.display = 'none';
    
    appendOutput('system', '🗺️ Map tab closed');
}

function initializeMap() {
    // Initialize the map
    map = L.map('map').setView([20, 0], 2); // Center on world view
    
    // Add tile layer
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);
    
    // Add system markers
    addSystemMarkers();
    
    appendOutput('system', '🗺️ Interactive map initialized with system components');
}

function addSystemMarkers() {
    // Example locations for each continent
    const locations = [
        // Europe
        { lat: 52.5200, lng: 13.4050, name: 'EU-Server', type: 'Server', continent: 'EU', city: 'Berlin' },
        { lat: 48.8566, lng: 2.3522, name: 'EU-Agr01', type: 'Aggregator', continent: 'EU', city: 'Paris' },
        { lat: 51.5074, lng: -0.1278, name: 'EU-Wavy01', type: 'Wavy', continent: 'EU', city: 'London' },
        { lat: 55.7558, lng: 37.6176, name: 'EU-Wavy02', type: 'Wavy', continent: 'EU', city: 'Moscow' },
        
        // North America
        { lat: 40.7128, lng: -74.0060, name: 'NA-Server', type: 'Server', continent: 'NA', city: 'New York' },
        { lat: 34.0522, lng: -118.2437, name: 'NA-Agr01', type: 'Aggregator', continent: 'NA', city: 'Los Angeles' },
        { lat: 41.8781, lng: -87.6298, name: 'NA-Wavy01', type: 'Wavy', continent: 'NA', city: 'Chicago' },
        { lat: 43.6532, lng: -79.3832, name: 'NA-Wavy02', type: 'Wavy', continent: 'NA', city: 'Toronto' },
        
        // South America
        { lat: -23.5505, lng: -46.6333, name: 'SA-Server', type: 'Server', continent: 'SA', city: 'São Paulo' },
        { lat: -34.6037, lng: -58.3816, name: 'SA-Agr01', type: 'Aggregator', continent: 'SA', city: 'Buenos Aires' },
        { lat: -22.9068, lng: -43.1729, name: 'SA-Wavy01', type: 'Wavy', continent: 'SA', city: 'Rio de Janeiro' },
        
        // Africa
        { lat: -26.2041, lng: 28.0473, name: 'AF-Server', type: 'Server', continent: 'AF', city: 'Johannesburg' },
        { lat: 30.0444, lng: 31.2357, name: 'AF-Agr01', type: 'Aggregator', continent: 'AF', city: 'Cairo' },
        { lat: -1.2921, lng: 36.8219, name: 'AF-Wavy01', type: 'Wavy', continent: 'AF', city: 'Nairobi' },
        
        // Asia
        { lat: 35.6762, lng: 139.6503, name: 'AS-Server', type: 'Server', continent: 'AS', city: 'Tokyo' },
        { lat: 39.9042, lng: 116.4074, name: 'AS-Agr01', type: 'Aggregator', continent: 'AS', city: 'Beijing' },
        { lat: 28.6139, lng: 77.2090, name: 'AS-Wavy01', type: 'Wavy', continent: 'AS', city: 'New Delhi' },
        { lat: 1.3521, lng: 103.8198, name: 'AS-Wavy02', type: 'Wavy', continent: 'AS', city: 'Singapore' },
        
        // Oceania
        { lat: -33.8688, lng: 151.2093, name: 'OC-Server', type: 'Server', continent: 'OC', city: 'Sydney' },
        { lat: -37.8136, lng: 144.9631, name: 'OC-Agr01', type: 'Aggregator', continent: 'OC', city: 'Melbourne' },
        { lat: -27.4698, lng: 153.0251, name: 'OC-Wavy01', type: 'Wavy', continent: 'OC', city: 'Brisbane' },
        
        // Antarctica
        { lat: -77.8463, lng: 166.6667, name: 'AQ-Server', type: 'Server', continent: 'AQ', city: 'McMurdo Station' },
        { lat: -70.6693, lng: 2.5333, name: 'AQ-Wavy01', type: 'Wavy', continent: 'AQ', city: 'Research Base' }
    ];
    
    locations.forEach(location => {
        let iconColor = '#00ff00'; // Default green
        let iconSymbol = '●';
        
        // Set different colors and symbols based on component type
        switch(location.type) {
            case 'Server':
                iconColor = '#ff6600';
                iconSymbol = '🖥️';
                break;
            case 'Aggregator':
                iconColor = '#6666ff';
                iconSymbol = '📊';
                break;
            case 'Wavy':
                iconColor = '#00ff00';
                iconSymbol = '📡';
                break;
        }
        
        // Create marker
        const marker = L.marker([location.lat, location.lng]).addTo(map);
        
        // Check if component is currently running
        const processId = location.type === 'Server' ? 'server' : 
                         location.type === 'Aggregator' ? `aggregator-${location.name}` : 
                         `wavy-${location.name}`;
        
        const isRunning = runningProcesses.has(processId);
        const statusColor = isRunning ? '#00ff00' : '#ff6666';
        const status = isRunning ? 'Running' : 'Stopped';
        
        // Create popup content
        const popupContent = `
            <div style="color: ${iconColor}; font-family: 'Courier New', monospace; min-width: 200px;">
                <strong>${iconSymbol} ${location.name}</strong><br>
                <strong>Type:</strong> ${location.type}<br>
                <strong>Location:</strong> ${location.city}<br>
                <strong>Continent:</strong> ${location.continent}<br>
                <strong>Status:</strong> <span style="color: ${statusColor};">${status}</span><br>
                <strong>Coordinates:</strong> ${location.lat.toFixed(4)}, ${location.lng.toFixed(4)}
            </div>
        `;
        
        marker.bindPopup(popupContent);
        
        // Add click event to select component in main interface
        marker.on('click', () => {
            appendOutput('system', `📍 Clicked on ${location.name} (${location.type}) in ${location.city}`);
            
            // If component is running, switch to it in main interface
            if (isRunning) {
                selectComponent(processId);
            }
        });
        
        // Change marker opacity based on running status
        marker.setOpacity(isRunning ? 1.0 : 0.6);
    });
}

// Helper function to append output (using existing addToOutput if available)
function appendOutput(componentId, message) {
    if (typeof addToOutput === 'function') {
        addToOutput(componentId, `[${new Date().toLocaleTimeString()}] ${message}\n`);
    }
}
