namespace Sapphire.Shared.Kernel.Common;

/// <summary>
/// Represents an application error with code and description.
/// </summary>
public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public static Error Create(string code, string description, ErrorType type = ErrorType.Validation) => new(code, description, type);

    public static Error NotFound(string description) => new("not_found", description, ErrorType.NotFound);
    public static Error Conflict(string description) => new("conflict", description, ErrorType.Conflict);
    public static Error Unauthorized(string description) => new("unauthorized", description, ErrorType.Unauthorized);
    public static Error Forbidden(string description) => new("forbidden", description, ErrorType.Forbidden);
    public static Error Validation(string description) => new("validation", description, ErrorType.Validation);
    public static Error Internal(string description) => new("internal", description, ErrorType.Internal);

    public static implicit operator string(Error error) => error.Code;
}

/// <summary>
/// Error type classification.
/// </summary>
public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Internal
}
