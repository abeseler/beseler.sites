using System.Text.Json.Serialization;

namespace Beseler.Deploy.Deployments;

public sealed record class WebhookRequest
{
    [JsonPropertyName("callback_url")]
    public required string CallbackUrl { get; init; }
    [JsonPropertyName("push_data")]
    public required PushData PushData { get; init; }
    public required Repository Repository { get; init; }
}
public sealed record class PushData
{
    [JsonPropertyName("pushed_at")]
    public long PushedAt { get; init; }
    public required string Pusher { get; init; }
    public required string Tag { get; init; }
}
public sealed record class Repository
{
    [JsonPropertyName("comment_count")]
    public int CommentCount { get; init; }
    [JsonPropertyName("date_created")]
    public long DateCreated { get; init; }
    public required string Description { get; init; }
    public required string Dockerfile { get; init; }
    [JsonPropertyName("full_description")]
    public required string FullDescription { get; init; }
    [JsonPropertyName("is_official")]
    public bool IsOfficial { get; init; }
    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; init; }
    [JsonPropertyName("is_trusted")]
    public bool IsTrusted { get; init; }
    public required string Name { get; init; }
    public required string Namespace { get; init; }
    public required string Owner { get; init; }
    [JsonPropertyName("repo_name")]
    public required string RepoName { get; init; }
    [JsonPropertyName("repo_url")]
    public required string RepoUrl { get; init; }
    [JsonPropertyName("star_count")]
    public int StarCount { get; init; }
    public required string Status { get; init; }
}
