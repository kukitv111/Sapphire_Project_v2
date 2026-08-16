using FluentValidation;
using Sapphire.Auth.Application.Commands.Register;
using Sapphire.Auth.Domain.ValueObjects;

namespace Sapphire.Auth.Application.Commands.Register;

/// <summary>
/// Validator for RegisterCommand.
/// </summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(Username.MinLength).WithMessage($"Username must be at least {Username.MinLength} characters")
            .MaximumLength(Username.MaxLength).WithMessage($"Username cannot exceed {Username.MaxLength} characters")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores and hyphens");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(Password.MinLength).WithMessage($"Password must be at least {Password.MinLength} characters")
            .MaximumLength(Password.MaxLength).WithMessage($"Password cannot exceed {Password.MaxLength} characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");

        RuleFor(x => x.Phone)
            .Matches("^\\+?[0-9]{10,15}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number must be in E.164 format (e.g., +79012345678)");
    }
}
