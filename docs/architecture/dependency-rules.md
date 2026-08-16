# Dependency Management Rules

## Mandatory Rules (Enforced)
1. **Domain Isolation**: Domain projects MUST NOT depend on Application, Infrastructure, or external frameworks.
2. **Directed Dependencies**: 
   - Application → Domain
   - Infrastructure → Application, Domain
   - Shared → All
3. **Service Independence**: Services (Auth, Billing, Session) MUST NOT reference each other.
4. **Explicit API**: Public contracts MUST be defined in `*.Abstractions` projects.

## Recommended Rules
- **Small Interfaces**: Prefer small, role-specific interfaces over large base classes.
- **Dependency Inversion**: Depend on abstractions, not concretions.
- **No Framework in Domain**: Domain code MUST NOT use ASP.NET, EF Core, or other infrastructure frameworks.

## Tooling
Use [NDepend](https://www.ndepend.com/) for:
- Cyclic dependency detection.
- Architecture tests.
- Rule enforcement in CI.

## Future Rules (Planned)
- **Database Isolation**: Each service MUST have its own database schema.
- **Event-Driven Communication**: Services communicate via events only.
