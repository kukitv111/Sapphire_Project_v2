# Sapphire API Contracts

Единая спецификация HTTP-контрактов для всех сервисов Sapphire.
Все ответы оборачиваются в `Result<T>` и возвращаются с HTTP 200:

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": { }
}
```

При ошибке (`isSuccess: false`) `error` содержит `{ code, description, type }`.
Исключение — authn/authz middleware: отсутствует/невалидный токен → HTTP 401,
недостаточная роль → HTTP 403 (это проверяется до входа в action и не заворачивается в Result).
`ApiControllerBase.FromResult` (маппинг ошибок в HTTP-коды) зарезервирован, но пока не используется контроллерами.

## Конфигурация окружения

| Сервис | Local URL (launchSettings) | Docker URL |
|---|---|---|
| Auth API | `http://localhost:5166` | `http://localhost:5001` |
| Billing API | `http://localhost:5191` | `http://localhost:5002` |
| Session API | `http://localhost:5268` | `http://localhost:5003` |
| Gateway (не реализован) | — | `http://localhost:8080` |

Frontend задаёт URL-ы через переменные Vite (`admin-react/.env.example`):
`VITE_AUTH_API_URL`, `VITE_BILLING_API_URL`, `VITE_SESSION_API_URL`, `VITE_GATEWAY_URL`.

Все сервисы читают строку подключения из `ConnectionStrings:DefaultConnection`.
JWT-параметры едины для всех сервисов: секция `Jwt` (Issuer `sapphire-auth`, Audience `sapphire-clients`),
валидация — `Sapphire.Shared.Security.Jwt` (HS256, ValidateIssuer/Audience/Lifetime/IssuerSigningKey, ClockSkew=0).
Secrets задаются через env (`Jwt__SecretKey`, `ConnectionStrings__DefaultConnection`) или user-secrets;
в tracked `appsettings.json` — только placeholders.

## Auth Service (`/api`)

| Method | Path | Request DTO | Response DTO | Auth |
|---|---|---|---|---|
| POST | `/api/auth/register` | `RegisterCommand {username, email, password, phone?}` | `Result<AuthResultDto>` | — |
| POST | `/api/auth/login` | `LoginCommand {login, password}` | `Result<AuthResultDto>` | — |
| POST | `/api/auth/refresh` | `RefreshTokenCommand {refreshToken}` | `Result<AuthResultDto>` | — |
| POST | `/api/auth/change-password` | `ChangePasswordCommand {oldPassword, newPassword}` | `Result` | Bearer |
| GET | `/api/auth/me` | — | `Result<UserDto>` | Bearer |
| GET | `/api/users` | — | `Result<UserDto[]>` | Bearer, role `Admin` |
| GET | `/api/users/{userId}` | — | `Result<UserDto>` | Bearer, role `Admin` |

`AuthResultDto`: `{ user: UserDto, tokens: { accessToken, refreshToken, refreshTokenId, expiresAt } }`.
`UserDto`: `{ id, username, email, phone?, branchId?, bonusBalance, status, isBanned, banReason?, createdAt, lastLoginAt?, roles[] }`.

## Billing Service (`/api/billing`)

| Method | Path | Request DTO | Response DTO | Auth |
|---|---|---|---|---|
| POST | `/api/billing/tariffs` | `CreateTariffCommand` | `Result<TariffDto>` | Bearer |
| GET | `/api/billing/tariffs` | — | `Result<TariffDto[]>` | Bearer |
| GET | `/api/billing/users/{userId}/wallet` | — | `Result<WalletDto>` | Bearer |
| POST | `/api/billing/users/{userId}/tariffs` | `AssignTariffToUserCommand` | `Result<WalletDto>` | Bearer |
| POST | `/api/billing/wallets/{walletId}/promocodes` | `ApplyPromocodeCommand` | `Result<WalletDto>` | Bearer |

`TariffDto`: `{ id, name, type, pricePerMinuteCents, pricePerHourCents, packageDurationMinutes?, packageBonusMinutes?, isActive, isSystem }`.
`WalletDto`: `{ id, userId, mainBalanceCents, bonusBalanceCents }`.
404 `Wallet not found` — если кошелёк пользователя ещё не создан.

## Session Service (`/api/sessions`)

| Method | Path | Request DTO | Response DTO | Auth |
|---|---|---|---|---|
| POST | `/api/sessions` | `StartSessionCommand {computerId, startTime, endTime}` (`userId` из JWT `sub`) | `Result<SessionDto>` | Bearer |
| GET | `/api/sessions?limit=50` | — | `Result<SessionDto[]>` | Bearer |

`SessionDto`: `{ id, computerId, userId, startTime, endTime?, status }` (`Active|Completed|Cancelled`).
`limit` — 1..200, по умолчанию 50, сортировка по `startTime` по убыванию.
400 `INVALID_TIME_SLOT` / `SESSION_TOO_LONG` — невалидный интервал (макс. 3 часа);
404 `COMPUTER_NOT_FOUND`; 409 `COMPUTER_BUSY`.
