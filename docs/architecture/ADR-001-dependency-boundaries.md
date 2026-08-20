# ADR-001: Dependency Boundaries

## Status
Proposed

## Context
The project follows Clean Architecture. To maintain loose coupling, we must enforce strict layer boundaries.

## Rules
1. **Domain**: Must not depend on Application, Infrastructure, or API.
2. **Application**: Must not depend on Infrastructure or API.
3. **Infrastructure**: Depends on Domain and Application.
4. **API**: Composition root, depends on all layers to wire up dependencies.

## Definitions
- **Project Reference**: Used for compile-time dependencies.
- **Composition Root**: API projects that register services into `IServiceCollection`.
- **Business Dependency**: High-level logic dependencies (e.g., Domain Services).
- **Runtime Dependency**: Dependencies resolved via DI.

## Enforcement
Architecture tests in `Sapphire.Architecture.Tests` enforce these rules via `NetArchTest.Rules`.
Violations fail the build.
