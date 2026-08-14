using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Content.Queries;

/// <summary>An editable storefront page by key ("privacy", …). Null when the key is unknown.</summary>
public sealed record GetContentPageQuery(string Key) : IQuery<ContentPageDto?>;
