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
        alert('Please enter a Wavy ID (e.g., Wavy01, Wavy02, etc.)');
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
    }    // Start aggregators for different ocean regions
    const aggregatorIds = ['ATL-Agr01', 'PAC-Agr01', 'IND-Agr01', 'ARC-Agr01', 'ANT-Agr01'];
    for (const aggregatorId of aggregatorIds) {
        document.getElementById('aggregator-id').value = aggregatorId;
        await startAggregator();
        await new Promise(resolve => setTimeout(resolve, 1500));
    }

    // Wait for aggregators to initialize
    await new Promise(resolve => setTimeout(resolve, 2000));

    // Start all Wavy sensors (Wavy01-Wavy26)
    const allWavyIds = [
        'Wavy01', 'Wavy02', 'Wavy03', 'Wavy04', 'Wavy05', 'Wavy06',
        'Wavy07', 'Wavy08', 'Wavy09', 'Wavy10', 'Wavy11', 'Wavy12',
        'Wavy13', 'Wavy14', 'Wavy15', 'Wavy16', 'Wavy17', 'Wavy18',
        'Wavy19', 'Wavy20', 'Wavy21', 'Wavy22', 'Wavy23', 'Wavy24',
        'Wavy25', 'Wavy26'
    ];

    for (const wavyId of allWavyIds) {
        document.getElementById('wavy-id').value = wavyId;
        await startWavy();
        await new Promise(resolve => setTimeout(resolve, 800)); // Shorter delay for many sensors
    }

    // Show completion message
    addToOutput('server', '\n[MANAGER] Full system startup completed! All 26 Wavy sensors and 5 ocean aggregators are now active.\n');
}

async function quickStartWavySet(setNumber) {
    // Define Wavy sets for different ocean regions
    const wavySets = {
        1: ['Wavy01', 'Wavy02', 'Wavy03'], // Atlantic Ocean
        2: ['Wavy04', 'Wavy05', 'Wavy06'], // North Atlantic
        3: ['Wavy07', 'Wavy08', 'Wavy09'], // Pacific Ocean 
        4: ['Wavy10', 'Wavy11', 'Wavy12'], // South Pacific
        5: ['Wavy13', 'Wavy14', 'Wavy15'], // Indian Ocean
        6: ['Wavy16', 'Wavy17', 'Wavy18'], // Arctic Ocean
        7: ['Wavy19', 'Wavy20', 'Wavy21'], // Southern Ocean
        8: ['Wavy22', 'Wavy23', 'Wavy24'], // Mediterranean/Caribbean
        9: ['Wavy25', 'Wavy26']            // Antarctic Waters
    };

    const wavyIds = wavySets[setNumber];
    if (!wavyIds) {
        alert('Invalid Wavy set number');
        return;
    }

    // Start the Wavys in sequence
    for (let i = 0; i < wavyIds.length; i++) {
        document.getElementById('wavy-id').value = wavyIds[i];
        await startWavy();
        
        // Small delay between starts
        if (i < wavyIds.length - 1) {
            await new Promise(resolve => setTimeout(resolve, 1000));
        }
    }    addToOutput('server', `\n[MANAGER] Quick Start: Ocean monitoring set ${setNumber} deployed (${wavyIds.join(', ')}).\n`);
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

    // Update map markers if map is open
    if (map) {
        updateMapMarkers();
    }
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
function toggleAccordion(id) {
    const content = document.getElementById(`${id}-content`);
    const arrow = document.getElementById(`${id}-arrow`);

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

// Map functionality
let map = null;
let mapMarkers = [];
let componentConfigs = new Map(); // Store component configurations with coordinates

async function openMapTab() {
    const modal = document.getElementById('map-modal');
    modal.style.display = 'flex';

    // Load configuration data if not already loaded
    if (componentConfigs.size === 0) {
        console.log('Loading component configurations from MongoDB...');
        await loadComponentConfigurations();
        console.log(`Loaded ${componentConfigs.size} configurations`);
    }

    // Initialize map if not already done
    if (!map) {
        setTimeout(() => {
            initializeMap();
        }, 100); // Small delay to ensure the modal is visible
    } else {
        // Refresh map size in case of container changes
        setTimeout(() => {
            map.invalidateSize();
            updateMapMarkers();
        }, 100);
    }
}

function closeMapTab() {
    const modal = document.getElementById('map-modal');
    modal.style.display = 'none';
}

// Close modal when clicking outside of it
document.addEventListener('click', (event) => {
    const modal = document.getElementById('map-modal');
    if (event.target === modal) {
        closeMapTab();
    }
});

function initializeMap() {
    map = L.map('map').setView([20, 0], 2); // Center on world view

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 18
    }).addTo(map);

    updateMapMarkers();
}

// Load component configurations from MongoDB
async function loadComponentConfigurations() {
    try {
        const { MongoClient } = require('mongodb');
        const client = new MongoClient('mongodb+srv://sdmongo25:w7KPjneQrqV7aOdH@sistemasdistribuidos.tybz613.mongodb.net/');
        
        await client.connect();
        const db = client.db('RabbitMQ-Communication');

        // Load server configurations from MongoDB
        const serverConfigs = await db.collection('ConfigServer').find({}).toArray();
        serverConfigs.forEach(config => {
            if (config.latitude && config.longitude) {
                componentConfigs.set(`server-${config.server_id}`, {
                    type: 'server',
                    id: config.server_id,
                    continent: config.continent,
                    latitude: parseFloat(config.latitude),
                    longitude: parseFloat(config.longitude)
                });
            }
        });

        // Load aggregator configurations from MongoDB
        const agrConfigs = await db.collection('ConfigAgr').find({}).toArray();
        agrConfigs.forEach(config => {
            if (config.latitude && config.longitude) {
                componentConfigs.set(`aggregator-${config.id}`, {
                    type: 'aggregator',
                    id: config.id,
                    continent: config.continent,
                    latitude: parseFloat(config.latitude),
                    longitude: parseFloat(config.longitude)
                });
            }
        });

        // Load wavy configurations from MongoDB
        const wavyConfigs = await db.collection('ConfigWavy').find({}).toArray();
        wavyConfigs.forEach(config => {
            if (config.latitude && config.longitude) {
                componentConfigs.set(`wavy-${config.WAVY_ID}`, {
                    type: 'wavy',
                    id: config.WAVY_ID,
                    continent: config.continent,
                    latitude: parseFloat(config.latitude),
                    longitude: parseFloat(config.longitude)
                });
            }
        });

        await client.close();
        console.log(`Loaded ${componentConfigs.size} component configurations from MongoDB`);
    } catch (error) {
        console.error('Error loading configurations from MongoDB:', error);
    }
}

function updateMapMarkers() {
    if (!map) return;

    // Clear existing markers
    mapMarkers.forEach(marker => map.removeLayer(marker));
    mapMarkers = [];

    // Get current map bounds for wrapping check
    const bounds = map.getBounds();

    // Iterate over all configured components
    componentConfigs.forEach((config, processKey) => {
        if (!config.latitude || !config.longitude) return;

        // Determine running state
        const isRunning = (config.type === 'server' && runningProcesses.has('server')) || runningProcesses.has(processKey);

        // Choose styling based on type
        let color, fillColor, radius, label;
        switch (config.type) {
            case 'server':
                color = '#ff4444'; fillColor = '#fe4544'; radius = 10; label = 'Server';
                break;
            case 'aggregator':
                color = '#1f1f82'; fillColor = '#4445fe'; radius = 8; label = 'Aggregator';
                break;
            case 'wavy':
                color = '#1c821e'; fillColor = '#44ff44'; radius = 6; label = 'Wavy Sensor';
                break;
            default:
                return;
        }

        // Dim color if not running
        if (!isRunning) {
            color = '#888'; fillColor = '#888';
        }

        // Base position and additional positions for wrapping
        const baseLat = config.latitude;
        const baseLng = config.longitude;
        const positions = [];
        // Original position
        positions.push([baseLat, baseLng]);
        // Check for left duplicate (subtract 360°)
        const leftPos = [baseLat, baseLng - 360];
        if (bounds.contains(L.latLng(leftPos))) {
            positions.push(leftPos);
        }
        // Check for right duplicate (add 360°)
        const rightPos = [baseLat, baseLng + 360];
        if (bounds.contains(L.latLng(rightPos))) {
            positions.push(rightPos);
        }

        // Create markers for each valid position
        positions.forEach(pos => {
            const marker = L.circleMarker(pos, {
                color, fillColor, fillOpacity: 0.8, radius, weight: 2
            }).addTo(map);

            // Popup with status info
            const statusText = isRunning ? 'Running' : 'Stopped';
            const popupContent = `
                <div style="font-family: 'Courier New', monospace; color: #000;">
                    <b>${config.id}</b><br>
                    Type: ${label}<br>
                    Continent: ${config.continent}<br>
                    Status: ${statusText}<br>
                    <small>Lat: ${baseLat.toFixed(4)}, Lng: ${baseLng.toFixed(4)}</small>
                </div>
            `;
            marker.bindPopup(popupContent);
            mapMarkers.push(marker);
        });
    });

    // If no markers found, show a message
    if (mapMarkers.length === 0 && runningProcesses.size > 0) {
        console.log('No coordinate data found for running processes. Make sure configurations are loaded.');
    }
}

// Initialize accordion state on page load
document.addEventListener('DOMContentLoaded', () => {
    // Initialize Quick Start accordion as open by default
    const quickStartContent = document.getElementById('quick-start-content');
    const quickStartArrow = document.getElementById('quick-start-arrow');

    if (quickStartContent && quickStartArrow) {
        quickStartContent.classList.remove('collapsed');
        quickStartArrow.classList.remove('rotated');
        quickStartArrow.textContent = '▼';
    }
});
