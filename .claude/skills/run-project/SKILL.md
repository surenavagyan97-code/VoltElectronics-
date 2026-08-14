---
name: run-project
description: Start VoltElectronics locally the fast way (local dev servers) or the full-stack way (Docker). Use whenever asked to run, start, serve, or launch this project.
---

# Running VoltElectronics

This is a two-part app: **backend** (.NET 10 Web API, `backend/`) and **frontend** (Angular 20,
`frontend/`), plus a SQL Server database. Two ways to run it — pick based on what's needed.

## Fast path: local dev servers (use this by default)

Best for iterating on code — hot reload on both sides, no image rebuilds.

1. **Database** — check first whether one is already running before starting a new container:
   ```bash
   docker ps --filter publish=1433
   ```
   If nothing is listening on 1433, start one:
   ```bash
   docker compose up mssql -d
   ```
2. **Backend** (from `backend/`) — reads `appsettings.Development.json`, which already points at
   `localhost,1433` with the same credentials `docker-compose.yml` uses (`sa` / `VoltDev!Passw0rd`).
   No `.env` needed for this path.
   ```bash
   dotnet run --project src/VoltElectronics.Api
   ```
   Listens on **http://localhost:5002** (`/swagger` for the API explorer). First run applies EF
   Core migrations and seeds demo data automatically.
3. **Frontend** (from `frontend/`):
   ```bash
   npm install   # first time only
   npm start
   ```
   Serves on **http://localhost:4200** and proxies `/api` + `/uploads` to `localhost:5002`
   (`proxy.conf.json`) — open the app at :4200, not :5002.

Verify the backend came up:
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/api/categories   # expect 200
```

## Full-stack path: Docker Compose

Best for a clean, production-like run (nginx-served frontend, everything containerized).

```bash
cp .env.example .env   # only if .env doesn't already exist — defaults work out of the box
docker compose up --build
```
- Store: **http://localhost** — API/Swagger: **http://localhost:8080/swagger**
- Admin login: `admin@volt.local` / `Admin123$` (override via `ADMIN_PASSWORD` in `.env`)
- Payments default to the built-in fake gateway (no real credentials needed).

Note: the `backend/Dockerfile` may be stale relative to the current `.slnx`-based solution layout —
if `docker compose up --build` fails on `COPY VoltElectronics.sln`, that file no longer exists
(`VoltElectronics.slnx` replaced it); fix the `COPY`/`dotnet restore` lines in the Dockerfile before
retrying, or just use the local dev path above instead.

## Tests

```bash
cd backend && dotnet test    # pricing, cart merge, checkout/stock, payment callback idempotency
```

## Stopping things

Find and kill a dev server by port:
```bash
lsof -nP -iTCP:5002 -sTCP:LISTEN   # or :4200
kill <pid>
```

## Project-wide rule

Per the repo's `CLAUDE.md`: don't launch/run any of this (dev servers, `docker compose`, browser
automation) unless the user explicitly asks for it in that turn. Build/typecheck (`dotnet build`,
`ng build`) is fine any time to verify changes compile.
