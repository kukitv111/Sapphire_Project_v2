# Sapphire Production Audit and Remediation Plan

**Date:** 2026-09-23
**Scope:** ASP.NET Core services, Clean Architecture boundaries, JWT/RBAC, persistence, API contracts, Docker/runtime, tests, admin frontend.

## Executive findings

The solution currently builds with 0 warnings and 0 errors, but it is not production-ready. The highest-risk defects are authorization bypasses and data-integrity/deployment gaps:

1. **Critical — RBAC is not wired consistently.** `AddSapphireAuthorization()` and `UseSapphireMiddleware()` are not called by any API host. Billing tariff mutation endpoints use only `[Authorize]`, while the existing security tests expect admin-only behavior. `JwtTokenService` emits role GUIDs (`RoleId`) rather than canonical role names (`Admin`, `Owner`, etc.), so role policies cannot work reliably.
2. **Critical — IDOR risk in password change.** `ChangePasswordCommand.UserId` is accepted from the request body and `AuthController` forwards it unchanged. An authenticated caller can target another user's password if the handler is reached with that ID.
3. **Critical — persistence/outbox is incomplete.** `AuthDbContext` uses an `IConfiguration` constructor and `OnConfiguring` instead of registered `DbContextOptions`; all contexts call plain `SaveChangesAsync` and do not capture/queue/clear aggregate domain events into the outbox. There are no migrations, and startup relies on `EnsureCreated` in development-only initializers.
4. **High — API error contract is inconsistent.** Controllers return `Result<T>` directly, so HTTP responses are not consistently mapped through `ApiControllerBase`; the base controller is only used by no production controller. Auth has a global exception handler class but does not register `AddExceptionHandler`/`UseExceptionHandler`.
5. **High — production operational hardening is missing.** Health checks are only present for Auth and are not registered/mapped. Rate limiting is defined but unused. CORS is hard-coded to localhost in all hosts. Swagger and development initializers are present, but there is no production readiness gate for connection strings and no common observability pipeline.
6. **High — secret/configuration policy is contradictory.** Development appsettings contain a known JWT secret and database password; `.gitignore` ignores development settings but the files are tracked in the repository. Production templates must use environment/secret providers and startup validation must reject placeholders/defaults in all production hosts.
7. **Medium — service deployment is incomplete.** `docker-compose.yml` references a Gateway project/Dockerfile not present in the repository, runs APIs as `Development`, and has no API health checks. No database migrations are wired.
8. **Medium — test coverage is concentrated in synthetic security tests.** Existing tests verify isolated middleware and JWT helpers but do not exercise the real service composition roots, real Result HTTP mapping, real RBAC claims, persistence transaction/outbox behavior, or migration startup.
9. **Medium — frontend CI is not integrated.** React has a package/build setup but no repository CI workflow or root verification script tying frontend and backend quality gates together.

## Remediation order

### P0 — authorization and identity safety

- Add and invoke centralized Sapphire authorization/rate-limiting registration and middleware in all three hosts.
- Protect administrative billing mutations with `PolicyNames.AdminOnly`; keep read endpoints authenticated or explicitly public according to the API contract.
- Make password-change identity server-owned: resolve the authenticated subject at the API boundary/application current-user port; remove caller authority over `UserId` for self-service password changes and add an authorization regression test.
- Emit canonical role names from loaded role data, not role IDs; ensure role navigation is loaded and add a token/RBAC regression test.

### P1 — persistence correctness and API contract

- Change `AuthDbContext` to the standard `DbContextOptions<AuthDbContext>` constructor and rely on DI registration.
- Add a shared, explicit unit-of-work/outbox save pipeline per service: collect domain events from tracked aggregate roots, serialize outbox records in the same transaction, clear events only after they are queued, and preserve cancellation/transaction semantics.
- Register and map RFC 7807 exception handling in every host, and make controller responses use one consistent Result-to-HTTP mapping strategy.
- Add migration projects/initial migrations or a documented migration runner path; remove `EnsureCreated` from any production execution path and add startup checks for schema readiness.

### P1 — production security and operations

- Replace tracked development secrets with placeholders only; keep local values in user secrets or ignored environment files. Add production config validation for placeholder DB/JWT values.
- Move CORS origins to configuration, reject wildcard credentials combinations, and add host-specific tests.
- Register health checks for each database and expose liveness/readiness endpoints without leaking connection details.
- Add structured Serilog request logging/correlation IDs and ensure sensitive values are not logged.

### P2 — delivery and maintainability

- Repair or remove the missing Gateway compose entry until its project/Dockerfile exists; add service health checks and production environment defaults.
- Add repository CI for restore, Release build, test, architecture tests, and React install/build/lint.
- Expand architecture tests to cover API composition-root constraints and all service project references; add integration tests against real composition roots where feasible.
- Update architecture/current-state documents so they reflect the actual modular-monolith state, migration strategy, auth policy, and operational limits.

## Files expected to change first

- `src/services/Auth/Sapphire.Auth.Api/Program.cs`
- `src/services/Billing/Sapphire.Billing.Api/Program.cs`
- `src/services/Session/Sapphire.Session.Api/Program.cs`
- `src/services/Auth/Sapphire.Auth.Api/Controllers/AuthController.cs`
- `src/services/Billing/Sapphire.Billing.Api/Controllers/BillingController.cs`
- `src/services/Billing/Sapphire.Billing.Api/Controllers/UserTariffController.cs`
- `src/services/Auth/Sapphire.Auth.Infrastructure/Persistence/AuthDbContext.cs`
- `src/services/Auth/Sapphire.Auth.Infrastructure/Security/JwtTokenService.cs`
- `src/services/Auth/Sapphire.Auth.Infrastructure/Persistence/Repositories/UserRepository.cs`
- `src/shared/Sapphire.Shared.Security/Authorization/SapphireAuthorizationExtensions.cs`
- shared error/HTTP handling and outbox persistence files
- security/JWT and architecture tests
- `appsettings*.json`, `docker-compose.yml`, CI workflow, and architecture docs

## Verification gates

1. Baseline: `dotnet build Sapphire.sln --configuration Release` and `dotnet test Sapphire.sln --configuration Release`.
2. After each P0/P1 batch: targeted tests plus full solution tests.
3. `dotnet build Sapphire.sln --configuration Release --warnaserror` must remain 0/0.
4. `npm ci && npm run build` (and lint if configured) under `admin-react`.
5. Architecture tests must cover all declared service/layer boundaries.
6. Final `git diff`, `git status`, and secret scan must show no tracked credentials or accidental generated files.

## Risks and constraints

- Do not silently redesign the modular-monolith boundaries into microservices.
- Do not add cross-service references to solve identity/authorization; use claims/contracts at the API boundary.
- Database migrations require a deliberate schema baseline; do not fabricate migration contents without validating the EF model.
- Full PostgreSQL/Compose E2E verification may be blocked if Docker is unavailable; report that explicitly rather than treating an in-memory test as equivalent.
