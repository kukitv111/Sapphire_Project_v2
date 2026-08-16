# Current Architecture (Observed State)

## Overview
Modular monolith using Clean Architecture + DDD. Solution contains:

- **Shared**: Cross-cutting concerns (Security, Abstractions).
- **Services**: 
  - Auth (JWT, User management)
  - Billing (Pricing, Payments)
  - Session (Computer usage tracking)
- **Client**: 
  - WPF (Employee UI)
  - React (Admin UI, Vite/Tailwind)

## Technology Stack
- **Backend**: ASP.NET Core 9, MediatR, EF Core, PostgreSQL, Redis.
- **Frontend**: React 18, Vite, Shadcn UI.
- **Security**: JWT Bearer tokens, HTTPS.

## Dependency Rules (Current)
1. Domain projects do NOT reference Application/Infrastructure.
2. Application references Domain and Shared.Abstractions.
3. Infrastructure references Application and Domain.
4. No cyclic dependencies between services.

## Known Issues
- JWT options not fully unified across services.
- Frontend build pipeline not fully integrated into CI.

# Target Architecture (Design Decisions)

## Modular Monolith
Continue with modular monolith for Phase 1. Future decomposition into microservices requires:
1. Unified event bus.
2. Service discovery.
3. API gateway.

## Dependency Rules (Target)
Same as current with added:
- **Explicit API** between modules.
- **No shared database** (future state).

# Unverified Assumptions
- PostgreSQL performance meets 500 concurrent users.
- Redis is sufficient for session state.

# Future Ideas
- Introduce CQRS for complex queries.
- Add compensation workflows for Billing.
