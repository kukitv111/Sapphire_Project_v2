# Production audit — 2026-09-23

> Follow-up on 2026-09-23: prepaid tariff purchase, session reservation/settlement,
> and local outbox delivery were implemented after this audit. Findings 1 and 2
> in "Remaining release blockers" below describe the state *at the time of the audit*.
> Current design and remaining constraints are in
> [ADR 0001](decisions/0001-local-outbox-http.md) and
> [ADR 0002](decisions/0002-prepaid-tariff-settlement.md). The new PostgreSQL smoke
> check covers migrations, purchase, concurrent reservation, settlement and inbox.

## Verdict and scope

**Not approved for production release.** Confirmed implementation defects have been repaired and regression-tested, but the product still contains incomplete business flows. PostgreSQL/container execution, load testing, backup restoration and deployment infrastructure could not be verified locally: the Docker Linux engine was unavailable. Passing unit, model and HTTP tests is not equivalent to passing these gates.

Scope: the solution, three API composition roots, authorization and authentication, persistence and outbox, financial primitives, React admin, desktop client, deployment configuration and dependency audit. This is a source and local execution audit, not a penetration test of a deployed environment. The working tree already contained changes in 20 files and an audit plan before this pass; those changes were preserved and extended. No deployment, database update, commit or history rewrite was performed.

## Architecture

The repository contains three independently hosted bounded contexts: Auth, Billing and Session. Each uses Domain / Application / Infrastructure / API layering, MediatR commands and queries, and an independent PostgreSQL database. Shared Kernel contains value objects and events; Shared Security supplies JWT and policies; Shared Messaging contains outbox storage. React calls the APIs directly. The WPF client currently contains a shell window and DTOs, not a complete club workstation client.

This is a distributed service skeleton, not a complete modular monolith: no executable gateway exists, no event transport or outbox consumer is registered, and Session does not integrate with Billing to reserve/debit funds. Shared Observability and Abstractions are largely scaffolding. Architecture tests cover layer/reference boundaries; they cannot prove business completeness or distributed consistency.

## Implementation plan and completed repairs

| Priority | Finding | Repair / evidence |
| --- | --- | --- |
| P0 | Billing allowed an authenticated caller to read another user's wallet | Compare token subject to route user ID; only Cashier/Admin/Owner may cross user boundaries; real-host HTTP regression |
| P0 | Administrative mutations and global session list exposed too broadly | Central policies on tariff mutations, staff-only promo application and session list; real-host 403 regressions |
| P0 | Password identity could be caller-controlled in original implementation | Preserve server-owned identity fix; added test with forged victim ID that verifies both accounts' passwords |
| P0 | Rate limiter registered but no endpoint used it; fixed window shared by all clients | Attach to register/login/refresh, partition by connection IP, return 429; real Auth host test |
| P0 | Outbox serialized IDomainEvent instead of concrete payload | Serialize runtime type and store runtime type name; payload/type regression |
| P0 | Failed save cleared events or left duplicate staged messages | Clear events only after successful save; detach this attempt's messages on failure; include events on child entities; fail-once retry regression |
| P0 | Login queried `.Value` on converted EF properties | Compare complete Username/Email value objects; PostgreSQL SQL-translation regression |
| P1 | Session EF model could not materialize time slots | Explicitly map Start and End; model and persistence regression; generated initial migration |
| P1 | Concurrent session starts and monetary updates could overwrite state | Computer status concurrency token, unique active-session index, balance and promo counter concurrency tokens, unique wallet owner and normalized promo indexes |
| P1 | Per-user promo limits lost after a fresh repository read | Eager-load Usages; reload regression test |
| P1 | Percentage promo interpreted as a number of cents | Reject percentage codes on wallet-credit endpoint; percentage discounts require a purchase flow |
| P1 | Money addition and percentage discount could overflow | Checked addition, decimal intermediate percentage arithmetic; boundary tests |
| P1 | Application failures returned HTTP 200 | Global action filter maps failure status while retaining existing Result JSON envelope for clients |
| P1 | Inactive roles could still produce role/permission claims | Filter inactive roles when issuing JWTs; retain canonical role-name generation from existing changes |
| P1 | No migration history or executable deployment files | Initial EF migrations and design-time factories for all contexts, local EF tool manifest, explicit `--migrate` mode, three non-root Dockerfiles |
| P1 | Known secrets and development deployment defaults | Empty tracked development credentials, reject known JWT/DB placeholders in Production, required Compose variables, Production environment, loopback-only published ports |
| P1 | No operational readiness signal | `/health/live`, `/health/ready` checks pending migrations and outbox access; failure returns 503 without connection details; JSON console logging |
| P2 | Hard-coded CORS and missing error middleware | Configured exact origins; default localhost only in Development; exception middleware in all hosts |
| P2 | Vulnerable test dependencies | WireMock.Net upgraded from 1.6.2 to 2.18.0; repeated full transitive NuGet audit reports no known vulnerabilities |
| P2 | Missing repeatable quality gates | GitHub Actions backend build/tests/migration snapshot checks and frontend install/build/lint/audit; frontend request timeout |

Plan order: identity/access control → persistence and money integrity → model/migrations → deployment/configuration → regression and delivery checks. The earlier draft is retained in `.hermes/plans/2026-09-23-production-audit.md`; this report supersedes its unverified findings (Billing/Session already had outbox queuing, for example).

## Remaining release blockers and follow-up work

1. **P0: Incomplete billing and session lifecycle.** `AssignTariffToUserCommandHandler` still contains an example that credits tariff-price bonuses instead of persisting a purchased tariff entitlement. Session creation does not reserve/debit a wallet, and no complete checkout/session-completion workflow is exposed. Define entitlement, pricing, payment idempotency and refund rules before implementing this as a financial product. Staff authorization limits exposure but does not make this business logic correct.
2. **P0: Event delivery is absent.** Outbox rows are now stored correctly, but there is no dispatcher, durable transport, consumer deduplication or dead-letter/retention policy. Select the delivery contract and implement/verify consumers before relying on cross-service events. Event type names currently contain assembly identity; versioned external contracts are needed before independently upgrading producers and consumers.
3. **P0: Real database/deployment gate remains open.** Run migrations against empty PostgreSQL databases, exercise concurrent wallet/promo/session writes and refresh rotation/replay, and test recovery after an interrupted transaction. The current fail-once test uses EF InMemory and proves event bookkeeping only. After an explicit transaction rollback, discard the request scope/context rather than reusing accepted EF state. Initial migrations must not be blindly applied to existing `EnsureCreated` databases: compare and baseline their schema on a restored copy first.
4. **P1: Production operations are not provisioned.** Configure TLS ingress, trusted proxy addresses, edge/distributed rate limiting, request tracing, alerting, resource limits, database least-privilege credentials and backups with a tested restore procedure. IP limiting uses the actual connection address, intentionally does not trust arbitrary forwarded headers, and is per process. Configure probes in the deployment platform; Compose currently supplies database health checks only.
5. **P1: Identity provisioning and revocation.** Fresh migrations do not provision an administrator. A controlled bootstrap/role-assignment flow is needed. Existing access JWTs remain valid until expiry after password changes or bans; refresh revocation alone does not revoke them immediately. Token DTO expiration currently represents refresh expiry and should be split into explicit access/refresh fields in a versioned client contract.
6. **P1: Client completeness.** React persists tokens in localStorage and lacks a complete refresh/revocation lifecycle; the route guard checks token presence rather than validity. Backend policies remain the security boundary. Decide on a BFF/HttpOnly-cookie session design before treating the admin UI as hardened. The WPF shell has no implemented device locking or session enforcement.
7. **P1: Runtime servicing.** .NET 9 support ends **2026-11-10**, per the [official support policy](https://dotnet.microsoft.com/en-us/platform/support/policy). Plan .NET 10 LTS migration and current servicing patches; this audit did not change the major runtime target. A dependency audit reporting no known advisories is not a support guarantee.
8. **P2: Error and API consistency.** Billing/Session unexpected errors use generic ProblemDetails, whereas application failures retain Result envelopes. Concurrency conflicts fail safely but currently surface as generic errors rather than a dedicated 409/retry contract. Add bounded pagination to user queries and version the API before changing success envelopes.

Credentials previously committed in history must be considered exposed if they were used anywhere. Clearing current files does not rotate external secrets or remove historical copies.

## Verification

- Baseline: 38 architecture + 44 E2E/contract/security tests passed.
- Final: **93 tests passed** (38 architecture + 55 E2E/contract/security/regression), zero failed or skipped; 11 new regression tests in this pass.
- Strict Release solution build: zero errors and warnings.
- New regressions: real Billing/Session authorization, forged password-change identity, actual Auth throttling, outbox runtime serialization and failed-save retry, PostgreSQL login query translation, money boundaries and promo usage reload.
- EF model snapshots: no pending model changes for Auth, Billing or Session; idempotent SQL scripts generated for each without applying them.
- React: `npm ci`, production build and oxlint passed; npm reported zero known vulnerabilities.
- NuGet: full solution transitive vulnerability audit reported no known vulnerable packages after WireMock update.
- Docker: client available, Linux engine unavailable. Images, SQL execution and Compose services were **not** run. CI configuration added but not executed remotely.
- `docker compose config --quiet` passed with temporary validation-only environment values; `git diff --check` passed.

See [deployment runbook](production-runbook.md) for explicit migration/start commands and compatibility notes.
