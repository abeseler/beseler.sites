using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;

namespace BeselerNet.Api.Settings;

internal sealed class SettingResource : IAuthorizableResource
{
    public static string ResourceName => Resources.Setting;
}
