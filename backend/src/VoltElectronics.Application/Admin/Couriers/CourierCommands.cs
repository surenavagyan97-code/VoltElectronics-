using FluentValidation;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;

namespace VoltElectronics.Application.Admin.Couriers;

// Handlers live in Infrastructure/Identity — courier accounts are ASP.NET Identity users,
// so creating and deleting them needs UserManager.

public sealed record CreateCourierCommand(string Email, string Password, string FullName)
    : ICommand<Result<CourierDto>>;

internal sealed class CreateCourierValidator : AbstractValidator<CreateCourierCommand>
{
    public CreateCourierValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(150);
    }
}

public sealed record DeleteCourierCommand(string Id) : ICommand<Result>;

internal sealed class DeleteCourierValidator : AbstractValidator<DeleteCourierCommand>
{
    public DeleteCourierValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
