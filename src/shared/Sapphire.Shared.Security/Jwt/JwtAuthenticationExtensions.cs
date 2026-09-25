using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Sapphire.Shared.Security.Jwt;
using System.Text;

namespace Sapphire.Shared.Security.Jwt;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>();
        
        JwtOptionsValidator.Validate(jwtOptions, environment.EnvironmentName);
        services.Configure<JwtOptions>(jwtSection);
        services.AddSingleton(_ => new TokenService(jwtOptions!, environment.EnvironmentName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = GetTokenValidationParameters(jwtOptions!);
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var subject = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        var version = context.Principal?.FindFirst("token_version")?.Value;
                        if (!Guid.TryParse(subject, out var userId) || !long.TryParse(version, out var issuedVersion))
                        {
                            context.Fail("Access token has no version");
                            return;
                        }
                        try
                        {
                            var validator = context.HttpContext.RequestServices
                                .GetRequiredService<ITokenRevocationService>();
                            var state = await validator.GetAsync(userId, context.HttpContext.RequestAborted);
                            if (state is null || !state.Active || state.Version != issuedVersion ||
                                (state.MustChangePassword &&
                                 !context.Request.Path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)))
                                context.Fail("Access token revoked or password change required");
                        }
                        catch (Exception)
                        {
                            context.Fail("Token revocation service unavailable");
                        }
                    }
                };
            });

        return services;
    }

    public static TokenValidationParameters GetTokenValidationParameters(JwtOptions jwtOptions)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                RoleClaimType = SapphireClaims.Role,
                ClockSkew = TimeSpan.Zero
        };
    }
}
