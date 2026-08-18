using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.App;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Settings;

internal static class SettingHandlers
{
    public static async Task<IResult> Status(RoleDataSource roles, SettingDataSource settings, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(new AppStatusResponse
        {
            SignupOpen = await AppStatus.SignupOpen(roles, settings, cancellationToken)
        });
    }

    public static async Task<IResult> List(ClaimsPrincipal user, SettingDataSource settings, CancellationToken cancellationToken)
    {
        if (Forbid(user, Actions.Read) is { } denied)
            return denied;

        var rows = await settings.List(cancellationToken);
        return TypedResults.Ok(rows.Select(Map).ToArray());
    }

    public static async Task<IResult> Update(string key, UpdateSettingRequest request, ClaimsPrincipal user, SettingDataSource settings, CancellationToken cancellationToken)
    {
        if (Forbid(user, Actions.Update) is { } denied)
            return denied;

        if (!Shared.Settings.Known.Contains(key, StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.Problem(new()
            {
                Title = "Unknown Setting",
                Detail = "That setting does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        int? updatedBy = int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId)
            ? accountId
            : null;

        var value = bool.Parse(request.Value).ToString().ToLowerInvariant();
        var setting = await settings.Set(key, value, updatedBy, cancellationToken);
        return TypedResults.Ok(Map(setting));
    }

    private static IResult? Forbid(ClaimsPrincipal user, string action) =>
        AccountProblems.Forbid(user, new SettingResource(), action);

    private static AppSettingResponse Map(AppSetting setting) => new()
    {
        Key = setting.Key,
        Value = setting.Value,
        UpdatedAt = setting.UpdatedAt,
        UpdatedByAccountId = setting.UpdatedByAccountId
    };
}
