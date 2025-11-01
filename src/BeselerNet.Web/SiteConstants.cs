namespace BeselerNet.Web;

internal static class SiteConstants
{
    public const string SiteName = "Beseler .NET";
}

internal static class Features
{
    public const string UserSignUp = "UserSignUp";
}

[Flags]
public enum LoginProvider
{
    None = 0,
    Email = 1,
    Google = 1 << 1,
    Facebook = 1 << 2,
    X = 1 << 3,
    GitHub = 1 << 4,
    OAuth = Google | Facebook | X | GitHub
}
