using FluentValidation;
using Sapphire.Auth.Application.Commands.Login;
using Sapphire.Auth.Domain.ValueObjects;

namespace Sapphire.Auth.Application.Commands.Login;

/// <summary>
/// Validator for LoginCommand.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Login is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(Password.MinLength).WithMessage($"Password must be at least {Password.MinLength} characters");
    }
}
