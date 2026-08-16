using System.Text.RegularExpressions;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Domain.ValueObjects;

/// <summary>
/// Phone number value object with international format support.
/// Stores phone numbers in E.164 format (e.g., +79001234567).
/// </summary>
public sealed record PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+[1-9]\d{6,14}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a PhoneNumber from a string value.
    /// Accepts E.164 format (+CountryCodeNumber).
    /// </summary>
    /// <param name="value">The phone number string.</param>
    /// <returns>A validated PhoneNumber instance.</returns>
    /// <exception cref="ArgumentException">Thrown when phone number is invalid.</exception>
    public static PhoneNumber From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty", nameof(value));

        var normalized = value.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        // Add + prefix if missing
        if (!normalized.StartsWith("+"))
            normalized = "+" + normalized;

        if (!PhoneRegex.IsMatch(normalized))
            throw new ArgumentException("Invalid phone number format. Use E.164 format (e.g., +79001234567)", nameof(value));

        return new PhoneNumber(normalized);
    }

    /// <summary>
    /// Tries to create a PhoneNumber without throwing.
    /// </summary>
    public static PhoneNumber? TryFrom(string value)
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

    /// <summary>
    /// Checks if the phone number is valid without creating an instance.
    /// </summary>
    public static bool IsValid(string value)
    {
        return TryFrom(value) != null;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}
