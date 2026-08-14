using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Content;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class ContentPageRepository(AppDbContext db) : IContentPageRepository
{
    public Task<ContentPage?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        db.ContentPages.FirstOrDefaultAsync(p => p.Key == key, cancellationToken);

    public void Add(ContentPage page) => db.ContentPages.Add(page);
}
