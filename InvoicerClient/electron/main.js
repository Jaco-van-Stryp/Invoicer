const { app, BrowserWindow, protocol, net } = require('electron');
const path = require('path');
const fs = require('fs');

// Must be called before app.whenReady()
protocol.registerSchemesAsPrivileged([
  { scheme: 'app', privileges: { secure: true, standard: true, supportFetchAPI: true } },
]);

const isDev = !app.isPackaged;
let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 900,
    minWidth: 800,
    minHeight: 600,
    show: false,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
      preload: path.join(__dirname, 'preload.js'),
    },
  });

  mainWindow.once('ready-to-show', () => mainWindow.show());

  const startUrl = isDev ? 'http://localhost:4200/login' : 'app://localhost/login';
  mainWindow.loadURL(startUrl);

  if (isDev) mainWindow.webContents.openDevTools({ mode: 'detach' });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

app.whenReady().then(() => {
  if (!isDev) {
    protocol.handle('app', async (request) => {
      const { pathname } = new URL(request.url);
      const browserDir = path.join(__dirname, '../dist/InvoicerClient/browser');

      let filePath;
      if (path.extname(pathname)) {
        // Asset request (e.g. /main.AbCd.js, /assets/logo.png)
        filePath = path.join(browserDir, pathname);
      } else {
        // Route request — try prerendered index.html first, then SPA fallback
        const routeIndex = path.join(browserDir, pathname, 'index.html');
        filePath = fs.existsSync(routeIndex)
          ? routeIndex
          : path.join(browserDir, 'index.html');
      }

      return net.fetch(new URL(filePath, 'file:///').href);
    });
  }

  createWindow();
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) createWindow();
});
