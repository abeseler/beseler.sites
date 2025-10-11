using BeselerNet.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BeselerNet.Api.Communications;

internal static class AzureEmailEventsWebhook
{
    private static readonly GenericMessageResponse s_okResponse = new() { Message = "Events submitted for processing." };
    public static IResult Handle(string? apikey, IOptions<AzureOptions> options)
    {
        var validApiKey = options.Value.WebhookApiKey;
        if (validApiKey is { Length: > 0 } && (apikey is null || apikey != validApiKey))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(s_okResponse);
    }

    public static async Task<IResult> ValidationHandshake(
        [FromHeader(Name = "WebHook-Request-Origin")] string? origin,
        [FromHeader(Name = "WebHook-Request-Callback")] string? callback,
        [FromHeader(Name = "WebHook-Request-Rate")] string? rate,
        HttpResponse response,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(callback))
        {
            using var httpClient = new HttpClient();
            using var httpResponse = await httpClient.PostAsync(callback, null, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return TypedResults.Problem($"Callback to {callback} failed with status code {httpResponse.StatusCode}");
            }
        }

        var allowedRate = rate is not null && int.TryParse(rate, out var r) && r <= 100 ? r : 100;

        response.Headers.Allow = "POST, OPTIONS";
        response.Headers.Append("WebHook-Allowed-Origin", origin ?? "*");
        response.Headers.Append("WebHook-Allowed-Rate", allowedRate.ToString());

        return TypedResults.Ok();
    }
}
