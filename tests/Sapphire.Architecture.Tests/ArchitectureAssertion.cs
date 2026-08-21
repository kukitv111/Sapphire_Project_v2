using System.Reflection;
using System.Text;
using NetArchTest.Rules;
using Xunit;

namespace Sapphire.Architecture.Tests;

internal static class ArchitectureAssertion
{
    public static void ShouldNotDependOn(Assembly assembly, string forbiddenDependency)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenDependency)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(assembly, forbiddenDependency, result));
    }

    public static void ShouldNotDependOnAny(Assembly assembly, params string[] forbiddenDependencies)
    {
        foreach (var forbidden in forbiddenDependencies)
        {
            ShouldNotDependOn(assembly, forbidden);
        }
    }

    private static string FormatFailure(Assembly assembly, string forbiddenDependency, TestResult result)
    {
        var builder = new StringBuilder();
        builder.Append(assembly.GetName().Name)
            .Append(" must not depend on ")
            .Append(forbiddenDependency)
            .Append('.');

        if (result.FailingTypeNames is { } names && names.Any())
        {
            builder.Append(" Failing types: ").Append(string.Join(", ", names));
        }

        return builder.ToString();
    }
}
