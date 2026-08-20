using NetArchTest.Rules;
using Xunit;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(System.Reflection.Assembly.Load("Sapphire.Auth.Domain"))
            .ShouldNot().HaveDependencyOn("Sapphire.Auth.Infrastructure")
            .GetResult();
        Assert.True(result.IsSuccessful, "Domain should not depend on Infrastructure");
    }
}
