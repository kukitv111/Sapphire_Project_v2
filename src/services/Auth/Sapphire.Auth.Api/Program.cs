using Microsoft.EntityFrameworkCore;
using Sapphire.Shared.Messaging.Outbox;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sapphire.Auth.Api.Services;
using Sapphire.Auth.Application;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Infrastructure;
using Sapphire.Auth.Infrastructure.Security;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Entities;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Auth.Application.Interfaces.Security;
using Sapphire.Shared.Security;
using Sapphire.Shared.Security.Jwt;
using Sapphire.Shared.Security.Telemetry;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.Services.AddSapphireTelemetry(builder.Configuration, "sapphire-auth");
ProductionConfiguration.Validate(builder.Configuration, builder.Environment);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);
builder.Services.AddScoped<ITokenRevocationService, LocalTokenRevocationService>();
builder.Services.AddScoped<IIncomingEventHandler, NoOpIncomingEventHandler>();
builder.Services.AddOutboxTransport<Sapphire.Auth.Infrastructure.Persistence.AuthDbContext>();

builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddExceptionHandler<Sapphire.Auth.Api.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSapphireAuthorization();

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Sapphire.Auth.Infrastructure.Persistence.AuthDbContext>();
    await db.Database.MigrateAsync();
    return;
}

if (args.Contains("--bootstrap-admin", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Sapphire.Auth.Infrastructure.Persistence.AuthDbContext>();
    var username = builder.Configuration["Bootstrap:AdminUsername"]
        ?? throw new InvalidOperationException("Bootstrap:AdminUsername is required");
    var email = builder.Configuration["Bootstrap:AdminEmail"]
        ?? throw new InvalidOperationException("Bootstrap:AdminEmail is required");
    var password = builder.Configuration["Bootstrap:AdminPassword"]
        ?? throw new InvalidOperationException("Bootstrap:AdminPassword is required");
    Password.ValidatePlainText(password);
    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
    if (await db.BootstrapRecords.AnyAsync() ||
        await db.Roles.AnyAsync(r => r.NormalizedName == "ADMIN"))
        throw new InvalidOperationException("Administrator bootstrap already completed");
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var (hash, salt) = hasher.HashPassword(password);
    var admin = User.Create(username, email, Password.FromHash(hash, salt));
    admin.RequirePasswordChange();
    var role = Role.Create("Admin", "Bootstrap administrator", isSystem: true);
    admin.AssignRole(role, admin.Id);
    db.Roles.Add(role);
    db.Users.Add(admin);
    db.BootstrapRecords.Add(new Sapphire.Auth.Infrastructure.Persistence.BootstrapRecord());
    await db.SaveChangesAsync();
    await transaction.CommitAsync();
    Console.WriteLine("Administrator bootstrap completed; password change required on first login.");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseTrustedProxyHeaders(builder.Configuration);
app.UseSapphireCorrelation();
app.UseExceptionHandler();
app.UseSapphirePoolDiagnostics();
app.UseHttpsRedirection();
app.UseCors();
app.UseSapphireMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();
app.MapGet("/health/ready", async (Sapphire.Auth.Infrastructure.Persistence.AuthDbContext db, CancellationToken ct) =>
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
app.MapOutboxInbox<Sapphire.Auth.Infrastructure.Persistence.AuthDbContext>();
app.MapGet("/internal/token-version/{userId:guid}", async (Guid userId,
    HttpContext http, ITokenRevocationService tokens, IConfiguration config, CancellationToken ct) =>
{
    var secret = config["Messaging:SharedSecret"];
    var supplied = http.Request.Headers["X-Sapphire-Message-Key"].ToString();
    if (string.IsNullOrWhiteSpace(secret) || supplied.Length != secret.Length ||
        !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(secret)))
        return Results.Unauthorized();
    var state = await tokens.GetAsync(userId, ct);
    return state is null ? Results.NotFound() : Results.Ok(state);
}).AllowAnonymous();

app.Run();

public partial class Program { }
