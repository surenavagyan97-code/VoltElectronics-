using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Content;
using VoltElectronics.Application.Content.Queries;
using VoltElectronics.Domain.Content;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Queries.Content;

internal sealed class GetContentPageHandler(AppDbContext db)
    : IQueryHandler<GetContentPageQuery, ContentPageDto?>
{
    public async Task<ContentPageDto?> HandleAsync(GetContentPageQuery query, CancellationToken cancellationToken)
    {
        var key = query.Key.Trim().ToLowerInvariant();
        var lang = query.Lang.Trim().ToLowerInvariant();

        var page = await Find(key, lang, cancellationToken);
        if (page is null && query.Fallback && lang != ContentPage.DefaultLang)
            page = await Find(key, ContentPage.DefaultLang, cancellationToken);
        return page;
    }

    private Task<ContentPageDto?> Find(string key, string lang, CancellationToken ct) =>
        db.ContentPages.AsNoTracking()
            .Where(p => p.Key == key && p.Lang == lang)
            .Select(p => new ContentPageDto(p.Key, p.Lang, p.Body, p.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
