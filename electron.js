const { app, BrowserWindow, shell } = require('electron');
const path = require('path');
const child_process = require('child_process');
const isDev = require('electron-is-dev'); // To check if running in development

let backendProcess = null;
const backendPort = 5080; // Example port, ensure your .NET app is configured for this

// Adjust this path to where your .NET application will be published/located
// This path assumes electron.js is in a root folder, and the API is in ImageClassifier/ImageClassifier.Api
const backendAppPath = isDev
    ? path.join(__dirname, 'ImageClassifier', 'ImageClassifier.Api', 'bin', 'Debug', 'net8.0', 'ImageClassifier.Api.exe') // Example for dev on Windows
    : path.join(process.resourcesPath, 'app.asar.unpacked', 'backend', 'ImageClassifier.Api.exe'); // Example for packaged app

const backendAppDllPath = isDev
    ? path.join(__dirname, 'ImageClassifier', 'ImageClassifier.Api', 'bin', 'Debug', 'net8.0', 'ImageClassifier.Api.dll')
    : path.join(process.resourcesPath, 'app.asar.unpacked', 'backend', 'ImageClassifier.Api.dll');


function createWindow() {
    const mainWindow = new BrowserWindow({
        width: 1200,
        height: 800,
        webPreferences: {
            // preload: path.join(__dirname, 'preload.js') // If you need a preload script
            nodeIntegration: false, // Recommended for security
            contextIsolation: true, // Recommended for security
        },
        icon: path.join(__dirname, 'assets', 'icon.png') // Assuming you have an icon
    });

    // Load the app once the backend is ready
    const appUrl = `http://localhost:${backendPort}`;

    // Try to connect to the backend, retry a few times
    const tryConnect = (retries = 10, delay = 1000) => {
        const http = require('http');
        const request = http.get(appUrl, (res) => {
            if (res.statusCode === 200 || res.statusCode === 404) { // 404 means server is up but path not found (SPA will handle)
                mainWindow.loadURL(appUrl);
                // Open dev tools if in development
                if (isDev) {
                    mainWindow.webContents.openDevTools();
                }
            } else {
                if (retries > 0) {
                    setTimeout(() => tryConnect(retries - 1, delay), delay);
                } else {
                    console.error('Backend did not start or is not accessible.');
                    app.quit(); // Or show an error window
                }
            }
        });
        request.on('error', (err) => {
            if (retries > 0) {
                setTimeout(() => tryConnect(retries - 1, delay), delay);
            } else {
                console.error('Backend did not start or is not accessible (on error). Error: ', err);
                app.quit(); // Or show an error window
            }
        });
        request.end();
    };

    tryConnect();

    // Open external links in the default browser
    mainWindow.webContents.setWindowOpenHandler(({ url }) => {
        shell.openExternal(url);
        return { action: 'deny' };
    });
}

function startBackend() {
    // Determine if we should run .exe or use dotnet run .dll
    const isWindows = process.platform === "win32";
    let command = 'dotnet';
    let args = [backendAppDllPath, `--urls=http://localhost:${backendPort}`];

    if (isWindows && !isDev) { // In production on Windows, prefer .exe if available
         // For a packaged app, the .exe might be directly available
        const exePath = backendAppPath; // Path to .exe from production build
        // Check if exePath exists, otherwise fallback to dotnet dll
        // This logic needs to be robust based on your packaging structure
        command = exePath; // This assumes backendAppPath points to the executable
        args = [`--urls=http://localhost:${backendPort}`];
    } else if (isWindows && isDev) { // In development on Windows, might use .exe
        command = backendAppPath; // Path to .exe from development build
        args = [`--urls=http://localhost:${backendPort}`];
    }
    // For non-Windows or if .exe is not preferred/found, use 'dotnet <dll_path>'

    console.log(`Starting backend: ${command} ${args.join(' ')}`);
    backendProcess = child_process.spawn(command, args);

    backendProcess.stdout.on('data', (data) => {
        console.log(`Backend STDOUT: ${data}`);
        // You can look for a specific message from the backend indicating it's ready
        // e.g., if (data.toString().includes("Application started.")) tryConnect();
    });

    backendProcess.stderr.on('data', (data) => {
        console.error(`Backend STDERR: ${data}`);
    });

    backendProcess.on('close', (code) => {
        console.log(`Backend process exited with code ${code}`);
        if (code !== 0 && !app.isQuitting) { // If backend crashes or fails to start
            // Optionally show an error message to the user
            // For now, just quit the app
            if (BrowserWindow.getAllWindows().length > 0) {
                 const currentWindow = BrowserWindow.getFocusedWindow() || BrowserWindow.getAllWindows()[0];
                 if(currentWindow) {
                    currentWindow.close(); // This will trigger 'window-all-closed'
                 } else {
                    app.quit();
                 }
            } else {
                app.quit();
            }
        }
    });
}

app.on('ready', () => {
    startBackend();
    createWindow(); // Create window, but loadURL will wait for backend
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('will-quit', () => {
    if (backendProcess) {
        console.log('Attempting to kill backend process.');
        backendProcess.kill();
        backendProcess = null;
    }
});

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
});
