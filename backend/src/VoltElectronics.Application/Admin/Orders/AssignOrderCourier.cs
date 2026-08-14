using FluentValidation;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;

namespace VoltElectronics.Application.Admin.Orders;

/// <summary>
/// Puts an order on a delivery person's plate (or takes it off, with a null courier id).
/// Handled in Infrastructure/Identity — the courier must be verified to hold the Courier role.
/// </summary>
public sealed record AssignOrderCourierCommand(string OrderNumber, string? CourierId) : ICommand<Result>;

internal sealed class AssignOrderCourierValidator : AbstractValidator<AssignOrderCourierCommand>
{
    public AssignOrderCourierValidator()
    {
        RuleFor(c => c.OrderNumber).NotEmpty();
    }
}
