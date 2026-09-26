namespace FluxusManager.Domain.Common;

/// <summary>Página de uma listagem.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;

    public PagedResult<TResult> Map<TResult>(Func<T, TResult> map)
        => new(Items.Select(map).ToList(), Page, PageSize, TotalCount);
}
