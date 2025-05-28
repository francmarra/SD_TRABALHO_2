const { app, BrowserWindow, ipcMain } = require('electron');
const { spawn } = require('child_process');
const path = require('path');

let mainWindow;
let processes = new Map();

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1200,
        height: 800,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false
        },
        show: false,
        backgroundColor: '#000000',
        titleBarStyle: 'default'
    });

    mainWindow.loadFile('index.html');
    
    mainWindow.once('ready-to-show', () => {
        mainWindow.show();
        
        // Auto-start server after window is shown
        setTimeout(() => {
            const result = startProcess('server', 'Servidor', ['run']);
            if (result.success && mainWindow) {
                mainWindow.webContents.send('server-auto-started');
            }
        }, 1500); // Give UI time to initialize
    });

    mainWindow.on('closed', () => {
        // Terminate all running processes when main window is closed
        processes.forEach((process, id) => {
            if (process && !process.killed) {
                process.kill('SIGTERM');
            }
        });
        processes.clear();
        mainWindow = null;
    });
}

app.whenReady().then(() => {
    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow();
        }
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

// IPC handlers for starting processes
ipcMain.handle('start-server', () => {
    return startProcess('server', 'Servidor', ['run']);
});

ipcMain.handle('start-aggregator', (event, aggregatorId) => {
    return startProcess(`aggregator-${aggregatorId}`, 'Agregador', ['run'], aggregatorId);
});

ipcMain.handle('start-wavy', (event, wavyId) => {
    return startProcess(`wavy-${wavyId}`, 'Wavy', ['run'], wavyId);
});

ipcMain.handle('stop-process', (event, processId) => {
    return stopProcess(processId);
});

ipcMain.handle('get-processes', () => {
    const processList = [];
    processes.forEach((proc, id) => {
        processList.push({
            id,
            alive: proc && !proc.killed,
            pid: proc ? proc.pid : null
        });
    });
    return processList;
});

ipcMain.handle('send-input', (event, processId, input) => {
    const process = processes.get(processId);
    if (process && process.stdin && !process.stdin.destroyed && !process.killed) {
        try {
            process.stdin.write(input + '\n');
            return true;
        } catch (error) {
            console.error('Error sending input to process:', error);
            return false;
        }
    }
    return false;
});

function startProcess(processId, component, args, componentId = null) {
    if (processes.has(processId)) {
        return { success: false, message: `Process ${processId} is already running` };
    }

    try {
        const cwd = path.join(__dirname, component);
        
        // Check if directory exists
        if (!require('fs').existsSync(cwd)) {
            return { success: false, message: `Directory ${cwd} does not exist` };
        }

        const dotnetProcess = spawn('dotnet', args, {
            cwd: cwd,
            stdio: ['pipe', 'pipe', 'pipe'],
            env: { ...process.env, FORCE_COLOR: '0' } // Disable colors for cleaner output
        });

        processes.set(processId, dotnetProcess);

        // Send startup message
        if (mainWindow) {
            mainWindow.webContents.send('process-output', processId, 
                `[MANAGER] Starting ${component}${componentId ? ` with ID: ${componentId}` : ''}...\n`);
        }

        // Send output to renderer
        dotnetProcess.stdout.on('data', (data) => {
            if (mainWindow) {
                const output = data.toString().replace(/\r\n/g, '\n');
                mainWindow.webContents.send('process-output', processId, output);
            }
        });

        dotnetProcess.stderr.on('data', (data) => {
            if (mainWindow) {
                const output = data.toString().replace(/\r\n/g, '\n');
                mainWindow.webContents.send('process-output', processId, `[ERROR] ${output}`);
            }
        });

        dotnetProcess.on('error', (error) => {
            if (mainWindow) {
                mainWindow.webContents.send('process-output', processId, 
                    `[ERROR] Failed to start process: ${error.message}\n`);
                mainWindow.webContents.send('process-closed', processId);
            }
            processes.delete(processId);
        });

        dotnetProcess.on('close', (code) => {
            if (mainWindow) {
                const exitMessage = code === 0 ? 
                    `[MANAGER] Process exited normally\n` : 
                    `[MANAGER] Process exited with code ${code}\n`;
                mainWindow.webContents.send('process-output', processId, exitMessage);
                mainWindow.webContents.send('process-closed', processId);
            }
            processes.delete(processId);
        });

        // If component requires an ID, send it automatically after a delay
        if (componentId) {
            setTimeout(() => {
                if (dotnetProcess.stdin && !dotnetProcess.killed) {
                    try {
                        dotnetProcess.stdin.write(componentId + '\n');
                        if (mainWindow) {
                            mainWindow.webContents.send('process-output', processId, 
                                `[MANAGER] Sent ID: ${componentId}\n`);
                        }
                    } catch (error) {
                        console.error('Error sending component ID:', error);
                    }
                }
            }, 2000); // Increased delay to ensure process is ready
        }

        return { success: true, message: `Started ${component} with ID ${processId}` };
    } catch (error) {
        return { success: false, message: `Failed to start ${component}: ${error.message}` };
    }
}

function stopProcess(processId) {
    const process = processes.get(processId);
    if (process && !process.killed) {
        try {
            if (mainWindow) {
                mainWindow.webContents.send('process-output', processId, 
                    '[MANAGER] Attempting graceful shutdown...\n');
            }

            // Send DLG command first for graceful shutdown (for Wavy and Agregador)
            if (process.stdin && !process.stdin.destroyed) {
                try {
                    process.stdin.write('DLG\n');
                } catch (error) {
                    console.log('Error sending DLG command:', error.message);
                }
            }
            
            // Force kill after 5 seconds if still running
            const forceKillTimer = setTimeout(() => {
                if (!process.killed) {
                    if (mainWindow) {
                        mainWindow.webContents.send('process-output', processId, 
                            '[MANAGER] Force terminating process...\n');
                    }
                    try {
                        process.kill('SIGTERM');
                        // If SIGTERM doesn't work on Windows, try SIGKILL
                        setTimeout(() => {
                            if (!process.killed) {
                                process.kill('SIGKILL');
                            }
                        }, 2000);
                    } catch (error) {
                        console.error('Error force killing process:', error);
                    }
                }
            }, 5000);

            // Clear the timer if process exits gracefully
            process.on('close', () => {
                clearTimeout(forceKillTimer);
            });
            
            return { success: true, message: `Stopping process ${processId}` };
        } catch (error) {
            return { success: false, message: `Failed to stop process ${processId}: ${error.message}` };
        }
    }
    return { success: false, message: `Process ${processId} not found or already stopped` };
}
