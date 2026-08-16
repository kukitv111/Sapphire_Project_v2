using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sapphire.Auth.Api.Services;
using Sapphire.Auth.Application;
using Sapphire.Auth.Application.Interfaces;
using Sapphire.Auth.Infrastructure;
using Sapphire.Shared.Security.Jwt;

public partial class Program { }

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton(_ =>
{
    var options = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
    if (options == null)
    {
        throw new InvalidOperationException("JWT configuration is missing");
    }
    if (string.IsNullOrEmpty(options.SecretKey))
    {
        throw new InvalidOperationException("JWT secret key is missing or empty");
    }
    return new TokenService(options);
});
builder.Services.AddAuthApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        if (string.IsNullOrEmpty(jwtOptions.SecretKey))
        {
            throw new InvalidOperationException("JWT secret key is missing or empty");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

// Add controllers and OpenAPI
builder.Services.AddControllers();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Sapphire.Auth.Infrastructure.Persistence.AuthDbContext>();
    db.Database.EnsureCreated();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
