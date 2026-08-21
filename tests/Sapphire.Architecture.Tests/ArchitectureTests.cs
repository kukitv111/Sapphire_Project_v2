using NetArchTest.Rules;
using Xunit;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Session.Domain.Aggregates;
using System.Reflection;

namespace Sapphire.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly Assembly AuthDomain = typeof(User).Assembly;
    private static readonly Assembly BillingDomain = typeof(Wallet).Assembly;
    private static readonly Assembly SessionDomain = typeof(Session.Domain.Aggregates.Session).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnApplication_AllServices()
    {
        var result = Types.InAssemblies(new[] { AuthDomain, BillingDomain, SessionDomain })
            .That().ResideInNamespace("Sapphire.*.Domain")
            .ShouldNot().HaveDependencyOn("Sapphire.*.Application")
            .GetResult();
        Assert.True(result.IsSuccessful, "Domains should not depend on Application");
    }

    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure_AllServices()
    {
        var result = Types.InAssemblies(new[] { AuthDomain, BillingDomain, SessionDomain })
            .That().ResideInNamespace("Sapphire.*.Domain")
            .ShouldNot().HaveDependencyOn("Sapphire.*.Infrastructure")
            .GetResult();
        Assert.True(result.IsSuccessful, "Domains should not depend on Infrastructure");
    }
    
    [Fact]
    public void Domain_ShouldNotDependOnFrameworks()
    {
        var result = Types.InAssemblies(new[] { AuthDomain, BillingDomain, SessionDomain })
            .That().ResideInNamespace("Sapphire.*.Domain")
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();
        Assert.True(result.IsSuccessful, "Domain should not depend on EF Core or ASP.NET");
    }
}
