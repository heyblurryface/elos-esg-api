using System.ComponentModel.DataAnnotations;

namespace Kaizen.Api.Common.Pagination;

/// <summary>Parâmetros de paginação recebidos via query string (?page=1&amp;pageSize=10).</summary>
public class PageQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    private int _page = 1;

    [Range(1, int.MaxValue, ErrorMessage = "page deve ser >= 1.")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [Range(1, MaxPageSize, ErrorMessage = "pageSize deve estar entre 1 e 100.")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : (value > MaxPageSize ? MaxPageSize : value);
    }
}
