using System.Text.RegularExpressions;

namespace Sapphire.Shared.Kernel.ValueObjects;

/// <summary>
/// Email value object with validation.
/// </summary>
public sealed record Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Creates an Email from a string value.
    /// Throws ArgumentException if email is invalid.
    /// </summary>
    public static Email From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        var email = value.Trim();

        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException("Invalid email format", nameof(value));

        return new Email(email);
    }

    /// <summary>
    /// Tries to create an Email without throwing.
    /// Returns null if invalid.
    /// </summary>
    public static Email? TryFrom(string value)
    {
        try
        {
            return From(value);
        }
        catch
        {
            return null;
        }
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
