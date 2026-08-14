using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Content;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class ContentPageRepository(AppDbContext db) : IContentPageRepository
{
    public Task<ContentPage?> GetAsync(string key, string lang, CancellationToken cancellationToken = default) =>
        db.ContentPages.FirstOrDefaultAsync(p => p.Key == key && p.Lang == lang, cancellationToken);

    public void Add(ContentPage page) => db.ContentPages.Add(page);
}
