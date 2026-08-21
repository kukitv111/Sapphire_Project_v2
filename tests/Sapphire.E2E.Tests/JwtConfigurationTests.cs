using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Sapphire.Shared.Security.Jwt;
using Xunit;

namespace Sapphire.E2E.Tests;

public sealed class JwtConfigurationTests
{
    private const string Secret = "test-secret-32-chars-placeholder-ok!!";
    private const string OtherSecret = "another-test-secret-32-chars-ok!!";
    private const string Issuer = "sapphire-auth";
    private const string Audience = "sapphire-clients";

    [Fact]
    public void MissingJwtOptions_AreRejected()
    {
        var act = () => JwtOptionsValidator.Validate(null, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT configuration is missing");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptySecret_IsRejected(string secret)
    {
        var options = ValidOptions() with { SecretKey = secret };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT SecretKey is missing or empty");
    }

    [Fact]
    public void TooShortSecret_IsRejected()
    {
        var options = ValidOptions() with { SecretKey = "short" };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT SecretKey must be at least 32 characters long for HMAC signing");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingIssuer_IsRejected(string issuer)
    {
        var options = ValidOptions() with { Issuer = issuer };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT Issuer is missing or empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingAudience_IsRejected(string audience)
    {
        var options = ValidOptions() with { Audience = audience };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT Audience is missing or empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAccessExpiration_IsRejected(int minutes)
    {
        var options = ValidOptions() with { AccessTokenExpirationMinutes = minutes };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT AccessTokenExpirationMinutes must be greater than zero");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidRefreshExpiration_IsRejected(int days)
    {
        var options = ValidOptions() with { RefreshTokenExpirationDays = days };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT RefreshTokenExpirationDays must be greater than zero");
    }

    [Fact]
    public void KnownDevelopmentSecret_InProduction_IsRejected()
    {
        var options = ValidOptions() with
        {
            SecretKey = "sapphire-dev-secret-key-change-me-in-production-32chars-min"
        };

        var act = () => JwtOptionsValidator.Validate(options, "Production");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT secret key uses a known development/default value in Production environment");
    }

    [Fact]
    public void UseRsaTrue_IsRejectedExplicitly()
    {
        var options = ValidOptions() with { UseRsa = true };
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<NotSupportedException>().WithMessage("RSA signing is not implemented yet. Use HMAC (UseRsa=false).");
    }

    [Fact]
    public void TokenService_RejectsUnsupportedRsaConfiguration()
    {
        var options = ValidOptions() with { UseRsa = true };
        var act = () => new TokenService(options);
        act.Should().Throw<NotSupportedException>().WithMessage("RSA signing is not implemented yet. Use HMAC (UseRsa=false).");
    }

    [Fact]
    public void ValidDevelopmentConfiguration_IsAccepted()
    {
        var act = () => JwtOptionsValidator.Validate(ValidOptions(), "Development");
        act.Should().NotThrow();
    }

    [Fact]
    public void HmacTokenGeneratedByAuthOptions_IsAcceptedBySharedValidation()
    {
        var options = ValidOptions();
        var token = new TokenService(options).GenerateAccessToken(Guid.NewGuid(), "user@test.local", ["User"], ["sessions.start"]);

        var principal = Validate(token, options, out _);

        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.Claims.Should().Contain(claim => claim.Type == "permission" && claim.Value == "sessions.start");
    }

    [Fact]
    public void AuthTokenGeneration_IsCompatibleWithBillingAndSessionValidationOptions()
    {
        var authOptions = ValidOptions();
        var billingOptions = ValidOptions();
        var sessionOptions = ValidOptions();
        var token = new TokenService(authOptions).GenerateAccessToken(Guid.NewGuid(), "user@test.local", ["Admin"], []);

        Validate(token, billingOptions, out _).Identity!.IsAuthenticated.Should().BeTrue();
        Validate(token, sessionOptions, out _).Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IssuerMismatch_IsRejected()
    {
        var token = BuildToken(ValidOptions() with { Issuer = "wrong-issuer" });

        var act = () => Validate(token, ValidOptions(), out _);

        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public void AudienceMismatch_IsRejected()
    {
        var token = BuildToken(ValidOptions() with { Audience = "wrong-audience" });

        var act = () => Validate(token, ValidOptions(), out _);

        act.Should().Throw<SecurityTokenInvalidAudienceException>();
    }

    [Fact]
    public void ExpiredToken_IsRejectedWithZeroClockSkew()
    {
        var token = BuildToken(ValidOptions(), expires: DateTime.UtcNow.AddSeconds(-1));

        var act = () => Validate(token, ValidOptions(), out _);

        act.Should().Throw<SecurityTokenExpiredException>();
    }

    [Fact]
    public void InvalidSigningKey_IsRejected()
    {
        var token = BuildToken(ValidOptions() with { SecretKey = OtherSecret });

        var act = () => Validate(token, ValidOptions(), out _);

        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    [Fact]
    public void SharedValidation_UsesZeroClockSkew()
    {
        var parameters = JwtAuthenticationExtensions.GetTokenValidationParameters(ValidOptions());
        parameters.ClockSkew.Should().Be(TimeSpan.Zero);

        var service = new TokenService(ValidOptions());
        service.GetValidationParameters().ClockSkew.Should().Be(TimeSpan.Zero);
    }

    private static ClaimsPrincipal Validate(string token, JwtOptions options, out SecurityToken validatedToken)
    {
        return new JwtSecurityTokenHandler().ValidateToken(
            token,
            JwtAuthenticationExtensions.GetTokenValidationParameters(options),
            out validatedToken);
    }

    private static string BuildToken(JwtOptions options, DateTime? expires = null)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "user@test.local")
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: expires ?? DateTime.UtcNow.AddMinutes(options.AccessTokenExpirationMinutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static JwtOptions ValidOptions() => new()
    {
        SecretKey = Secret,
        Issuer = Issuer,
        Audience = Audience,
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 30,
        UseRsa = false
    };
}
