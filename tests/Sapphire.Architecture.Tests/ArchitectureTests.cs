using NetArchTest.Rules;
using Xunit;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Application.Commands.Login;

namespace Sapphire.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly Types AuthDomain = Types.InAssembly(typeof(User).Assembly);
    private static readonly Types AuthApplication = Types.InAssembly(typeof(LoginCommand).Assembly);

    [Fact]
    public void Domain_ShouldNotDependOnApplication()
    {
        var result = AuthDomain.ShouldNot().HaveDependencyOn("Sapphire.Auth.Application").GetResult();
        Assert.True(result.IsSuccessful, "Domain should not depend on Application");
    }

    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        var result = AuthDomain.ShouldNot().HaveDependencyOn("Sapphire.Auth.Infrastructure").GetResult();
        Assert.True(result.IsSuccessful, "Domain should not depend on Infrastructure");
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        var result = AuthApplication.ShouldNot().HaveDependencyOn("Sapphire.Auth.Infrastructure").GetResult();
        Assert.True(result.IsSuccessful, "Application should not depend on concrete Infrastructure");
    }
}
