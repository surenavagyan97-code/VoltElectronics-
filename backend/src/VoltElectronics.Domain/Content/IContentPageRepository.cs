namespace VoltElectronics.Domain.Content;

public interface IContentPageRepository
{
    Task<ContentPage?> GetAsync(string key, string lang, CancellationToken cancellationToken = default);
    void Add(ContentPage page);
}
