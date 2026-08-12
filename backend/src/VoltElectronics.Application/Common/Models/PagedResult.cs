namespace VoltElectronics.Application.Common.Models;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>Clamps caller-supplied paging so a query handler never has to repeat the guard.</summary>
public static class Paging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, maxPageSize));
}
