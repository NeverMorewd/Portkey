# Portkey Roadmap

## Phase 1 — Foundation ✅
- [x] Angular 21 + Electron integration
- [x] .NET 10 Minimal API backend (sidecar process)
- [x] Cross-platform port scanner (Windows / Linux / macOS)
- [x] Port list UI with PrimeNG Table
- [x] Dual-mode support: ASP.NET hosted web + Electron desktop
- [x] Environment-based API URL configuration

## Phase 2 — Service Manager ✅
- [x] Add / delete local services
- [x] Start / stop services via process management
- [x] Service status persistence (SQLite + EF Core)
- [x] Toast notifications for operation results
- [x] Graceful process exit detection

## Phase 3 — Real-time Log Streaming ✅
- [x] SignalR Hub for real-time log push
- [x] Stdout / stderr capture and streaming
- [x] Log viewer component (dark terminal style)
- [x] Service status auto-sync on unexpected exit
- [x] Cross-origin SignalR support (dev + prod config)

## Phase 4 — UI & Navigation 🚧
- [ ] Sidebar navigation layout
- [ ] App shell (sidebar + content area)
- [ ] Port page: pie chart by process name (ECharts)
- [ ] Service page: status summary chart
- [ ] System resource monitor (CPU / memory real-time line chart via SignalR)

## Phase 5 — Environment Variable Manager
- [ ] Read / write `.env` files across projects
- [ ] Multi-environment switching (dev / staging / prod)
- [ ] Sensitive value encryption

## Phase 6 — Git Repository Overview
- [ ] Scan local directories for git repositories
- [ ] Display branch, uncommitted changes, last commit
- [ ] LibGit2Sharp integration

## Phase 7 — Health Check
- [ ] Periodic HTTP / TCP health check for running services
- [ ] Auto-update status to `Unhealthy` on failure
- [ ] Visual indicator in service list

## Phase 8 — Engineering Improvements
- [ ] HTTP interceptor for unified error handling (401 / 500)
- [ ] Loading states and error boundaries
- [ ] Unit tests (backend: xUnit, frontend: Vitest)
- [ ] Electron starts .NET backend as child process automatically

## Phase 9 — Packaging & Distribution
- [ ] .NET self-contained publish embedded in Electron package
- [ ] `electron-builder` configuration (Windows / macOS / Linux)
- [ ] GitHub Actions CI/CD pipeline (auto build on tag)
- [ ] Release artifacts on GitHub Releases
