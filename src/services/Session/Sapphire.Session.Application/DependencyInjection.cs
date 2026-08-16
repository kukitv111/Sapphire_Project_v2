using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Sapphire.Session.Application;

/// <summary>
/// Extension methods for registering Session Application services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
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

        return services;
    }
}
