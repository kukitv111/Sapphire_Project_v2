# Deployment and validation runbook

For the production overlay, distinct database roles, TLS ingress, monitoring,
bootstrap and backup operations, follow [production infrastructure](production-infrastructure.md).
The commands below describe the original local/base Compose deployment.

## Configuration

Set independent random `DB_PASSWORD`, `JWT_SECRET_KEY` and `EVENT_TRANSPORT_KEY` values in an ignored `.env` file or the deployment secret store. JWT and event transport secrets must each be at least 32 characters. Set `ADMIN_ORIGIN` to the exact admin frontend origin. The example contains empty values intentionally; copying it without configuring secrets must not start the services.

For local `dotnet run`, provide `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, `Messaging__SharedSecret`, and optionally `Cors__AllowedOrigins__0` in that process's environment or user secrets. Session also needs `Billing__InternalUrl`; each host needs `Messaging__Subscribers__N` for event delivery. Tracked development settings contain no working credentials. All three services must agree on JWT issuer, audience and secret. Never use a shared development/test signing key in production.

## Empty-database deployment

With a running Docker Linux engine and configured environment:

```sh
docker compose build
docker compose up -d --wait auth-db billing-db session-db
docker compose run --rm auth-api --migrate
docker compose run --rm billing-api --migrate
docker compose run --rm session-api --migrate
docker compose up -d auth-api billing-api session-api
```

Stop immediately if any migration command fails. Migration mode applies migrations and exits; ordinary startup does not mutate the schema. Use deployment migration credentials for migration jobs and narrower runtime credentials in a real installation. Initial schema creation does not provision roles or an administrator.

The API ports are published on loopback: 5001 Auth, 5002 Billing, 5003 Session. PostgreSQL ports 5433–5435 are also loopback-only. There is no gateway on port 8080. Place a configured TLS reverse proxy in front of the APIs before external access.

Probe `/health/live` for process health and `/health/ready` for database/schema readiness. Readiness returns 503 for unavailable databases or unapplied migrations; it does not verify event delivery, business workflows or every column. The runtime container does not install curl; configure HTTP probes externally.

The Session API reserves prepaid tariff credit synchronously from Billing before
starting a session. `session.completed.v1` and `session.cancelled.v1` settle or
release that reserve asynchronously. Monitor old reservations and outbox rows
with `DeadLetterAt` set; investigate and explicitly reset `RetryCount`, `Error`,
`DeadLetterAt`, and `NextAttemptAt` only after correcting the cause. Keep
`/internal/*` inaccessible from the public ingress. See the
[transport decision](decisions/0001-local-outbox-http.md) for failure handling.

## Existing databases

Take a backup, verify restoring it, compare its schema with the generated initial migrations and establish a reviewed migration baseline on a restored copy. Do not apply InitialCreate blindly to a database previously initialized with EnsureCreated. Do not drop data to make migration errors disappear. The audit did not access or migrate a live database.

## Local quality gates

```sh
dotnet tool restore
dotnet restore Sapphire.sln
dotnet build Sapphire.sln -c Release --no-restore --warnaserror
dotnet test Sapphire.sln -c Release --no-build
dotnet list Sapphire.sln package --vulnerable --include-transitive
dotnet ef migrations has-pending-model-changes --project src/services/Auth/Sapphire.Auth.Infrastructure
dotnet ef migrations has-pending-model-changes --project src/services/Billing/Sapphire.Billing.Infrastructure
dotnet ef migrations has-pending-model-changes --project src/services/Session/Sapphire.Session.Infrastructure
cd admin-react
npm ci
npm run build
npm run lint
npm audit --omit=dev
```

The full solution includes WPF and is built on Windows in CI. Container builds target API projects only. Generate reviewable migration SQL with `dotnet ef migrations script --idempotent --project <Infrastructure-project> --output <file.sql>`.

For a disposable local PostgreSQL integration check with a running Docker engine:

```sh
python tests/Sapphire.Postgres.Smoke/run_temp_postgres.py
```

This creates and removes a distinct Compose project with disposable Auth,
Billing, Session and restore volumes. It runs each API's `--migrate` against
an empty database, checks runtime DDL denial, concurrent one-PC starts,
wallet debit/credit and one-use promo, refresh replay, an interrupted write,
one-time admin bootstrap, and a backup-script dump restored on another
PostgreSQL instance. It does not touch the base Compose volumes or existing
databases.

## API compatibility changes

- **Breaking Auth token DTO change:** `expiresAt` was removed and replaced by
  `accessTokenExpiresAt` and `refreshTokenExpiresAt`; update clients before
  deploying the APIs. First-login users receive `user.mustChangePassword` and
  must use `/api/auth/change-password` before other authenticated routes.

- Result failures now use meaningful non-2xx HTTP status codes; JSON Result envelopes remain unchanged. Axios callers must handle rejections.
- Wallet reads require ownership or staff role; promo application and session lists require Cashier/Admin/Owner. Tariff creation requires Admin/Owner.
- Percentage promos are rejected on the wallet-credit route; that route only supports fixed credits.
- Auth register/login/refresh share a ten-request-per-minute budget per connection IP and process. Configure trusted proxies and an edge limiter for multi-instance deployments.
- Sessions lists are limited to 1–200 items.
- Database/model concurrency protection requires fresh reads after conflicts; do not blindly retry financial operations without idempotency keys.
- Tariff purchases require an `Idempotency-Key` header and debit the user's
  wallet once to create prepaid credit. Package tariffs require
  `PackagePriceCents` when created. The purchase response contains the
  entitlement and wallet payment transaction IDs.
- Session start requires `EntitlementId`; completion is
  `POST /api/sessions/{id}/complete`. Staff cancellation is
  `POST /api/sessions/{id}/cancel` and refunds the entire reserve. Ordinary
  early completion charges elapsed time and refunds only unused reserve.

Before production release, verify live HTTP delivery, workload concurrency,
fault reconciliation, network isolation and deployment/backup procedures.
External payment provider integration remains an explicit TODO.
