using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Content;

/// <summary>
/// An admin-editable page of storefront copy (privacy policy, about, FAQ, …): one row per
/// key + language, so each page can be translated independently. The body is plain text;
/// the storefront renders it preserving line breaks. Readers fall back to English when a
/// translation hasn't been written yet.
/// </summary>
public sealed class ContentPage : AggregateRoot
{
    public const string DefaultLang = "en";

    private ContentPage() { }

    public int Id { get; private set; }
    public string Key { get; private set; } = null!;
    public string Lang { get; private set; } = DefaultLang;
    public string Body { get; private set; } = "";
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static ContentPage Create(string key, string lang, string body)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new DomainException("Key is required.");
        if (string.IsNullOrWhiteSpace(lang)) throw new DomainException("Language is required.");
        var page = new ContentPage
        {
            Key = key.Trim().ToLowerInvariant(),
            Lang = lang.Trim().ToLowerInvariant(),
        };
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
