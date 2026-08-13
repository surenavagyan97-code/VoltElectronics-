using FluentValidation;
using VoltElectronics.Domain.Catalog;

namespace VoltElectronics.Application.Admin.Products;

/// <summary>
/// Shape checks for the admin product form body, shared by create and update. Catalog-wide rules
/// that need the database (SKU uniqueness, category existence) stay in the handlers; string
/// lengths mirror the column limits in AppDbContext.
/// </summary>
internal sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Sku).NotEmpty().MaximumLength(50);
        RuleFor(r => r.CategoryId).GreaterThan(0);
        RuleFor(r => r.Price).GreaterThan(0);
        RuleFor(r => r.CompareAtPrice).GreaterThan(0).When(r => r.CompareAtPrice is not null);
        RuleFor(r => r.Stock).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Badge).MaximumLength(40);
        RuleFor(r => r.Status)
            .Must(s => Enum.TryParse<ProductStatus>(s, ignoreCase: true, out _))
            .WithMessage("Status must be Active, Draft or Archived.");
        RuleForEach(r => r.Specs).ChildRules(spec =>
        {
            spec.RuleFor(s => s.Name).MaximumLength(100);
            spec.RuleFor(s => s.Value).MaximumLength(300);
        });
    }
}

internal sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator() =>
        RuleFor(c => c.Product).SetValidator(new SaveProductRequestValidator());
}

internal sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator() =>
        RuleFor(c => c.Product).SetValidator(new SaveProductRequestValidator());
}

internal sealed class AddProductImageValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageValidator()
    {
        RuleFor(c => c.Url).NotEmpty().MaximumLength(500);
        RuleFor(c => c.ThumbUrl).NotEmpty().MaximumLength(500);
        RuleFor(c => c.CardUrl).NotEmpty().MaximumLength(500);
    }
}

internal sealed class ImportProductsValidator : AbstractValidator<ImportProductsCommand>
{
    private const int MaxRows = 10_000;

    public ImportProductsValidator() =>
        RuleFor(c => c.Rows)
            .NotNull()
            .Must(rows => rows.Count <= MaxRows)
            .WithMessage($"Import is limited to {MaxRows:N0} rows per file.");
}
