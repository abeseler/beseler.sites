namespace BeselerNet.Shared.Core;

public sealed record CollectionResponse<T>
{
    public required T[] Items { get; set; }
}

public sealed record PaginatedCollectionResponse<T>
{
    public int Page { get; set; }
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }
    public required T[] Items { get; set; }
}
