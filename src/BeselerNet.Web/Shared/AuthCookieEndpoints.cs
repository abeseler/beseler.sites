namespace BeselerNet.Web.Shared;

internal static class AuthCookieEndpoints
{
    public static void MapAuthCookieEndpoints(this WebApplication app)
    {
        app.MapGet(Routes.EstablishSession, Establish)
            .AllowAnonymous()
            .DisableAntiforgery();

        app.MapGet(Routes.SignOut, SignOut)
            .AllowAnonymous()
            .DisableAntiforgery();
    }

    private static IResult Establish(string? ticket, string? returnUrl, AuthCookie cookie)
    {
        if (string.IsNullOrWhiteSpace(ticket) || cookie.TakeHandoff(ticket) is not { } auth)
            return Results.Redirect(Routes.Login);

        cookie.Set(auth);
        return Results.Redirect(ReturnUrl.Sanitize(returnUrl, allowVerify: true) ?? Routes.Dashboard);
    }

    private static IResult SignOut(AuthCookie cookie)
    {
        cookie.Clear();
        return Results.Redirect(Routes.Login);
    }
}
