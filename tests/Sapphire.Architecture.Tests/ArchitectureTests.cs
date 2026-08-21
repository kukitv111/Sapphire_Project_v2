using Sapphire.Auth.Application.Commands.Login;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Billing.Application.Commands.TopUpWallet;
using Sapphire.Billing.Domain.Aggregates;
using Sapphire.Session.Application.Commands.StartSession;
using Sapphire.Shared.Kernel.Common;
using SessionAggregate = Sapphire.Session.Domain.Aggregates.Session;

namespace Sapphire.Architecture.Tests;

public class ArchitectureTests
{
    public static TheoryData<string> Services
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var service in ArchitectureAssemblies.Services)
            {
                data.Add(service);
            }

            return data;
        }
    }

    [Fact]
    public void Loaded_assemblies_are_real_production_assemblies()
    {
        ArchitectureAssemblies.EnsureKnownProductionAssembly(typeof(User).Assembly, "Sapphire.Auth.Domain");
        ArchitectureAssemblies.EnsureKnownProductionAssembly(typeof(LoginCommand).Assembly, "Sapphire.Auth.Application");
        ArchitectureAssemblies.EnsureKnownProductionAssembly(typeof(Wallet).Assembly, "Sapphire.Billing.Domain");
        ArchitectureAssemblies.EnsureKnownProductionAssembly(typeof(TopUpWalletCommand).Assembly, "Sapphire.Billing.Application");
        ArchitectureAssemblies.EnsureKnownProductionAssembly(typeof(SessionAggregate).Assembly, "Sapphire.Session.Domain");
        ArchitectureAssemblies.EnsureKnownProductionAssembly(typeof(StartSessionCommand).Assembly, "Sapphire.Session.Application");
        ArchitectureAssemblies.EnsureKnownProductionAssembly(typeof(Result).Assembly, "Sapphire.Shared.Kernel");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_does_not_depend_on_Application(string service)
    {
        ArchitectureAssertion.ShouldNotDependOn(ArchitectureAssemblies.DomainOf(service), $"{service}.Application");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_does_not_depend_on_Infrastructure(string service)
    {
        ArchitectureAssertion.ShouldNotDependOn(ArchitectureAssemblies.DomainOf(service), $"{service}.Infrastructure");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_does_not_depend_on_Api(string service)
    {
        ArchitectureAssertion.ShouldNotDependOn(ArchitectureAssemblies.DomainOf(service), $"{service}.Api");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_does_not_depend_on_EF_Core(string service)
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.DomainOf(service),
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Relational",
            "Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_does_not_depend_on_ASPNET(string service)
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.DomainOf(service),
            "Microsoft.AspNetCore",
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.AspNetCore.Http");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_depend_on_Infrastructure(string service)
    {
        ArchitectureAssertion.ShouldNotDependOn(ArchitectureAssemblies.ApplicationOf(service), $"{service}.Infrastructure");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_depend_on_Api(string service)
    {
        ArchitectureAssertion.ShouldNotDependOn(ArchitectureAssemblies.ApplicationOf(service), $"{service}.Api");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_depend_on_EF_Core(string service)
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.ApplicationOf(service),
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Relational",
            "Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_depend_on_ASPNET(string service)
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.ApplicationOf(service),
            "Microsoft.AspNetCore",
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.AspNetCore.Http");
    }

    [Fact]
    public void SharedKernel_does_not_depend_on_service_or_host_projects()
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.SharedKernel,
            "Sapphire.Auth",
            "Sapphire.Auth.Domain",
            "Sapphire.Auth.Application",
            "Sapphire.Auth.Infrastructure",
            "Sapphire.Auth.Api",
            "Sapphire.Billing",
            "Sapphire.Billing.Domain",
            "Sapphire.Billing.Application",
            "Sapphire.Billing.Infrastructure",
            "Sapphire.Billing.Api",
            "Sapphire.Session",
            "Sapphire.Session.Domain",
            "Sapphire.Session.Application",
            "Sapphire.Session.Infrastructure",
            "Sapphire.Session.Api");
    }

    [Fact]
    public void Auth_does_not_depend_on_Billing_or_Session()
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.AuthDomain,
            "Sapphire.Billing",
            "Sapphire.Billing.Domain",
            "Sapphire.Billing.Application",
            "Sapphire.Session",
            "Sapphire.Session.Domain",
            "Sapphire.Session.Application");

        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.AuthApplication,
            "Sapphire.Billing",
            "Sapphire.Billing.Domain",
            "Sapphire.Billing.Application",
            "Sapphire.Session",
            "Sapphire.Session.Domain",
            "Sapphire.Session.Application");
    }

    [Fact]
    public void Billing_does_not_depend_on_Auth_or_Session()
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.BillingDomain,
            "Sapphire.Auth",
            "Sapphire.Auth.Domain",
            "Sapphire.Auth.Application",
            "Sapphire.Session",
            "Sapphire.Session.Domain",
            "Sapphire.Session.Application");

        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.BillingApplication,
            "Sapphire.Auth",
            "Sapphire.Auth.Domain",
            "Sapphire.Auth.Application",
            "Sapphire.Session",
            "Sapphire.Session.Domain",
            "Sapphire.Session.Application");
    }

    [Fact]
    public void Session_does_not_depend_on_Auth_or_Billing()
    {
        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.SessionDomain,
            "Sapphire.Auth",
            "Sapphire.Auth.Domain",
            "Sapphire.Auth.Application",
            "Sapphire.Billing",
            "Sapphire.Billing.Domain",
            "Sapphire.Billing.Application");

        ArchitectureAssertion.ShouldNotDependOnAny(
            ArchitectureAssemblies.SessionApplication,
            "Sapphire.Auth",
            "Sapphire.Auth.Domain",
            "Sapphire.Auth.Application",
            "Sapphire.Billing",
            "Sapphire.Billing.Domain",
            "Sapphire.Billing.Application");
    }
}
