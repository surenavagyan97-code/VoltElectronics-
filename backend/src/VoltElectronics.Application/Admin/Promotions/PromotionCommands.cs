using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Application.Promotions;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Promotions;

namespace VoltElectronics.Application.Admin.Promotions;

public sealed record CreatePromotionCommand(SavePromotionRequest Promotion) : ICommand<Result<PromotionDto>>;

internal sealed class CreatePromotionHandler(
    IPromotionRepository promotions, IUnitOfWork unitOfWork) : ICommandHandler<CreatePromotionCommand, Result<PromotionDto>>
{
    public async Task<Result<PromotionDto>> HandleAsync(CreatePromotionCommand command, CancellationToken cancellationToken)
    {
        var r = command.Promotion;

        if (!Enum.TryParse<PromotionType>(r.Type, ignoreCase: true, out var type))
            return Error.Invalid("Invalid promotion type.");
        if (!Enum.TryParse<PromotionScope>(r.Scope, ignoreCase: true, out var scope))
            return Error.Invalid("Invalid promotion scope.");

        if (!string.IsNullOrWhiteSpace(r.Code) &&
            await promotions.CodeExistsAsync(Promotion.Normalize(r.Code), cancellationToken: cancellationToken))
            return Error.Invalid("A promotion with this code already exists.");

        Promotion promotion;
        try
        {
            promotion = Promotion.Create(
                r.Code, r.Name, type, r.Value, scope, r.CategoryId, r.ProductIds,
                r.MinSubtotal, r.MaxDiscountAmount, r.MaxRedemptions, r.StartsAt, r.ExpiresAt);
        }
        catch (DomainException ex) { return Error.Invalid(ex.Message); }

        promotions.Add(promotion);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return PromotionMapping.ToDto(promotion, categoryName: null);
    }
}

public sealed record UpdatePromotionCommand(int Id, SavePromotionRequest Promotion) : ICommand<Result>;

internal sealed class UpdatePromotionHandler(
    IPromotionRepository promotions, IUnitOfWork unitOfWork) : ICommandHandler<UpdatePromotionCommand, Result>
{
    public async Task<Result> HandleAsync(UpdatePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(command.Id, cancellationToken);
        if (promotion is null) return Error.Invalid("Promotion not found.");

        var r = command.Promotion;
        if (!Enum.TryParse<PromotionType>(r.Type, ignoreCase: true, out var type))
            return Error.Invalid("Invalid promotion type.");
        if (!Enum.TryParse<PromotionScope>(r.Scope, ignoreCase: true, out var scope))
            return Error.Invalid("Invalid promotion scope.");

        var normalizedCode = string.IsNullOrWhiteSpace(r.Code) ? null : Promotion.Normalize(r.Code);
        if (normalizedCode is not null &&
            await promotions.CodeExistsAsync(normalizedCode, command.Id, cancellationToken))
            return Error.Invalid("A promotion with this code already exists.");

        try
        {
            promotion.Update(
                r.Code, r.Name, type, r.Value, scope, r.CategoryId, r.ProductIds,
                r.MinSubtotal, r.MaxDiscountAmount, r.MaxRedemptions, r.StartsAt, r.ExpiresAt, r.IsActive);
        }
        catch (DomainException ex) { return Error.Invalid(ex.Message); }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeletePromotionCommand(int Id) : ICommand<Result>;

internal sealed class DeletePromotionHandler(
    IPromotionRepository promotions, IUnitOfWork unitOfWork) : ICommandHandler<DeletePromotionCommand, Result>
{
    public async Task<Result> HandleAsync(DeletePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(command.Id, cancellationToken);
        if (promotion is null) return Error.Invalid("Promotion not found.");
        promotions.Remove(promotion);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class PromotionMapping
{
    public static PromotionDto ToDto(Promotion p, string? categoryName) => new(
        p.Id, p.Code, p.Name, p.Type.ToString(), p.Value, p.Scope.ToString(),
        p.CategoryId, categoryName, p.Products.Select(x => x.ProductId).ToList(),
        p.MinSubtotal, p.MaxDiscountAmount, p.MaxRedemptions, p.RedemptionCount,
        p.StartsAt, p.ExpiresAt, p.IsActive, p.CreatedAt);
}
