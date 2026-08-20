# ADR-002: Infrastructure Registration

## Status
Proposed

## Context
API projects should not have direct knowledge of infrastructure implementation details (like DbContext or repository classes). This couples the API to the underlying storage and prevents easy infrastructure changes.

## Decision
1.  **Registration Extensions**: Infrastructure registration is now encapsulated in `DependencyInjection.cs` files in each `Infrastructure` project using `IServiceCollection` extension methods (e.g., `AddAuthInfrastructure`).
2.  **Composition Root**: `Program.cs` in API projects calls these extension methods, acting as the only point where concrete infrastructure is wired up.
3.  **Database Initialisation**: `Database.EnsureCreated()` is moved to a temporary development-only `DatabaseInitializer` class within each infrastructure project. This removes production-inappropriate database logic from API hosts.
4.  **Enforcement**: Use `DependencyInjection` extension methods exclusively for infrastructure. API `Program.cs` must NOT reference `DbContext` types directly for configuration or initialization purposes.
