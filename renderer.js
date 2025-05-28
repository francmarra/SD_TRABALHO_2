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

function updateTerminalContent() {
    const terminalContent = document.getElementById('terminal-content');
    
    if (currentComponent && componentOutputs.has(currentComponent)) {
        const output = componentOutputs.get(currentComponent);
        const isRunning = runningProcesses.has(currentComponent);
        const status = isRunning ? 'RUNNING' : 'STOPPED';
        const statusColor = isRunning ? '#00ff00' : '#ff6666';
        
        terminalContent.innerHTML = `
            <div style="color: #00aaaa; margin-bottom: 10px; border-bottom: 1px solid #333; padding-bottom: 5px;">
                [${currentComponent.toUpperCase()}] - <span style="color: ${statusColor}">${status}</span>
            </div>` + 
            formatOutput(output);
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

function addToOutput(componentId, text) {
    if (!componentOutputs.has(componentId)) {
        componentOutputs.set(componentId, '');
    }
    componentOutputs.set(componentId, componentOutputs.get(componentId) + text);
    
    if (currentComponent === componentId) {
        updateTerminalContent();
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
