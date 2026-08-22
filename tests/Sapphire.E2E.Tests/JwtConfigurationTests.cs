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
        var options = ValidOptions(secretKey: secret);
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT SecretKey is missing or empty");
    }

    [Fact]
    public void TooShortSecret_IsRejected()
    {
        var options = ValidOptions(secretKey: "short");
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT SecretKey must be at least 32 characters long for HMAC signing");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingIssuer_IsRejected(string issuer)
    {
        var options = ValidOptions(issuer: issuer);
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT Issuer is missing or empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingAudience_IsRejected(string audience)
    {
        var options = ValidOptions(audience: audience);
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT Audience is missing or empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAccessExpiration_IsRejected(int minutes)
    {
        var options = ValidOptions(accessTokenExpirationMinutes: minutes);
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT AccessTokenExpirationMinutes must be greater than zero");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidRefreshExpiration_IsRejected(int days)
    {
        var options = ValidOptions(refreshTokenExpirationDays: days);
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT RefreshTokenExpirationDays must be greater than zero");
    }

    [Fact]
    public void KnownDevelopmentSecret_InProduction_IsRejected()
    {
        var options = ValidOptions(secretKey: "sapphire-dev-secret-key-change-me-in-production-32chars-min");

        var act = () => JwtOptionsValidator.Validate(options, "Production");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT secret key uses a known development/default value in Production environment");
    }

    [Fact]
    public void TokenService_UsesActualProductionEnvironmentForValidation()
    {
        var options = ValidOptions(secretKey: "sapphire-dev-secret-key-change-me-in-production-32chars-min");

        var act = () => new TokenService(options, "Production");

        act.Should().Throw<InvalidOperationException>().WithMessage("JWT secret key uses a known development/default value in Production environment");
    }

    [Fact]
    public void UseRsaTrue_IsRejectedExplicitly()
    {
        var options = ValidOptions(useRsa: true);
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<NotSupportedException>().WithMessage("RSA signing is not implemented yet. Use HMAC (UseRsa=false).");
    }

    [Fact]
    public void TokenService_RejectsUnsupportedRsaConfiguration()
    {
        var options = ValidOptions(useRsa: true);
        var act = () => new TokenService(options, "Development");
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
        var token = new TokenService(options, "Development").GenerateAccessToken(Guid.NewGuid(), "user@test.local", ["User"], ["sessions.start"]);

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
        var token = new TokenService(authOptions, "Development").GenerateAccessToken(Guid.NewGuid(), "user@test.local", ["Admin"], []);

        Validate(token, billingOptions, out _).Identity!.IsAuthenticated.Should().BeTrue();
        Validate(token, sessionOptions, out _).Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IssuerMismatch_IsRejected()
    {
        var token = BuildToken(ValidOptions(issuer: "wrong-issuer"));

        var act = () => Validate(token, ValidOptions(), out _);

        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public void AudienceMismatch_IsRejected()
    {
        var token = BuildToken(ValidOptions(audience: "wrong-audience"));

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
        var token = BuildToken(ValidOptions(secretKey: OtherSecret));

        var act = () => Validate(token, ValidOptions(), out _);

        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    [Fact]
    public void SharedValidation_UsesZeroClockSkew()
    {
        var parameters = JwtAuthenticationExtensions.GetTokenValidationParameters(ValidOptions());
        parameters.ClockSkew.Should().Be(TimeSpan.Zero);

        var service = new TokenService(ValidOptions(), "Development");
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

    private static JwtOptions ValidOptions(
        string secretKey = Secret,
        string issuer = Issuer,
        string audience = Audience,
        int accessTokenExpirationMinutes = 15,
        int refreshTokenExpirationDays = 30,
        bool useRsa = false) => new()
    {
        SecretKey = secretKey,
        Issuer = issuer,
        Audience = audience,
        AccessTokenExpirationMinutes = accessTokenExpirationMinutes,
        RefreshTokenExpirationDays = refreshTokenExpirationDays,
        UseRsa = useRsa
    };
}

