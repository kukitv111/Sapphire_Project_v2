using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sapphire.Auth.Domain.ValueObjects;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Infrastructure.Persistence.Converters;

/// <summary>
/// Value converters for Auth domain value objects.
/// Method names are prefixed with "To" to avoid collision with type names.
/// </summary>
public static class AuthValueConverters
{
    public static ValueConverter<Username, string> UsernameConverter() =>
        new(v => v.Value, s => Username.From(s));

    public static ValueConverter<Email, string> EmailConverter() =>
        new(v => v.Value, s => Email.From(s));

    public static ValueConverter<PhoneNumber?, string?> PhoneConverter() =>
        new(v => v == null ? null : v.Value, s => string.IsNullOrEmpty(s) ? null : PhoneNumber.From(s));
}
