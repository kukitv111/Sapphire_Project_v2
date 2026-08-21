# ADR-001: Dependency Boundaries

## Status

Accepted

## Context

Sapphire is a multi-service Clean Architecture / DDD system (Auth, Billing, Session) with shared DDD primitives in `Sapphire.Shared.Kernel`. Compile-time and runtime coupling must stay one-directional so Domain remains framework-free and bounded contexts stay independent.

## Decision

Layer and module boundaries:

```text
API            → Application, Infrastructure   (composition root only)
Infrastructure → Application, Domain
Application    → Domain
Domain         → Shared.Kernel (domain-neutral primitives only)
Shared.Kernel  → no service-specific project
```

Cross-service concrete references are forbidden:

```text
Auth    ↛ Billing, Session
Billing ↛ Auth, Session
Session ↛ Auth, Billing
```

## Definitions

| Term | Meaning |
| --- | --- |
| Project reference | Compile-time `ProjectReference` in a `.csproj`. Detected by the graph checker. |
| Composition-root registration | API `Program.cs` calling `Add*Infrastructure(...)`. This is an allowed project reference, not a business dependency. |
| Business dependency | A type in Domain or Application that uses a type from a forbidden layer or another bounded context. |
| Runtime dependency | A service resolved through DI. It must still obey the same layer rules as the compile-time graph. |

`API → Infrastructure` is **allowed** as a composition-root project reference so the host can call registration extensions. It is **not** a license for API controllers to construct `DbContext`, repositories, or other persistence types.

## Automated rules

Implemented in `tests/Sapphire.Architecture.Tests` with NetArchTest 1.3.2 against real production assemblies loaded via `typeof(KnownType).Assembly`.

Covered for Auth, Billing and Session Domain:

- Domain ↛ Application
- Domain ↛ Infrastructure
- Domain ↛ API
- Domain ↛ EF Core (`Microsoft.EntityFrameworkCore`, Relational, Npgsql EF)
- Domain ↛ ASP.NET (`Microsoft.AspNetCore`, Mvc, Http)

Covered for Auth, Billing and Session Application:

- Application ↛ Infrastructure
- Application ↛ API
- Application ↛ EF Core
- Application ↛ ASP.NET

Covered for Shared.Kernel:

- Kernel ↛ Auth / Billing / Session (any layer)
- Kernel ↛ API
- Kernel ↛ Infrastructure

Covered for service isolation on loaded Domain and Application assemblies:

- Auth ↛ Billing / Session
- Billing ↛ Auth / Session
- Session ↛ Auth / Billing

Covered for the repository `.csproj` graph:

- No circular `ProjectReference` among discovered projects.
- Domain projects cannot reach same-service Application, Infrastructure or API projects.
- Application projects cannot reach same-service Infrastructure or API projects.
- Shared.Kernel cannot reach service projects.
- Auth, Billing and Session projects cannot reach another service's projects.

Negative validation:

- Cycle detector is proven against a synthetic `A → B → C → A` graph inside the test project.
- The suite does **not** mutate production assemblies (no planted `BadClass` in Domain). Mutation testing of NetArchTest itself is therefore out of scope.

## Manual / out-of-scope rules

These remain review-time or later-prompt work:

- Controllers must not contain business logic (not mechanically scanned).
- API must not resolve `DbContext` except through Infrastructure development helpers.
- Infrastructure types must not leak into Application method signatures beyond interfaces defined in Domain/Application.
- Package-level drift inside `Shared.Kernel` (for example MediatR contracts) is a Kernel-size concern, not a module-isolation concern.
- Runtime-only coupling that never appears as an assembly or project reference.

## Tooling limitations

NetArchTest 1.3.2:

- Inspects IL of **loaded assemblies**, not `.csproj` files.
- `HaveDependencyOn` matches an assembly / namespace name; wildcards like `Sapphire.*.Domain` are not a supported filter API and are not used.
- A missing assembly reference cannot be loaded, so tests must ProjectReference every assembly they inspect. The test project therefore references Domain, Application and Shared.Kernel. It does **not** reference Infrastructure or API, because those layers are not required to prove Domain/Application isolation and because `API → Infrastructure` is an allowed composition-root edge.
- Service isolation of Infrastructure/API assemblies is therefore **not** covered by NetArchTest. Cross-service `ProjectReference` cycles and illegal edges that exist only at csproj level are covered by the graph checker instead.

Project-reference graph checker:

- Locates the repository by walking up from `AppContext.BaseDirectory` until `Sapphire.sln` is found. No machine-specific absolute path is stored.
- Parses SDK-style `ProjectReference Include="..."` via XML, resolves paths relative to each `.csproj`, and DFS-detects cycles.
- Skips `bin/`, `obj/` and `node_modules`.
- Does not interpret `PackageReference`.
- `dotnet list Sapphire.sln reference` is not used: the current SDK rejects a solution file as input.

## Consequences

Architecture tests fail CI when a forbidden compile-time dependency is introduced in Domain, Application or Shared.Kernel, or when `.csproj` references form a cycle. Adding a new bounded context requires extending `ArchitectureAssemblies` with a known public type from that context.
