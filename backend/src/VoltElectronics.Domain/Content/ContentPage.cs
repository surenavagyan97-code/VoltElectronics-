using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Content;

/// <summary>
/// An admin-editable page of storefront copy (privacy policy, terms, …), addressed by a stable
/// key. The body is plain text; the storefront renders it preserving line breaks.
/// </summary>
public sealed class ContentPage : AggregateRoot
{
    private ContentPage() { }

    public int Id { get; private set; }
    public string Key { get; private set; } = null!;
    public string Body { get; private set; } = "";
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static ContentPage Create(string key, string body)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new DomainException("Key is required.");
        var page = new ContentPage { Key = key.Trim().ToLowerInvariant() };
        page.Edit(body);
        return page;
    }

    public void Edit(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new DomainException("Content is required.");
        Body = body.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
