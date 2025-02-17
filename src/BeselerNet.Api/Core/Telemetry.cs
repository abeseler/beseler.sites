using System.Diagnostics;

namespace BeselerNet.Api.Core;

internal static class Telemetry
{
    public static readonly ActivitySource Source = new("BeselerNet.Api");
    public static void SetTag_AccountId(this Activity activity, int accountId) =>
        activity.SetTag("account.id", accountId);
}
