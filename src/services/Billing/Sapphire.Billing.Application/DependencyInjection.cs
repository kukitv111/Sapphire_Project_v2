using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Sapphire.Billing.Application;

/// <summary>
/// Extension methods for registering Billing Application services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // AutoMapper — register all profiles found in the assembly
        services.AddAutoMapper(cfg =>
        {
            foreach (var profileType in assembly.GetTypes()
                         .Where(t => t.IsClass && !t.IsAbstract && typeof(Profile).IsAssignableFrom(t)))
            {
                cfg.AddProfile(profileType);
            }
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
