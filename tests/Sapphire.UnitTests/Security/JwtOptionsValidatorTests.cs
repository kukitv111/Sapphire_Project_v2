using FluentAssertions;
using Sapphire.Shared.Security.Jwt;
using Xunit;

namespace Sapphire.UnitTests.Security;

public class JwtOptionsValidatorTests
{
    [Fact]
    public void Validate_WithNullOptions_ThrowsInvalidOperationException()
    {
        var act = () => JwtOptionsValidator.Validate(null, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT configuration is missing");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptySecretKey_ThrowsInvalidOperationException(string secret)
    {
        var options = CreateValidOptions();
        options.SecretKey = secret!;
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT SecretKey is missing or empty");
    }

    [Fact]
    public void Validate_WithShortSecretKey_ThrowsInvalidOperationException()
    {
        var options = CreateValidOptions();
        options.SecretKey = "short";
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT SecretKey must be at least 32 characters long for HMAC signing");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyIssuer_ThrowsInvalidOperationException(string issuer)
    {
        var options = CreateValidOptions();
        options.Issuer = issuer!;
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT Issuer is missing or empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyAudience_ThrowsInvalidOperationException(string audience)
    {
        var options = CreateValidOptions();
        options.Audience = audience!;
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT Audience is missing or empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidAccessTokenExpiration_ThrowsInvalidOperationException(int minutes)
    {
        var options = CreateValidOptions();
        options.AccessTokenExpirationMinutes = minutes;
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT AccessTokenExpirationMinutes must be greater than zero");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidRefreshTokenExpiration_ThrowsInvalidOperationException(int days)
    {
        var options = CreateValidOptions();
        options.RefreshTokenExpirationDays = days;
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT RefreshTokenExpirationDays must be greater than zero");
    }

    [Fact]
    public void Validate_WithUseRsaTrue_ThrowsNotSupportedException()
    {
        var options = CreateValidOptions();
        options.UseRsa = true;
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().Throw<NotSupportedException>().WithMessage("RSA signing is not implemented yet. Use HMAC (UseRsa=false).");
    }

    [Fact]
    public void Validate_WithKnownDevSecretInProduction_ThrowsInvalidOperationException()
    {
        var options = CreateValidOptions();
        options.SecretKey = "sapphire-dev-secret-key-change-me-in-production-32chars-min";
        var act = () => JwtOptionsValidator.Validate(options, "Production");
        act.Should().Throw<InvalidOperationException>().WithMessage("JWT secret key uses a known development/default value in Production environment");
    }

    [Fact]
    public void Validate_WithValidOptions_DoesNotThrow()
    {
        var options = CreateValidOptions();
        var act = () => JwtOptionsValidator.Validate(options, "Development");
        act.Should().NotThrow();
    }

    private static JwtOptions CreateValidOptions() => new()
    {
        SecretKey = "super-secret-key-must-be-at-least-32-chars!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 30,
        UseRsa = false
    };
}
