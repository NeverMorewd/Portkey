import { app, BrowserWindow } from 'electron';
import * as path from 'path';
import * as child_process from 'child_process';
import * as http from 'http';

let mainWindow: BrowserWindow | null = null;
let backendProcess: child_process.ChildProcess | null = null;

const BACKEND_PORT = 5000;
const HEALTH_URL = `http://localhost:${BACKEND_PORT}/health`;

// ── Backend launcher ──────────────────────────────────────────────────────────

function spawnBackend(): void {
  const isDev = !app.isPackaged;

  if (isDev) {
    const backendDir = path.resolve(__dirname, '../../../../backend/Portkey.Api');
    backendProcess = child_process.spawn(
      'dotnet', ['run', '--no-build', '--project', backendDir],
      {
        env: { ...process.env, ASPNETCORE_URLS: `http://localhost:${BACKEND_PORT}` },
        stdio: 'pipe',
      }
    );
  } else {
    const ext = process.platform === 'win32' ? '.exe' : '';
    const exePath = path.join(path.dirname(app.getPath('exe')), `Portkey.Api${ext}`);
    backendProcess = child_process.spawn(exePath, [], {
      env: { ...process.env, ASPNETCORE_URLS: `http://localhost:${BACKEND_PORT}` },
      stdio: 'pipe',
    });
  }

  backendProcess.on('error', err =>
    console.error('[backend] Failed to start:', err.message));
  backendProcess.stdout?.on('data', (d: Buffer) =>
    console.log('[backend]', d.toString().trim()));
  backendProcess.stderr?.on('data', (d: Buffer) =>
    console.error('[backend]', d.toString().trim()));
}

function waitForBackend(timeoutMs = 30_000): Promise<void> {
  return new Promise((resolve, reject) => {
    const deadline = Date.now() + timeoutMs;

    const poll = () => {
      if (Date.now() > deadline) {
        reject(new Error('Backend did not become ready within 30 s'));
        return;
      }
      http.get(HEALTH_URL, res => {
        if (res.statusCode === 200) resolve();
        else setTimeout(poll, 600);
        res.resume();
      }).on('error', () => setTimeout(poll, 600));
    };

    setTimeout(poll, 1200); // initial grace period
  });
}

// ── Window ────────────────────────────────────────────────────────────────────

function createWindow(): void {
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

  mainWindow.on('closed', () => { mainWindow = null; });
}

// ── App lifecycle ─────────────────────────────────────────────────────────────

app.whenReady().then(async () => {
  spawnBackend();
  try {
    await waitForBackend();
  } catch (e) {
    console.error('Backend did not start; continuing anyway.', e);
  }
  createWindow();
});

app.on('window-all-closed', () => {
  if (backendProcess) {
    backendProcess.kill();
    backendProcess = null;
  }
  if (process.platform !== 'darwin') app.quit();
});

app.on('before-quit', () => {
  if (backendProcess) {
    backendProcess.kill();
    backendProcess = null;
  }
});
