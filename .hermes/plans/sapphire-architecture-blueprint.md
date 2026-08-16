# Sapphire — Architecture Blueprint v1.0

> **Document owner:** CTO
> **Status:** Approved for Phase 0
> **Date:** 2026-08-08
> **Classification:** Enterprise · Commercial Product

---

## 1. Vision & Product Definition

Sapphire — коммерческая система управления компьютерными клубами уровня Gizmo, SmartShell, SENET. Multi-branch (сеть клубов), multi-tenant ready, on-premise deployment per operator.

### 1.1 Product pillars

| Pillar | Requirement |
|---|---|
| **Reliability** | Сессия не должна теряться при сетевых сбоях (offline-tolerant client, queue-and-replay) |
| **Billing accuracy** | Денежные операции — строгая согласованность (event-sourced ledger, no floats) |
| **Anti-tamper** | Клиент защищён от kill-process, session hijack, time manipulation |
| **Scalability** | Горизонтальное масштабирование сервисов через stateless API + Redis |
| **Extensibility** | Diskless, Game Launchers, Payment Gateways — подключаются как новые сервисы без изменения ядра |

### 1.2 Non-goals (v1)

- Diskless (архитектурно подготовлено — отдельный сервис, см. §7.7)
- Mobile client (API спроектирован, клиент — позже)
- Cloud SaaS (on-premise, но сервисы cloud-ready)

---

## 2. Architecture Principles

1. **Clean Architecture** — каждый сервис: Domain → Application → Infrastructure → Presentation (inward dependency only).
2. **DDD** — богатая доменная модель в Domain layer; агрегаты, value objects, domain events.
3. **CQRS** — Commands (write path, MediatR) / Queries (read path, dedicated read models через Dapper для сложных выборок).
4. **Eventual consistency между сервисами** — только через async events (Redis Streams / Outbox pattern). Никаких синхронных кросс-сервисных транзакций.
5. **Event Sourcing (только Billing)** — кошелёк пользователя и все финансовые операции — immutable ledger. Баланс = проекция событий. Исключает расхождения денег.
6. **Saga pattern** — длинные бизнес-процессы (бронирование → оплата → сессия) через choreography (events).
7. **Stateless API** — любой экземпляр сервиса обрабатывает любой запрос; состояние — в PostgreSQL/Redis.
8. **API Gateway** — единственная точка входа, JWT validation, rate limiting, routing.
9. **Observability by default** — Serilog structured logging + OpenTelemetry traces + Prometheus metrics на каждом сервисе.
10. **Config as code** — per-service `appsettings.json` + env overrides; secrets — не в репозитории.

---

## 3. System Context & Service Map

```
                        ┌─────────────────────────────┐
                        │        API GATEWAY          │
                        │  (YARP / .NET 9 reverse     │
                        │   proxy + auth + rate limit)│
                        └─────────────┬───────────────┘
        ┌───────────┬─────────┬───────┴───┬───────────┬───────────┐
        │           │         │           │           │           │
   ┌────▼───┐ ┌─────▼──┐ ┌───▼────┐ ┌─────▼────┐ ┌───▼────┐ ┌────▼───┐
   │ Auth   │ │Billing │ │Session │ │  Admin   │ │Client  │ │Statistics│
   │ Service│ │Service │ │Service │ │    API   │ │  API   │ │Service │
   └───┬────┘ └───┬────┘ └───┬────┘ └─────┬────┘ └───┬────┘ └───┬────┘
       │          │         │            │          │          │
       │          │    ┌────▼────┐        │          │    ┌─────▼─────┐
       │          │    │Notification│      │          │    │  Updater  │
       │          │    │ Service  │       │          │    │  Service  │
       │          │    └────┬────┘        │          │    └─────┬─────┘
       │          │         │             │          │          │
       │          │    ┌────▼────┐        │          │    ┌─────▼─────┐
       └──────────┴────►│Monitoring│◄─────┴──────────┴────┤ Launcher  │
                        │ Service  │                      │ Service   │
                        └─────────┘                       └───────────┘
```

### 3.1 Service registry

| # | Service | Responsibility | Owned data (bounded context) |
|---|---|---|---|
| 1 | **Auth Service** | Identity, JWT+Refresh, RBAC, MFA-ready, audit | Users, Roles, Permissions, Sessions(token), ActivityHistory |
| 2 | **Billing Service** | Wallet, tariffs, packages, payments, promocodes, cashback, ledger | Tariffs, TariffPackages, Payments, Transactions, Wallet, PromoCodes, DiscountCards |
| 3 | **Session Service** | Session lifecycle, computer control, reservations, time tracking | Sessions, Reservations, Computers (runtime state) |
| 4 | **Admin API** | Admin read/write façade over other services (BFF pattern) | — (composition only) |
| 5 | **Client API** | Thin BFF for the WPF client: auth flow, session start/stop, wallet ops | — (composition only) |
| 6 | **Statistics Service** | Aggregations, analytics, reports; **read model** built from domain events | Statistics, Aggregates, Reports |
| 7 | **Notification Service** | SignalR hub, push to client/admin, email/SMS/Telegram adapters | Notifications |
| 8 | **Updater Service** | Version manifests, delta packages, rollback metadata | Updates, VersionManifests |
| 9 | **Launcher Service** | Game catalog, install detection, launch orchestration, game paths, Steam/Epic/Riot/Battle.net bindings | Games, InstalledGames, Categories |
| 10 | **Monitoring Service** | Computer health, resource metrics, uptime, alerts, heartbeat ingest | ComputerMetrics, Alerts |
| — | **API Gateway** | Reverse proxy, JWT validation, rate limit, routing | — |
| — | **Diskless Service** *(v2, prepared)* | PXE boot images, iSCSI targets | Will add own bounded context |

### 3.2 Shared infrastructure

| Component | Technology | Purpose |
|---|---|---|
| PostgreSQL 16 | Per-service databases (`sapphire_auth`, `sapphire_billing`, ...) | System of record |
| Redis 7 | Cache, rate-limit counters, SignalR backplane, distributed locks, pub/sub | Hot path |
| Redis Streams | Event bus (Outbox → relay → consumers) | Async integration |
| RabbitMQ *(optional v2)* | Heavy fan-out (statistics, notifications) | Scale-out |
| MinIO/S3 | Update packages, game icons, report exports | Object storage |
| Seq / Loki | Central log aggregation | Observability |
| Prometheus + Grafana | Metrics + dashboards | Observability |
| Keycloak *(not used v1)* | — | Auth остаётся in-house (полный контроль) |

---

## 4. Solution Structure (Monorepo)

```
sapphire/
├── .github/                       # CI/CD workflows
├── docs/
│   ├── architecture/              # этот blueprint + ADRs
│   ├── api/                       # OpenAPI specs (per service)
│   ├── db/                        # migrations (per service)
│   ├── uml/                       # PlantUML sources
│   └── tz/                        # ТЗ на этапы (phase specs)
├── deploy/
│   ├── docker/                    # per-service Dockerfiles
│   ├── compose/                   # docker-compose for dev
│   └── k8s/                       # manifests (prod-ready, on-prem)
├── src/
│   ├── shared/
│   │   ├── Sapphire.Shared.Abstractions/      # contracts: events, DTOs, interfaces
│   │   ├── Sapphire.Shared.Kernel/            # base entities, ValueObjects, Result<T>
│   │   ├── Sapphire.Shared.Messaging/         # outbox, event bus, serialization
│   │   ├── Sapphire.Shared.Observability/     # Serilog, OTel, metrics setup
│   │   └── Sapphire.Shared.Security/          # crypto, hashing, tokens helpers
│   ├── services/
│   │   ├── Auth/
│   │   │   ├── Sapphire.Auth.Domain/
│   │   │   ├── Sapphire.Auth.Application/     # MediatR handlers, CQRS
│   │   │   ├── Sapphire.Auth.Infrastructure/  # EF Core, Redis, outbox
│   │   │   ├── Sapphire.Auth.Api/             # REST controllers, SignalR auth hub
│   │   │   └── Sapphire.Auth.Tests/           # unit + integration
│   │   ├── Billing/        # (same 5-project layout)
│   │   ├── Session/
│   │   ├── AdminApi/       # BFF — Api project only + Application
│   │   ├── ClientApi/      # BFF
│   │   ├── Statistics/
│   │   ├── Notification/
│   │   ├── Updater/
│   │   ├── Launcher/
│   │   └── Monitoring/
│   ├── gateways/
│   │   └── Sapphire.Gateway/                  # YARP config + auth middleware
│   └── clients/
│       ├── Sapphire.Client.Desktop/           # WPF .NET 9, MVVM, Material Design
│       │   ├── Views/ / ViewModels/ / Models/ / Services/ / Converters/
│       │   └── Sapphire.Client.Updater/       # отдельный процесс-апдейтер
│       └── sapphire-admin-web/                # React + TS + Vite + Tailwind + Shadcn
│           ├── src/
│           │   ├── app/ / features/ / components/ / lib/ / api/ / hooks/
│           └── ...
└── tests/
    └── Sapphire.Contract.Tests/               # consumer-driven contract tests
```

### 4.1 Layout convention per service (Clean Architecture)

```
Sapphire.<Service>.Domain/           # Entities, Aggregates, ValueObjects, DomainEvents, Enums, Exceptions
Sapphire.<Service>.Application/      # Commands/Queries, Handlers, DTOs, Interfaces (ports), Validators, Behaviors
Sapphire.<Service>.Infrastructure/   # EF Core (DbContext, migrations), Repositories, Redis, Outbox, HttpClient ports
Sapphire.<Service>.Api/              # Controllers, Middleware, DI composition root, OpenAPI, HealthChecks
Sapphire.<Service>.Tests/            # Unit (xUnit) + Integration (Testcontainers: PG+Redis)
```

**Dependency rule:** `Api → Application ← Domain`, `Infrastructure → Application → Domain`. Domain не знает ни о чём.

---

## 5. ER Diagram — Database Schema

Per-service databases, full normalization. Ниже — ключевые сущности по контекстам.

### 5.1 Auth DB (`sapphire_auth`)

```mermaid
erDiagram
    Users ||--o{ UserRoles : has
    Roles ||--o{ UserRoles : has
    Roles ||--o{ RolePermissions : grants
    Permissions ||--o{ RolePermissions : via
    Users ||--o{ RefreshTokens : owns
    Users ||--o{ ActivityHistory : performs
    Users ||--o{ Admins : "is staff"
    Users ||--o{ Employees : "is employee"
    Branches ||--o{ Employees : assigned
    Branches ||--o{ Computers : contains

    Users {
        uuid id PK
        string username UK
        string email UK
        string phone UK
        string password_hash
        string salt
        uuid branch_id FK
        decimal bonus_balance
        timestamp created_at
        timestamp updated_at
        boolean is_banned
        string ban_reason
    }
    Roles { uuid id PK, string name UK, string description }
    Permissions { uuid id PK, string code UK, string description }
    UserRoles { uuid user_id FK, uuid role_id FK }
    RolePermissions { uuid role_id FK, uuid permission_id FK }
    RefreshTokens { uuid id PK, uuid user_id FK, string token_hash, timestamp expires_at, boolean revoked, string device_info, string ip }
    ActivityHistory { bigint id PK, uuid user_id FK, string action, string entity_type, string entity_id, jsonb metadata, timestamp created_at }
    Admins { uuid user_id PK, uuid branch_id FK, string position }
    Employees { uuid user_id PK, uuid branch_id FK, string position, string employment_type }
    Branches { uuid id PK, string name, string address, string phone, jsonb working_hours, string timezone }
    Computers { uuid id PK, uuid branch_id FK, string name, string ip, string mac, string os, jsonb hardware, string status }
```

### 5.2 Billing DB (`sapphire_billing`)

```mermaid
erDiagram
    Wallets ||--o{ WalletTransactions : has
    Tariffs ||--o{ TariffPackages : bundles
    TariffPackages ||--o{ PackageItems : contains
    Games ||--o{ PackageItems : "included"
    PromoCodes ||--o{ PromoRedemptions : used
    Payments ||--o{ PaymentRefunds : refunded
    Users ||--o{ Wallets : owns
    Users ||--o{ PromoRedemptions : uses

    Wallets { uuid id PK, uuid user_id FK, decimal balance_cents, decimal bonus_cents, int version }
    WalletTransactions {
        bigint id PK
        uuid wallet_id FK
        string type        -- debit/credit/hold/release
        string reason      -- session_payment/topup/promo/cashback/refund/gift
        decimal amount_cents
        decimal balance_after_cents
        uuid reference_id  -- session_id / payment_id
        jsonb metadata
        timestamp created_at
    }
    Tariffs { uuid id PK, string name, string kind, decimal price_per_minute_cents, decimal price_per_hour_cents, time night_start, time night_end, decimal night_coef, boolean is_vip, boolean is_active }
    TariffPackages { uuid id PK, string name, int hours, decimal price_cents, uuid tariff_id FK, boolean is_active }
    PackageItems { uuid package_id FK, uuid game_id FK, string mode }
    PromoCodes { uuid id PK, string code UK, int discount_percent, decimal fixed_discount_cents, timestamp valid_from, timestamp valid_to, int max_uses, int used_count, uuid branch_id FK }
    PromoRedemptions { uuid id PK, uuid promo_id FK, uuid user_id FK, uuid payment_id FK, timestamp created_at }
    Payments { uuid id PK, uuid user_id FK, decimal amount_cents, string method, string status, string provider_ref, jsonb details, timestamp created_at }
    PaymentRefunds { uuid id PK, uuid payment_id FK, decimal amount_cents, string reason, timestamp created_at }
    DiscountCards { uuid id PK, string card_number UK, int discount_percent, uuid user_id FK, timestamp expires_at }
```

**Money rules:** все суммы — `decimal` в **копейках (cents)** как `BIGINT`-совместимые целые, либо `NUMERIC(12,2)` с контрактом "никогда не float". Wallet — optimistic concurrency (`version`), все изменения — только через append в `WalletTransactions` (event-sourced ledger: баланс всегда пересчитываем, никогда не "правим").

### 5.3 Session DB (`sapphire_session`)

```mermaid
erDiagram
    Computers ||--o{ Sessions : runs
    Sessions ||--o{ SessionHeartbeats : emits
    Users ||--o{ Sessions : owns
    Computers ||--o{ Reservations : reserved
    Users ||--o{ Reservations : books
    Tariffs ||--o{ Sessions : billed_with
    Sessions {
        uuid id PK
        uuid computer_id FK
        uuid user_id FK
        uuid tariff_id FK
        uuid reservation_id FK
        timestamp started_at
        timestamp expected_end_at
        timestamp ended_at
        string status        -- pending/active/paused/ended/terminated
        decimal rate_per_minute_cents
        decimal bonus_rate_percent
        string started_by    -- card/phone/employee
        string auth_method
        uuid branch_id FK
    }
    SessionHeartbeats { bigint id PK, uuid session_id FK, timestamp at, string state }
    Reservations {
        uuid id PK
        uuid computer_id FK
        uuid user_id FK
        timestamp start_at
        timestamp end_at
        decimal prepay_cents
        string status         -- pending/confirmed/active/cancelled/expired
        timestamp created_at
        timestamp cancelled_at
    }
```

### 5.4 Launcher DB (`sapphire_launcher`)

```mermaid
erDiagram
    Games ||--o{ GameCategories : categorized
    Categories ||--o{ GameCategories : via
    Games ||--o{ InstalledGames : installed
    Computers ||--o{ InstalledGames : on
    Games {
        uuid id PK
        string title
        string slug UK
        string icon_url
        string cover_url
        bigint size_bytes
        string version
        string install_path_template
        string launch_args
        string steam_id
        string epic_id
        string riot_id
        string battle_net_id
        boolean requires_account
        boolean is_active
    }
    Categories { uuid id PK, string name, string slug UK, int sort_order }
    GameCategories { uuid game_id FK, uuid category_id FK }
    InstalledGames { uuid id PK, uuid computer_id FK, uuid game_id FK, string install_path, string detected_version, timestamp last_detected_at }
```

### 5.5 Updater / Statistics / Monitoring / Notification DBs

```mermaid
erDiagram
    Updates { uuid id PK, string component, string version, string channel, string checksum, bigint size_bytes, string storage_key, string release_notes, timestamp released_at, boolean is_rollback_target }
    Statistics {
        bigint id PK
        uuid branch_id FK
        date day
        string metric            -- revenue/day, avg_load, sessions, top_games
        jsonb value
        timestamp computed_at
        string period            -- day/week/month
    }
    ComputerMetrics { bigint id PK, uuid computer_id FK, timestamp at, float cpu, float ram, float disk, float gpu, float net_in, float net_out, int process_count }
    Alerts { bigint id PK, uuid computer_id FK, string type, string severity, string message, timestamp created_at, timestamp resolved_at }
    Notifications { bigint id PK, uuid user_id FK, string channel, string template_key, jsonb payload, string status, timestamp created_at, timestamp sent_at, timestamp delivered_at }
    Licenses { uuid id PK, string license_key UK, uuid branch_id FK, string edition, timestamp expires_at, int max_computers, boolean is_active }
    Logs { bigint id PK, timestamp at, string level, string service, string message, jsonb context }
    Settings { string key PK, string value, string scope, uuid branch_id FK, string description }
```

---

## 6. REST API Design

### 6.1 Conventions

- `https://<host>/api/<service-prefix>/<resource>`
- Versioning: URL segment `v1` (прост и очевиден для интеграторов).
- Response envelope: `{ data, meta, errors[] }` — единый контракт во всех сервисах.
- Errors: RFC 7807 `application/problem+json`.
- Idempotency: `Idempotency-Key` header на все POST, создающие платежи/сессии.
- Pagination: `?page=&pageSize=` + `X-Total-Count` header. Sorting/filtering — query params, whitelist.
- Timestamps: ISO-8601 UTC. Money: integer cents.
- Auth: `Authorization: Bearer <jwt>`; refresh через `POST /api/v1/auth/refresh`.
- Rate limit: per-token `X-RateLimit-Remaining` headers; 429 + `Retry-After`.

### 6.2 Endpoint map (высокоуровневый)

```
POST   /api/v1/auth/register            {username,email,password}
POST   /api/v1/auth/login               {login,password}            → tokens+user
POST   /api/v1/auth/refresh             {refreshToken}
POST   /api/v1/auth/logout
POST   /api/v1/auth/change-password
GET    /api/v1/auth/me
POST   /api/v1/auth/impersonate         (admin)

GET    /api/v1/wallet                   → balance, bonus
GET    /api/v1/wallet/transactions      (paginated)
POST   /api/v1/wallet/topup             {amount, method}            (payment gateway stub→real)
GET    /api/v1/tariffs                  (active list)
GET    /api/v1/tariffs/{id}
GET    /api/v1/packages
POST   /api/v1/packages/{id}/purchase   {paymentMethod, promoCode?}
POST   /api/v1/promo/validate           {code}
GET    /api/v1/cards                    (discount cards)

GET    /api/v1/computers                (branch filter, online status)
GET    /api/v1/computers/{id}
POST   /api/v1/sessions/start           {computerId, tariffId?, authMethod}
POST   /api/v1/sessions/{id}/stop
POST   /api/v1/sessions/{id}/pause
POST   /api/v1/sessions/{id}/resume
GET    /api/v1/sessions/current
GET    /api/v1/sessions/history         (paginated, own)
POST   /api/v1/reservations             {computerId, startAt, endAt}
DELETE /api/v1/reservations/{id}
GET    /api/v1/reservations/mine

GET    /api/v1/games                    (catalog, categories, search)
GET    /api/v1/games/{id}
POST   /api/v1/games/{id}/launch        → returns launch ticket
GET    /api/v1/branches                 (public list)

--- Admin namespace (RBAC: admin.*, employee.*) ---
GET    /api/v1/admin/dashboard/summary
GET    /api/v1/admin/dashboard/revenue?period=
GET    /api/v1/admin/computers                (CRUD + control)
POST   /api/v1/admin/computers/{id}/shutdown
POST   /api/v1/admin/computers/{id}/reboot
POST   /api/v1/admin/computers/{id}/wake
POST   /api/v1/admin/computers/{id}/lock
POST   /api/v1/admin/computers/{id}/message   {text}
GET    /api/v1/admin/sessions                 (live + history, filters)
POST   /api/v1/admin/sessions/{id}/force-stop
GET/POST/PUT/DELETE /api/v1/admin/tariffs
GET/POST/PUT/DELETE /api/v1/admin/packages
GET/POST/PUT/DELETE /api/v1/admin/games
POST   /api/v1/admin/games/sync-installed     (per computer)
GET/POST/PUT/DELETE /api/v1/admin/users
POST   /api/v1/admin/users/{id}/ban | /unban
POST   /api/v1/admin/users/{id}/adjust-balance
GET/POST/PUT/DELETE /api/v1/admin/employees
GET/POST/PUT/DELETE /api/v1/admin/branches
GET/POST/PUT/DELETE /api/v1/admin/promocodes
GET/POST/PUT/DELETE /api/v1/admin/categories
GET    /api/v1/admin/logs                    (filterable)
GET    /api/v1/admin/reports/revenue?from=&to=&format=csv|xlsx|pdf
GET    /api/v1/admin/reports/sessions?from=&to=
GET    /api/v1/admin/statistics/*            (delegated to Statistics svc)
GET/PUT /api/v1/admin/settings
POST   /api/v1/admin/updates/publish
GET    /api/v1/admin/updates/history

--- Client ↔ server push ---
WS     /hub/client        (SignalR: session state, balance events, server messages)
WS     /hub/admin         (SignalR: live dashboards, computer events, alerts)
WS     /hub/monitoring    (SignalR: resource metrics stream)
```

Полный OpenAPI — отдельный артефакт по каждому сервису (генерируется в Phase 2 из кода, базовые контракты фиксируются в ТЗ).

### 6.3 Key flows

**Session lifecycle (core):**
```
Client POST /sessions/start ──► ClientApi ──► Billing: validate wallet + hold funds (Saga)
      │
      ├─► Session: create session (pending)
      ├─► Session: command computer (via Monitoring/agent) → start
      ├─► Notification: push "session started" to client hub
      ├─► Session: heartbeats (agent → Monitoring → Session)
      └─► Billing: tick-based (1 min) debit WalletTransactions
           │
           ├─ balance exhausted → Session: auto-stop → Notification: push
           └─ user stop → Session: stop → Billing: final settlement + cashback accrual
```

**Cross-service consistency — Outbox:** каждый сервис пишет domain events в свою `outbox` таблицу в той же БД-транзакции, что и бизнес-данные. Relay-процесс публикует их в Redis Streams. Потребители идемпотентны (dedup по `event_id`).

---

## 7. Cross-cutting concerns

### 7.1 Security

| Concern | Solution |
|---|---|
| Passwords | PBKDF2 (100k+ iterations, per-user salt) — `Sapphire.Shared.Security` |
| JWT | HS512/RS256, короткий access (15 min), refresh token (30 days, rotation + reuse detection) |
| Refresh tokens | Hash в БД (не plaintext), device binding, revoke-on-reuse |
| Transport | HTTPS only (TLS 1.3), HSTS; gateway terminates |
| Sensitive data | AES-256-GCM для полей «личный кабинет» (phone, email backup), ключи — в env/secret store |
| RBAC | Permission codes (`session.start`, `admin.computers.control`, ...) → claims в JWT; middleware-проверка на gateway + в сервисах |
| Rate limiting | Fixed-window per-IP/per-user в Redis (auth: 5/min; API: 100/min; wallet: 30/min) |
| Audit | ActivityHistory на все admin-мутации + auth events; append-only |
| Client anti-tamper | Двойной процесс (client + watchdog), WMI/service registration, mutex, integrity check (HMAC подпись собственного exe), kill → watchdog restart, fullscreen kiosk, блокировка Alt+F4/TaskManager через hooks (уровень, допустимый для клубного ПО) |
| Time manipulation | Клиент сверяет время с сервером (NTP-подобный drift detection); серверный tаймер — источник истины для биллинга |

### 7.2 Observability

- Serilog: structured JSON → Seq/Loki; контекст: `correlation_id` (сквозной через все сервисы), `user_id`, `service`.
- OpenTelemetry: distributed traces (W3C traceparent) через Redis-backed activity propagation.
- Prometheus metrics: `sapphire_requests_total`, `sapphire_session_active`, `sapphire_billing_debits_total`, histograms на latency.
- Health checks: liveness/readiness per service; `/health` агрегируется в Monitoring.
- Alerting: алерт «компьютер offline», «очередь outbox > N», «баланс сервиса < N».

### 7.3 Configuration & Secrets

- `appsettings.json` (base) + `appsettings.{Environment}.json` + env vars.
- Dev: `docker-compose` с PG/Redis/Seq/MinIO; seeded demo data.
- Prod: env-driven; secrets — Docker secrets / файл вне репозитория.

### 7.4 CI/CD

- GitHub Actions: build → unit tests → integration tests (Testcontainers) → contract tests → Docker images → push to registry.
- Environments: `dev` (compose), `staging`, `prod`. Versioning: SemVer + git tag; changelog auto.
- Rolling deploy сервисов; **Updater сервис управляет обновлением клиентов и сервера** (см. §7.5).

### 7.5 Updater design (собственный, без сторонних)

- Манифест версий: `{component, version, channel(stable/beta), files[{path, sha256, size}], delta?}` — хранится в Updater DB + object storage.
- Клиент: `Sapphire.Client.Updater.exe` (отдельный процесс) → проверяет манифест при старте/по расписанию → скачивает delta → атомарная замена файлов → restart.
- Сервер: та же механика для сервисов (системный updater agent на хосте).
- **Rollback:** манифест хранит предыдущую версию как `is_rollback_target`; при N failed health-checks после деплоя — автоматический откат; ручной откат из админки.
- Канал обновлений: HTTPS + подпись манифеста (Ed25519) — клиент не примет манифест без валидной подписи.

### 7.6 Client (WPF) architecture

```
Sapphire.Client.Desktop (.NET 9, WPF, MVVM — CommunityToolkit.Mvvm)
├── Views/          ShellWindow, LockScreenView, LoginView, DashboardView,
│                   GameLibraryView, StoreView (time/packages), ProfileView,
│                   HistoryView, BonusView, SessionBarView, AdminMessageView
├── ViewModels/     (one per view, INotifyPropertyChanged, async commands)
├── Services/
│   ├── ApiClient/  (typed HttpClient, JWT attach, refresh interceptor)
│   ├── SignalRClient/ (session hub)
│   ├── SessionManager/ (state machine: idle→auth→session→stop)
│   ├── GameLauncher/ (ticket → process spawn, account injection)
│   ├── TimeSync/   (server time drift detection)
│   ├── AntiTamper/ (watchdog handshake, integrity check)
│   ├── AutoStart/  (registry Run key + service fallback)
│   ├── KioskMode/  (fullscreen, shell replacement policy)
│   └── LocalCache/ (SQLite: offline queue, session state persistence)
└── App.xaml.cs     (composition root, DI, global exception handlers)
```

Клиент — **толстый, но «глупый»**: вся бизнес-логика и валидация на сервере; клиент только отображает состояние и шлёт команды. Оффлайн-режим: сессия не убивается при потере связи на ≤ N секунд (grace window), локальная очередь команд с replay.

### 7.7 Diskless readiness

- Session/Launcher работают через абстракцию `IComputerBootProvider` (порт в Domain, реализация v1 — физический boot/локальный запуск).
- Отдельный bounded context `Diskless` добавится как новый сервис: PXE/DHCP/TFTP/iSCSI, образы. Интеграция — только через существующий event bus и Computer CRUD. Никаких изменений в существующих сервисах, кроме регистрации нового провайдера в DI.

---

## 8. Quality Gates

| Gate | Definition of Done |
|---|---|
| Code | StyleCop/Roslyn analyzers, no warnings; nullable enabled; Async/Await везде (no sync-over-async) |
| Tests | Unit ≥ 70% coverage на Domain/Application; интеграционные тесты на все команды Billing/Session; contract tests на public API |
| Architecture | `netArchTest` — проверка dependency rule (Domain не зависит от Infrastructure) в CI |
| Security | OWASP Top 10 review per phase; dependency scan (Dependabot); no secrets in repo (gitleaks) |
| Performance | P95 API < 100ms (кроме отчётности); сессионные heartbeat — 2s; SignalR broadcast < 50ms |

---

## 9. Roadmap

| Phase | Name | Deliverables | Duration (est.) |
|---|---|---|---|
| **0** | Architecture & Foundations | Blueprint (этот документ), ER, UML, solution scaffold, shared kernel, CI skeleton, compose dev-env | 1 sprint |
| **1** | Auth + RBAC | Auth Service complete (register/login/refresh/roles/permissions/audit), shared Security lib, JWT + refresh rotation, unit+integration tests | 1–2 sprints |
| **2** | Billing core | Wallets, ledger (event-sourced), tariffs, packages, promocodes, discount cards, payment gateway interface (mock + ready for real), wallet API | 2 sprints |
| **3** | Session core | Computer registry, session lifecycle, heartbeat ingestion, reservation flow, Saga: start/stop/pause, auto-stop on empty balance, session API + SignalR events | 2 sprints |
| **4** | Client API + WPF client | ClientApi BFF, lock screen, auth, dashboard, store, balance, history, session control, game launch (ticket), kiosk + anti-tamper, autostart, offline queue | 2–3 sprints |
| **5** | Admin panel | React admin: dashboard, computers control (shutdown/reboot/WoL/lock/message), tariffs, games, users, employees, branches, settings, logs | 2–3 sprints |
| **6** | Launcher + Games | Game catalog CRUD, categories, icons, platform IDs, auto-detection agent, launch orchestration | 1–2 sprints |
| **7** | Statistics + Reports | Event-driven aggregation, revenue/finance analytics, top games/users, avg load, Excel/PDF export | 1–2 sprints |
| **8** | Notification + Monitoring | SignalR hub (client/admin), email/SMS/Telegram adapters, resource monitoring, alerts, computer online control | 1–2 sprints |
| **9** | Updater + Rollout | Updater service, manifest signing, client updater process, server updater agent, rollback, channel management | 1–2 sprints |
| **10** | Hardening & Release | Load testing, security audit, performance pass, docs, demo seed, v1.0 release | 1–2 sprints |

**Total: ~15–20 sprints (≈ 4–5 месяцев одной командой, параллелизуемо по сервисам).**

Порядок жёсткий в начале (Auth → Billing → Session — ядро), далее ветвится: Launcher и Admin могут идти параллельно после Phase 4.

---

## 10. ADR (Architecture Decision Records) — кратко

| # | Decision | Rationale |
|---|---|---|
| ADR-001 | Микросервисы, а не монолит | Изоляция bounded contexts, независимый деплой (обновление сервера без остановки биллинга), масштабирование под нагрузку клубной сети |
| ADR-002 | PostgreSQL per-service | Никаких распределённых транзакций; eventual consistency через outbox |
| ADR-003 | Event Sourcing только в Billing | Деньги — единственный контекст, где immutable ledger обязателен; остальные — классические CRUD (YAGNI) |
| ADR-004 | Redis Streams как bus v1 | Zero-extra-infra, персистентно, consumer groups; RabbitMQ — только если fan-out станет узким местом |
| ADR-005 | BFF (ClientApi/AdminApi) | Клиенты не знают топологию сервисов; сервер контролирует, какие данные уходят клиенту |
| ADR-006 | WPF (не Avalonia/MAUI) | Целевая платформа — Windows-клубы; WPF = зрелость, Material Design, shell-интеграция |
| ADR-007 | Собственный Auth (не IdentityServer/Keycloak) | Полный контроль над анти-тампер-логикой сессий клуба и RBAC-моделью; интеграция OIDC — порт для будущего |
| ADR-008 | Cents (integer money) в API | Исключает float-ошибки; единый контракт для клиентов |
| ADR-009 | In-house Updater | Клубное ПО обновляется под контролем оператора; подпись манифестов; rollback — требование коммерческого SLA |
| ADR-010 | SignalR (не gRPC-stream) для push | Браузерный admin + WPF client из одного стека; backplane на Redis |

---

## 11. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Сложность микросервисов для малого клуба | Docker compose «всё-в-одном» деплой для 1 клуба; k8s — для сетей |
| Eventual consistency в биллинге | Saga + idempotency keys + reconciliation job (сверка ledger и сессий раз в сутки) |
| Анти-тампер не сработает против решительного юзера | Threat model: клубное ПО защищает от «случайного» и «среднего» юзера; физическая безопасность — зона ответственности оператора |
| WPF + .NET 9 обновления | Windows 10/11 LTS-политика; Updater катит рантайм в составе пакета |
| Перегрузка Statistics при пиках | Очереди (bus) + batch aggregation (минутные окна), read model отделён от live-запросов |
| Vendor lock-in платежей | Порт `IPaymentGateway` + фабрика; v1 — mock-провайдер, интеграции (ЮKassa/CloudPayments/Stripe) — как адаптеры |

---

## 12. Что дальше (Phase 0 deliverables)

1. ✅ **Blueprint** (этот документ) — утверждён как архитектурная база
2. **UML diagrams** (PlantUML): context, deployment, component, sequence (session start/stop, topup, reservation) — отдельный артефакт `docs/uml/`
3. **Solution scaffold**: структура репозитория, shared kernel, CI skeleton — ТЗ Phase 0.1
4. **ТЗ Phase 1** (Auth Service) — детальная спецификация для разработчика

---

*Документ подлежит пересмотру только через ADR-процесс. Изменения архитектуры — только после явного согласования.*
