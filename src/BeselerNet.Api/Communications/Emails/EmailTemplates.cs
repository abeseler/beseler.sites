using System.Net;
using BeselerNet.Shared;

namespace BeselerNet.Api.Communications.Emails;

internal readonly struct EmailTemplate
{
    public readonly string SenderEmail { get; init; }
    public readonly string SenderName { get; init; }
    public required string CommunicationName { get; init; }
    public required string Subject { get; init; }
    public required string PlainTextContent { get; init; }
    public required string HtmlContent { get; init; }
}

internal static class EmailTemplates
{
    public static EmailTemplate EmailVerification(CommunicationOptions options, string token)
    {
        var url = ActionUrl(options.ConfirmEmailUrl, token);
        return Message(
            options,
            name: "Email Verification",
            subject: "Confirm your email",
            heading: "Confirm your email",
            plain:
                $"""
                Confirm your {Branding.ProductName} email by opening this link:

                {url}

                The link expires in {AuthLimits.ConfirmEmailExpiryText}.

                If you did not create an account, you can ignore this email.
                """,
            bodyHtml:
                $"""
                <p style="{P}">Confirm your email to finish setting up your {Encode(Branding.ProductName)} account.</p>
                {Button(url, "Confirm email")}
                <p style="{P}{Muted}">If the button does not work, paste this into your browser:</p>
                <p style="{P}{Muted}word-break:break-all;">{Encode(url)}</p>
                <p style="{P}{Muted}">The link expires in {AuthLimits.ConfirmEmailExpiryText}.</p>
                <p style="{P}{Muted}">If you did not create an account, ignore this email.</p>
                """);
    }

    public static EmailTemplate AccountLocked(CommunicationOptions options, string recipientName)
    {
        var forgotUrl = ForgotPasswordUrl(options.ResetPasswordUrl);
        var resetPlain = string.IsNullOrWhiteSpace(forgotUrl)
            ? "If this was you, use Forgot password on the sign-in page. The reset link unlocks the account."
            : $"If this was you, request a reset (it unlocks the account): {forgotUrl}";
        var resetHtml = string.IsNullOrWhiteSpace(forgotUrl)
            ? $"<p style=\"{P}\">If this was you, use Forgot password on the sign-in page. The reset link unlocks the account.</p>"
            : $"""
                <p style="{P}">If this was you, request a reset. The link unlocks the account.</p>
                {Button(forgotUrl, "Forgot password")}
                """;

        return Message(
            options,
            name: "Account Locked",
            subject: "Your account is locked",
            heading: "Your account is locked",
            plain:
                $"""
                Hi {recipientName},

                Your {Branding.ProductName} account was locked after too many failed sign-in attempts.

                {resetPlain}

                If this was not you, someone else may be trying to access the account.
                """,
            bodyHtml:
                $"""
                <p style="{P}">Hi {Encode(recipientName)},</p>
                <p style="{P}">Your account was locked after too many failed sign-in attempts.</p>
                {resetHtml}
                <p style="{P}{Muted}">If this was not you, someone else may be trying to access the account.</p>
                """);
    }

    public static EmailTemplate Invite(CommunicationOptions options, string recipientName, string token)
    {
        var url = ActionUrl(options.AcceptInviteUrl, token);
        return Message(
            options,
            name: "Account Invite",
            subject: $"You're invited to {Branding.ProductName}",
            heading: "You're invited",
            plain:
                $"""
                Hi {recipientName},

                You've been invited to {Branding.ProductName}. Set your password:

                {url}

                The link expires in {AuthLimits.InviteExpiryText}.
                """,
            bodyHtml:
                $"""
                <p style="{P}">Hi {Encode(recipientName)},</p>
                <p style="{P}">You've been invited to {Encode(Branding.ProductName)}. Set a password to join.</p>
                {Button(url, "Set password")}
                <p style="{P}{Muted}">If the button does not work, paste this into your browser:</p>
                <p style="{P}{Muted}word-break:break-all;">{Encode(url)}</p>
                <p style="{P}{Muted}">The link expires in {AuthLimits.InviteExpiryText}.</p>
                """);
    }

    public static EmailTemplate PasswordReset(CommunicationOptions options, string recipientName, string token)
    {
        var url = ActionUrl(options.ResetPasswordUrl, token);
        return Message(
            options,
            name: "Password Reset",
            subject: "Reset your password",
            heading: "Reset your password",
            plain:
                $"""
                Hi {recipientName},

                We received a request to reset your {Branding.ProductName} password. Open this link (it expires in {AuthLimits.ResetPasswordExpiryText}):

                {url}

                If you did not ask for a reset, you can ignore this email. Your password will stay the same.
                """,
            bodyHtml:
                $"""
                <p style="{P}">Hi {Encode(recipientName)},</p>
                <p style="{P}">We received a request to reset your password.</p>
                {Button(url, "Reset password")}
                <p style="{P}{Muted}">If the button does not work, paste this into your browser:</p>
                <p style="{P}{Muted}word-break:break-all;">{Encode(url)}</p>
                <p style="{P}{Muted}">The link expires in {AuthLimits.ResetPasswordExpiryText}.</p>
                <p style="{P}{Muted}">If you did not ask for a reset, ignore this email. Your password will stay the same.</p>
                """);
    }

    private static EmailTemplate Message(
        CommunicationOptions options,
        string name,
        string subject,
        string heading,
        string plain,
        string bodyHtml)
    {
        const string wrap = "font-family:'IBM Plex Mono',ui-monospace,Consolas,monospace;background-color:#0e1112;color:#d8d6d0;margin:0;padding:24px 12px;";
        const string card = "max-width:560px;margin:0 auto;padding:28px 24px;background-color:#161a1c;border:1px solid #2a3336;border-radius:10px;";
        const string mark = "margin:0 0 4px;color:#2ec4d6;font-size:12px;letter-spacing:0.04em;";
        const string h1 = "margin:0 0 16px;color:#d8d6d0;font-size:22px;font-weight:600;";
        const string foot = "margin:28px 0 0;color:#8b9398;font-size:13px;";

        return new()
        {
            SenderEmail = options.SenderEmail ?? throw new InvalidOperationException("SenderEmail is not configured."),
            SenderName = options.SenderName ?? Branding.ProductName,
            CommunicationName = name,
            Subject = subject,
            PlainTextContent = $"{plain.Trim()}\n\n— {Branding.ProductName}\n",
            HtmlContent = $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{Encode(subject)}</title>
                </head>
                <body style="{wrap}">
                <div style="{card}">
                  <p style="{mark}">beseler</p>
                  <h1 style="{h1}">{Encode(heading)}</h1>
                  {bodyHtml}
                  <p style="{foot}">{Encode(Branding.ProductName)}</p>
                </div>
                </body>
                </html>
                """
        };
    }

    private const string P = "margin:0 0 12px;color:#d8d6d0;font-size:15px;line-height:1.6;";
    private const string Muted = "color:#8b9398;";

    private static string Button(string url, string label) =>
        $"""
        <p style="margin:20px 0;text-align:center;">
          <a href="{Encode(url)}" style="display:inline-block;padding:12px 22px;background-color:#2ec4d6;color:#062226;text-decoration:none;border-radius:8px;font-weight:600;">{Encode(label)}</a>
        </p>
        """;

    private static string ActionUrl(string? baseUrl, string token)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Action URL is not configured.");

        var joiner = baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{baseUrl}{joiner}token={Uri.EscapeDataString(token)}";
    }

    private static string? ForgotPasswordUrl(string? resetUrl)
    {
        if (string.IsNullOrWhiteSpace(resetUrl))
            return null;

        return resetUrl.Replace("/account/reset-password", "/account/forgot-password", StringComparison.OrdinalIgnoreCase);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
