using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Domain.Content;

namespace VoltElectronics.Application.Content.Queries;

/// <summary>
/// An editable storefront page by key ("privacy", …) and language. With <paramref name="Fallback"/>
/// (the storefront's mode) a missing translation resolves to the default language; without it
/// (the admin editor's mode) it returns null so the editor shows an empty draft.
/// </summary>
public sealed record GetContentPageQuery(
    string Key,
    string Lang = ContentPage.DefaultLang,
    bool Fallback = true) : IQuery<ContentPageDto?>;
