using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sapphire.Billing.Application;
using Sapphire.Billing.Infrastructure;
using Sapphire.Billing.Infrastructure.Persistence;
using Sapphire.Shared.Security;
using Sapphire.Shared.Security.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
ProductionConfiguration.Validate(builder.Configuration, builder.Environment);

builder.Services.AddControllers(options => options.Filters.Add<ResultStatusFilter>());
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment() ? ["http://localhost:5173"] : []);
if (origins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
    || (uri.Scheme != "https" && uri.Scheme != "http") || uri.AbsolutePath != "/"))
    throw new InvalidOperationException("CORS origins must be absolute HTTP(S) origins");
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (origins.Length > 0)
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddProblemDetails();

builder.Services.AddBillingApplication();
builder.Services.AddBillingInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSapphireAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sapphire Billing API",
        Version = "v1",
        Description = "Billing and payment services for the Sapphire cybercafe platform."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token obtained from the Auth service."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Sapphire.Billing.Infrastructure.Persistence.BillingDbContext>();
    await db.Database.MigrateAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sapphire Billing API v1"));
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();
app.UseSapphireMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();
app.MapGet("/health/ready", async (Sapphire.Billing.Infrastructure.Persistence.BillingDbContext db, CancellationToken ct) =>
{
    try
    {
        if ((await db.Database.GetPendingMigrationsAsync(ct)).Any())
            return Results.StatusCode(503);
        await db.OutboxMessages.AnyAsync(ct);
        return Results.Ok(new { status = "Healthy" });
    }
    catch (Exception) when (!ct.IsCancellationRequested)
    {
        return Results.StatusCode(503);
    }
}).AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program { }
