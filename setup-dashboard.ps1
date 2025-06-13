# Dashboard Setup Script for SD_TRABALHO_2
Write-Host "Setting up Oceanographic Data Dashboard..." -ForegroundColor Cyan

# Check if Python is installed
Write-Host "Checking Python installation..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Python found: $pythonVersion" -ForegroundColor Green
    } else {
        throw "Python not found"
    }
} catch {
    Write-Host "Python not found. Please install Python 3.8+ from https://python.org" -ForegroundColor Red
    Write-Host "Make sure to check Add Python to PATH during installation" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Install Python dependencies
Write-Host "Installing Python dependencies..." -ForegroundColor Yellow
try {
    pip install -r requirements.txt
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Dependencies installed successfully" -ForegroundColor Green
    } else {
        throw "Failed to install dependencies"
    }
} catch {
    Write-Host "Failed to install dependencies. Please check your internet connection and try again" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Dashboard setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run npm start to launch the Distributed System Manager" -ForegroundColor White
Write-Host "2. Click the Dashboard tab in the sidebar" -ForegroundColor White
Write-Host "3. The dashboard will automatically start and show oceanographic data" -ForegroundColor White
Write-Host ""
Write-Host "Manual access:" -ForegroundColor Cyan
Write-Host "You can also run python dashboard_server.py manually" -ForegroundColor White
Write-Host "Then visit http://localhost:5001 in your browser" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to continue"
