using BeselerNet.Api.Registrars;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BeselerNet.Api.OpenApi;

internal sealed class AuthenticationSchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        var requirements = new Dictionary<string, IOpenApiSecurityScheme>();

        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {

            requirements["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                Name = "Authorization",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token"
            };

        }

        if (authenticationSchemes.Any(authScheme => authScheme.Name == "ApiKey"))
        {
            requirements["ApiKey"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Scheme = ApiKeyAuthOptions.Scheme,
                Name = ApiKeyAuthOptions.HeaderName,
                In = ParameterLocation.Header
            };
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = requirements;
    }
}
