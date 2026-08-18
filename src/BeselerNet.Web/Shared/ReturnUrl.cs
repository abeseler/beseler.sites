namespace BeselerNet.Web;

internal static class ReturnUrl
{
    public const string Query = "return";

    public static string? Sanitize(string? value, bool allowVerify = false)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith('/')
            || value.StartsWith("//")
            || value.Contains('\\', StringComparison.Ordinal))
        {
            return null;
        }

        if (IsAuthPath(value)
            && !(allowVerify && PathOnly(value).Equals(Routes.VerifyEmail, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return value;
    }

    public static string WithReturn(string path, string? returnUrl)
    {
        var safe = Sanitize(returnUrl);
        return safe is null ? path : $"{path}?{Query}={Uri.EscapeDataString(safe)}";
    }

    public static string Destination(string? returnUrl, bool emailVerified) =>
        emailVerified
            ? Sanitize(returnUrl) ?? Routes.Dashboard
            : WithReturn(Routes.VerifyEmail, returnUrl);

    public static bool IsAuthPath(string? value)
    {
        var path = PathOnly(value);
        return path.Equals(Routes.Login, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.SignUp, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.ConfirmEmail, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.VerifyEmail, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.ForgotPassword, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.ResetPassword, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.AcceptInvite, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.EstablishSession, StringComparison.OrdinalIgnoreCase)
            || path.Equals(Routes.SignOut, StringComparison.OrdinalIgnoreCase);
    }

    private static string PathOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var query = value.IndexOf('?', StringComparison.Ordinal);
        return query < 0 ? value : value[..query];
    }
}
