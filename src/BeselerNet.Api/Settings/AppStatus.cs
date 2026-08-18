using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;

namespace BeselerNet.Api.Settings;

internal static class AppStatus
{
    public static async Task<bool> SignupOpen(RoleDataSource roles, SettingDataSource settings, CancellationToken cancellationToken)
    {
        if (!await roles.IsAssignedToAnyone(Roles.Admin, cancellationToken))
            return true;

        return await settings.IsEnabled(Shared.Settings.PublicSignup, cancellationToken);
    }
}
