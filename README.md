# Sapphire Project

## Overview
Enterprise computer club management system (Gizmo/SmartShell alternative). Stack: .NET 9, CQRS, PostgreSQL, Redis, WPF, React.

## Local Setup

### Prerequisites
- .NET 9 SDK
- Node.js 20+
- Docker (optional)

### Commands

#### Backend
```bash
dotnet restore
dotnet build Sapphire.sln --configuration Release
dotnet test Sapphire.sln --no-build --configuration Release
```

#### Frontend (Admin UI)
```bash
cd admin-react
npm install
npm run build
```

#### Local Run (Docker)
```bash
docker-compose up --build
```

## Architecture
See [docs/architecture](docs/architecture).

## Production readiness
See the [production audit](docs/production-audit-2026-09-23.md) and
[deployment runbook](docs/production-runbook.md) before deployment. Configure
secrets and run explicit migrations before starting API containers. Production
release is not yet approved; the audit records remaining business and operational gaps.

## Security
See [docs/security](docs/security).
