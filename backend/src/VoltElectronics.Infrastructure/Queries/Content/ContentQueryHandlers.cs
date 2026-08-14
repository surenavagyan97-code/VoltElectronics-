using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Content;
using VoltElectronics.Application.Content.Queries;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Queries.Content;

internal sealed class GetContentPageHandler(AppDbContext db)
    : IQueryHandler<GetContentPageQuery, ContentPageDto?>
{
    public async Task<ContentPageDto?> HandleAsync(GetContentPageQuery query, CancellationToken cancellationToken)
    {
        var key = query.Key.Trim().ToLowerInvariant();
        return await db.ContentPages.AsNoTracking()
            .Where(p => p.Key == key)
            .Select(p => new ContentPageDto(p.Key, p.Body, p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
