using BeselerNet.Shared.Contracts.App;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static System.Net.Mime.MediaTypeNames;

namespace BeselerNet.Api.Settings;

internal static class SettingEndpoints
{
    public static void MapSettingEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/v1/app", SettingHandlers.Status)
            .WithName("GetAppStatus")
            .WithTags("App")
            .Produces<AppStatusResponse>(Status200OK, Application.Json)
            .AllowAnonymous();

        var settings = builder.MapGroup("/v1/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        settings.MapGet("/", SettingHandlers.List)
            .WithName("ListSettings")
            .Produces<IReadOnlyList<AppSettingResponse>>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json);

        settings.MapPut("/{key}", SettingHandlers.Update)
            .WithName("UpdateSetting")
            .Accepts<UpdateSettingRequest>(Application.Json)
            .Produces<AppSettingResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);
    }
}
