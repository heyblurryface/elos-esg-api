using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Common.Pagination;

/// <summary>Envelope padrão de resposta paginada.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static async Task<PagedResult<T>> CreateAsync(
        IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        // Otimização: só conta uma vez e aplica Skip/Take no banco (não materializa tudo).
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) => new()
    {
        Items = Items.Select(selector).ToList(),
        Page = Page,
        PageSize = PageSize,
        TotalItems = TotalItems
    };
}
