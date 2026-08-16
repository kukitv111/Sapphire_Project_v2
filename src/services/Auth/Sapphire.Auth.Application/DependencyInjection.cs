using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sapphire.Auth.Application.Behaviors;
using System.Reflection;

namespace Sapphire.Auth.Application;

/// <summary>
/// Extension methods for registering Auth Application services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
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

        // Pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
