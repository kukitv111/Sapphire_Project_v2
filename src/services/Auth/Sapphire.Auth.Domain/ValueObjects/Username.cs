using System.Text.RegularExpressions;
using Sapphire.Shared.Kernel.ValueObjects;

namespace Sapphire.Auth.Domain.ValueObjects;

/// <summary>
/// Username value object with validation rules.
/// Usernames must be alphanumeric with optional underscores and hyphens.
/// </summary>
public sealed record Username : ValueObject
{
    private static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9_-]+$",
        RegexOptions.Compiled);

    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Minimum username length.
    /// </summary>
    public const int MinLength = 3;

    /// <summary>
    /// Maximum username length.
    /// </summary>
    public const int MaxLength = 32;

    /// <summary>
    /// Creates a Username from a string value.
    /// </summary>
    /// <param name="value">The username string.</param>
    /// <returns>A validated Username instance.</returns>
    /// <exception cref="ArgumentException">Thrown when username is invalid.</exception>
    public static Username From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Username cannot be empty", nameof(value));

        var normalized = value.Trim();

        if (normalized.Length < MinLength)
            throw new ArgumentException($"Username must be at least {MinLength} characters", nameof(value));

        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Username cannot exceed {MaxLength} characters", nameof(value));

        if (!UsernameRegex.IsMatch(normalized))
            throw new ArgumentException("Username can only contain letters, numbers, underscores and hyphens", nameof(value));

        return new Username(normalized.ToLowerInvariant());
    }

    /// <summary>
    /// Tries to create a Username without throwing.
    /// </summary>
    public static Username? TryFrom(string value)
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

    public static implicit operator string(Username username) => username.Value;
}
