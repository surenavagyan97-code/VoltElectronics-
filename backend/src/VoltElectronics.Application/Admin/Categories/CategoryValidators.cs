using FluentValidation;

namespace VoltElectronics.Application.Admin.Categories;

// Name lengths mirror the Categories column limits; the duplicate-name check needs the
// database and stays in the handlers.

internal sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator() =>
        RuleFor(c => c.Category.Name).NotEmpty().MaximumLength(100).WithName("Name");
}

internal sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator() =>
        RuleFor(c => c.Category.Name).NotEmpty().MaximumLength(100).WithName("Name");
}

internal sealed class SetCategoryImageValidator : AbstractValidator<SetCategoryImageCommand>
{
    public SetCategoryImageValidator() =>
        RuleFor(c => c.Url).NotEmpty().MaximumLength(500);
}
