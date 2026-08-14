using FluentValidation;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Content;

namespace VoltElectronics.Application.Admin.Content;

/// <summary>Upserts one language of an editable page — the first save of a key+language creates it.</summary>
public sealed record UpdateContentPageCommand(string Key, string Lang, string Body) : ICommand<Result>;

internal sealed class UpdateContentPageHandler(
    IContentPageRepository pages,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateContentPageCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateContentPageCommand command, CancellationToken cancellationToken)
    {
        var key = command.Key.Trim().ToLowerInvariant();
        var lang = command.Lang.Trim().ToLowerInvariant();
        var page = await pages.GetAsync(key, lang, cancellationToken);

        if (page is null) pages.Add(ContentPage.Create(key, lang, command.Body));
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
        RuleFor(c => c.Lang).NotEmpty()
            .Matches("^[a-z]{2}(-[a-z]{2,4})?$")
            .WithMessage("Language must be a lowercase code like en, hy or ru.");
        RuleFor(c => c.Body).NotEmpty().MaximumLength(200_000);
    }
}
