using BeselerNet.Shared;

namespace BeselerNet.Web;

internal static class SiteConstants
{
    public const string SiteName = Branding.ProductName;
    public static readonly string Version = AppVersion.Of(typeof(SiteConstants).Assembly);
}
