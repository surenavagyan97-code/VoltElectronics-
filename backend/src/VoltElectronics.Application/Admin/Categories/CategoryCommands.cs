using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Admin.Categories;

public sealed record CreateCategoryCommand(SaveCategoryRequest Category) : ICommand<Result<CategoryDto>>;

internal sealed class CreateCategoryHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        // Create trims the name and rejects an empty one; the duplicate check then runs against the
        // trimmed form, so " Audio " can't slip past an existing "Audio".
        var category = Category.Create(command.Category.Name);

        if (await categories.NameExistsAsync(category.Name, cancellationToken: cancellationToken))
            return Error.Invalid("A category with this name already exists.");

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.Slug, 0);
    }
}

public sealed record UpdateCategoryCommand(int Id, SaveCategoryRequest Category) : ICommand<Result>;

internal sealed class UpdateCategoryHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null) return Error.Invalid("Category not found.");

        // Renaming before the duplicate check keeps the name rules in one place. Bailing out below
        // leaves the rename un-saved, and the tracked change dies with the request scope.
        category.Rename(command.Category.Name);

        if (await categories.NameExistsAsync(category.Name, command.Id, cancellationToken))
            return Error.Invalid("A category with this name already exists.");

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteCategoryCommand(int Id) : ICommand<Result>;

internal sealed class DeleteCategoryHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null) return Error.Invalid("Category not found.");

        if (await categories.HasProductsAsync(command.Id, cancellationToken))
            return Error.Invalid("Category has products — move or delete them first.");

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
