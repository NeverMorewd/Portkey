# Portkey

A cross-platform developer environment dashboard built with Angular + Electron + .NET.

> **Status: Work in Progress**

## Features (in progress)

- Port scanner — view all listening ports and their processes
- Service manager — add, start, stop local services
- Real-time log streaming via SignalR

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 21 + Electron |
| UI | PrimeNG |
| Backend | .NET 10 Minimal API |
| Real-time | SignalR |
| Database | SQLite + EF Core |

## Getting Started

**Backend**
```bash
cd src/backend/Portkey.Api
dotnet run
```

**Frontend (web)**
```bash
cd src/frontend/portkey-app
ng serve
```

**Frontend (desktop)**
```bash
cd src/frontend/portkey-app
npm run electron:start
```

## License

MIT
