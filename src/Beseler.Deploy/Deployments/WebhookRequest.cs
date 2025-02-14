using System.Text.Json.Serialization;

namespace Beseler.Deploy.Deployments;

public sealed record class WebhookRequest
{
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; init; }
    [JsonPropertyName("push_data")]
    public PushData? PushData { get; init; }
    [JsonPropertyName("repository")]
    public Repository? Repository { get; init; }
}
public sealed record class PushData
{
    [JsonPropertyName("pusher")]
    public string? Pusher { get; init; }
    [JsonPropertyName("pushed_at")]
    public long PushedAt { get; init; }
    [JsonPropertyName("tag")]
    public string? Tag { get; init; }
    [JsonPropertyName("images")]
    public List<string>? Images { get; init; }
    [JsonPropertyName("media_type")]
    public string? MediaType { get; init; }
}
public sealed record class Repository
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }
    [JsonPropertyName("namespace")]
    public string? Namespace { get; init; }
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("repo_name")]
    public string? RepoName { get; init; }
    [JsonPropertyName("repo_url")]
    public string? RepoUrl { get; init; }
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    [JsonPropertyName("full_description")]
    public string? FullDescription { get; init; }
    [JsonPropertyName("star_count")]
    public int StarCount { get; init; }
    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; init; }
    [JsonPropertyName("is_trusted")]
    public bool IsTrusted { get; init; }
    [JsonPropertyName("is_official")]
    public bool IsOfficial { get; init; }
    [JsonPropertyName("owner")]
    public string? Owner { get; init; }
    [JsonPropertyName("date_created")]
    public long DateCreated { get; init; }


}
