import { app, BrowserWindow } from 'electron';
  import * as path from 'path';

  let mainWindow: BrowserWindow | null = null;

  function createWindow() {
    mainWindow = new BrowserWindow({
      width: 1280,
      height: 800,
      webPreferences: {
        preload: path.join(__dirname, 'preload.js'),
        contextIsolation: true,
        nodeIntegration: false,
      },
    });
    const mainPage = path.join(__dirname, '../dist/portkey-app/browser/index.html');

    mainWindow.loadFile(mainPage);
  }

  app.whenReady().then(createWindow);

  app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit();
  });

