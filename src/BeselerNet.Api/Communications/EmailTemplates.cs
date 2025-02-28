namespace BeselerNet.Api.Communications;

internal readonly struct EmailTemplate
{
    public required string CommunicationName { get; init; }
    public required string Subject { get; init; }
    public required string PlainTextContent { get; init; }
    public required string HtmlContent { get; init; }
}

internal static class EmailTemplates
{
    public static EmailTemplate EmailVerification(string confirmEmailUrl, string token) => new()
    {
        CommunicationName = "Email Verification",
        Subject = "Activate Your Account 🚀",
        PlainTextContent = $"To confirm your email, navigate to the following url in your browser: {confirmEmailUrl}?token={token}",
        HtmlContent = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Welcome to Beseler dotNET!</title>
            </head>
            <body style="font-family: Arial, sans-serif; background-color: #f5f5f5; color: #333; margin: 0; padding: 0;">
            <div style="max-width: 600px; margin: 20px auto; padding: 20px; background-color: #fff; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);">
              <h2>Welcome to Beseler dotNET! 🌟</h2>
              <p>We're thrilled to have you join our community! To start your journey with us, please click the button below to verify your email address and activate your account:</p>
              <p style="text-align: center;"><a href="{confirmEmailUrl}?token={token}" style="display: inline-block; padding: 12px 24px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 4px;">Verify My Email</a></p>
              <p>If the button above doesn't work, you can also copy and paste the following link into your browser:</p>
              <p>{confirmEmailUrl}?token={token}</p>
              <p>If you didn't create an account with us, no worries! Just ignore this email, and your information will remain safe.</p>
              <p>Happy exploring! 🎉</p>
              <p>Best regards,<br>The Beseler dotNET Team</p>
            </div>
            </body>
            </html>
            """
    };

    public static EmailTemplate AccountLocked(string recipientName) => new()
    {
        CommunicationName = "Account Locked",
        Subject = "Your Account is Locked 🔒",
        PlainTextContent = $"Your account has been locked due to too many failed login attempts. Please contact support.",
        HtmlContent = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Your Account is Locked</title>
            </head>
            <body style="font-family: Arial, sans-serif; background-color: #f5f5f5; color: #333; margin: 0; padding: 0;">
            <div style="max-width: 600px; margin: 20px auto; padding: 20px; background-color: #fff; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);">
              <h2>Your Account is Locked 🔒</h2>
              <p>Dear {recipientName},</p>
              <p>We regret to inform you that your account has been locked due to too many failed login attempts. For your security, we have temporarily disabled access to your account.</p>
              <p>If you believe this is a mistake or if you need assistance unlocking your account, please contact our support team.</p>
              <p>Thank you for your understanding.</p>
              <p>Best regards,<br>The Beseler dotNET Team</p>
            </div>
            </body>
            </html>
            """
    };

    public static EmailTemplate PasswordReset(string recipientName, string resetPasswordUrl, string token) => new()
    {
        CommunicationName = "Password Reset",
        Subject = "Reset Your Password 🔑",
        PlainTextContent = $"To reset your password, navigate to the following url in your browser: {resetPasswordUrl}?token={token}",
        HtmlContent = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Reset Your Password</title>
            </head>
            <body style="font-family: Arial, sans-serif; background-color: #f5f5f5; color: #333; margin: 0; padding: 0;">
            <div style="max-width: 600px; margin: 20px auto; padding: 20px; background-color: #fff; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);">
              <h2>Reset Your Password 🔑</h2>
              <p>Dear {recipientName},</p>
              <p>We received a request to reset your password. To proceed, please click the button below:</p>
              <p style="text-align: center;"><a href="{resetPasswordUrl}?token={token}" style="display: inline-block; padding: 12px 24px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 4px;">Reset My Password</a></p>
              <p>If the button above doesn't work, you can also copy and paste the following link into your browser:</p>
              <p>{resetPasswordUrl}?token={token}</p>
              <p>If you didn't request a password reset, please ignore this email.</p>
              <p>Best regards,<br>The Beseler dotNET Team</p>
            </div>
            </body>
            </html>
            """
    };
}
