using System.Reflection;
using Xunit;
using Sapphire.Auth.Application.Commands.Login;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Billing.Application.Commands.TopUpWallet;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Session.Application.Commands.StartSession;
using Sapphire.Shared.Kernel.Common;
using SessionAggregate = Sapphire.Session.Domain.Aggregates.Session;

namespace Sapphire.Architecture.Tests;

/// <summary>
/// Production assemblies under test. Loaded only via typeof(known public type).Assembly
/// so the test project never depends on bin/obj paths or Assembly.Load(string).
/// </summary>
internal static class ArchitectureAssemblies
{
    public const string Auth = "Sapphire.Auth";
    public const string Billing = "Sapphire.Billing";
    public const string Session = "Sapphire.Session";

    public static readonly string[] Services = [Auth, Billing, Session];

    public static readonly Assembly AuthDomain = typeof(User).Assembly;
    public static readonly Assembly AuthApplication = typeof(LoginCommand).Assembly;
    public static readonly Assembly BillingDomain = typeof(Wallet).Assembly;
    public static readonly Assembly BillingApplication = typeof(TopUpWalletCommand).Assembly;
    public static readonly Assembly SessionDomain = typeof(SessionAggregate).Assembly;
    public static readonly Assembly SessionApplication = typeof(StartSessionCommand).Assembly;
    public static readonly Assembly SharedKernel = typeof(Result).Assembly;

    public static IReadOnlyList<(string Service, Assembly Assembly)> DomainAssemblies { get; } =
    [
        (Auth, AuthDomain),
        (Billing, BillingDomain),
        (Session, SessionDomain)
    ];

    public static IReadOnlyList<(string Service, Assembly Assembly)> ApplicationAssemblies { get; } =
    [
        (Auth, AuthApplication),
        (Billing, BillingApplication),
        (Session, SessionApplication)
    ];

    public static IReadOnlyList<Assembly> ServiceAssemblies { get; } =
    [
        AuthDomain, AuthApplication,
        BillingDomain, BillingApplication,
        SessionDomain, SessionApplication
    ];

    public static Assembly DomainOf(string service) =>
        DomainAssemblies.Single(item => item.Service == service).Assembly;

    public static Assembly ApplicationOf(string service) =>
        ApplicationAssemblies.Single(item => item.Service == service).Assembly;

    public static void EnsureKnownProductionAssembly(Assembly assembly, string expectedName)
    {
        var actual = assembly.GetName().Name;
        Assert.Equal(expectedName, actual);
        Assert.NotEmpty(assembly.GetTypes());
    }
}
