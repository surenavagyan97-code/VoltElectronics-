using FluentValidation;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Content;

namespace VoltElectronics.Application.Admin.Content;

/// <summary>Upserts an editable page's body — the first save of a new key creates it.</summary>
public sealed record UpdateContentPageCommand(string Key, string Body) : ICommand<Result>;

internal sealed class UpdateContentPageHandler(
    IContentPageRepository pages,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateContentPageCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateContentPageCommand command, CancellationToken cancellationToken)
    {
        var key = command.Key.Trim().ToLowerInvariant();
        var page = await pages.GetByKeyAsync(key, cancellationToken);

        if (page is null) pages.Add(ContentPage.Create(key, command.Body));
        else page.Edit(command.Body);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class UpdateContentPageValidator : AbstractValidator<UpdateContentPageCommand>
{
    public UpdateContentPageValidator()
    {
        RuleFor(c => c.Key).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Body).NotEmpty().MaximumLength(200_000);
    }
}
