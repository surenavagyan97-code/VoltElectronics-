using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Couriers;
using VoltElectronics.Application.Admin.Orders;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Application.Identity;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Ordering;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Identity;

internal sealed class CreateCourierHandler(
    UserManager<ApplicationUser> users) : ICommandHandler<CreateCourierCommand, Result<CourierDto>>
{
    public async Task<Result<CourierDto>> HandleAsync(
        CreateCourierCommand command, CancellationToken cancellationToken)
    {
        if (await users.FindByEmailAsync(command.Email) is not null)
            return Error.Invalid("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = true,
            FullName = command.FullName
        };
        var result = await users.CreateAsync(user, command.Password);
        if (!result.Succeeded)
            return Error.Invalid(string.Join(" ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(user, Roles.Courier);
        return new CourierDto(user.Id, command.Email, command.FullName, ActiveOrderCount: 0);
    }
}

internal sealed class DeleteCourierHandler(
    UserManager<ApplicationUser> users,
    AppDbContext db) : ICommandHandler<DeleteCourierCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteCourierCommand command, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(command.Id);
        // Only courier accounts may be deleted here — this endpoint must not reach admins or shoppers.
        if (user is null || !await users.IsInRoleAsync(user, Roles.Courier))
            return Error.Invalid("Courier not found.");

        // Their orders go back to the unassigned pool; TransactionBehavior keeps both steps atomic.
        var assigned = await db.Orders
            .Where(o => o.AssignedCourierId == command.Id)
            .ToListAsync(cancellationToken);
        foreach (var order in assigned)
            order.AssignCourier(null);

        var result = await users.DeleteAsync(user);
        if (!result.Succeeded)
            return Error.Invalid(string.Join(" ", result.Errors.Select(e => e.Description)));

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class AssignOrderCourierHandler(
    UserManager<ApplicationUser> users,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : ICommandHandler<AssignOrderCourierCommand, Result>
{
    public async Task<Result> HandleAsync(AssignOrderCourierCommand command, CancellationToken cancellationToken)
    {
        if (command.CourierId is not null)
        {
            var courier = await users.FindByIdAsync(command.CourierId);
            if (courier is null || !await users.IsInRoleAsync(courier, Roles.Courier))
                return Error.Invalid("Courier not found.");
        }

        var order = await orders.GetByOrderNumberAsync(command.OrderNumber, cancellationToken);
        if (order is null) return Error.Invalid("Order not found.");

        order.AssignCourier(command.CourierId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
