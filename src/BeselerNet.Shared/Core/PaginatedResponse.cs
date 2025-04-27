namespace BeselerNet.Shared.Core;

public sealed record PaginatedResponse<T>(int Page, int PageSize, int TotalItems, IEnumerable<T> Items)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
