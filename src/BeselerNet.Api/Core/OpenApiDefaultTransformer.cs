using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace BeselerNet.Api.Core;

internal sealed record OpenApiOptions
{
    public const string SectionName = "OpenApi";
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ContactUrl { get; init; }
    public string? ServerUrl { get; init; }
}

internal sealed class OpenApiDefaultTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var options = context.ApplicationServices.GetRequiredService<IOptions<OpenApiOptions>>().Value;

        document.Info.Title = options.Title ?? document.Info.Title;
        document.Info.Description = options.Description ?? document.Info.Description;

        if (options.ContactUrl is not null)
        {
            document.Info.Contact = new OpenApiContact()
            {
                Url = new Uri(options.ContactUrl)
            };
        }

        if (!string.IsNullOrWhiteSpace(options.ServerUrl))
        {
            document.Servers.Clear();
            document.Servers.Add(new OpenApiServer { Url = options.ServerUrl });
        }

        return Task.CompletedTask;
    }
}
