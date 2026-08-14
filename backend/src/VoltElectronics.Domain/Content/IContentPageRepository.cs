namespace VoltElectronics.Domain.Content;

public interface IContentPageRepository
{
    Task<ContentPage?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    void Add(ContentPage page);
}
