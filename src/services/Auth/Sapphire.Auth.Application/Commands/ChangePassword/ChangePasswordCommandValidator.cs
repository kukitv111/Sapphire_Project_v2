using FluentValidation;
using Sapphire.Auth.Domain.ValueObjects;

namespace Sapphire.Auth.Application.Commands.ChangePassword;

/// <summary>
/// FluentValidation validator for ChangePasswordCommand.
/// </summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(Password.MinLength).WithMessage($"Password must be at least {Password.MinLength} characters")
            .MaximumLength(Password.MaxLength).WithMessage($"Password cannot exceed {Password.MaxLength} characters")
            .Must(HasUpperCase).WithMessage("Password must contain at least one uppercase letter")
            .Must(HasLowerCase).WithMessage("Password must contain at least one lowercase letter")
            .Must(HasDigit).WithMessage("Password must contain at least one digit")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");
    }

    private static bool HasUpperCase(string password) => password.Any(char.IsUpper);
    private static bool HasLowerCase(string password) => password.Any(char.IsLower);
    private static bool HasDigit(string password) => password.Any(char.IsDigit);
}
