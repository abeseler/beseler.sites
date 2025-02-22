using BeselerNet.Shared.Core;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BeselerNet.Api.Communications;

internal sealed record SendGridOptions
{
    public const string SectionName = "SendGrid";
    public string? ApiKey { get; init; }
    public string? SenderEmail { get; init; }
    public string? SenderName { get; init; }
    public string? ConfirmEmailUrl { get; init; }
    public string? ResetPasswordUrl { get; init; }
}

internal sealed class SendGridEmailService(CommunicationDataSource communications, ISendGridClient client, IOptions<SendGridOptions> options, ILogger<SendGridEmailService> logger)
{
    private readonly CommunicationDataSource _communications = communications;
    private readonly ISendGridClient _client = client;
    private readonly SendGridOptions _options = options.Value;
    private readonly ILogger<SendGridEmailService> _logger = logger;
    public async Task<Result<Guid>> SendEmailVerification(int accountId, string email, string recipientName, string token, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Sending emails is disabled because SendGrid ApiKey is missing. Token not sent: {Token}", token);
            return Guid.Empty;
        }

        var communicationId = Guid.CreateVersion7();
        var emailMessage = new SendGridMessage
        {
            From = new EmailAddress(options.Value.SenderEmail, options.Value.SenderName),
            Subject = "Activate Your Account 🚀",
            PlainTextContent = $"To confirm your email, navigate to the following url in your browser: {_options.ConfirmEmailUrl}?token={token}",
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
                  <p style="text-align: center;"><a href="{_options.ConfirmEmailUrl}?token={token}" style="display: inline-block; padding: 12px 24px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 4px;">Verify My Email</a></p>
                  <p>If the button above doesn't work, you can also copy and paste the following link into your browser:</p>
                  <p>{_options.ConfirmEmailUrl}?token={token}</p>
                  <p>If you didn't create an account with us, no worries! Just ignore this email, and your information will remain safe.</p>
                  <p>Happy exploring! 🎉</p>
                  <p>Best regards,<br>The Beseler dotNET Team</p>
                </div>
                </body>
                </html>
                """,
            CustomArgs = new()
            {
                ["communication_id"] = communicationId.ToString(),
                ["account_id"] = accountId.ToString()
            }
        };
        emailMessage.AddTo(new EmailAddress(email, recipientName));
        var communication = Communication.Create(communicationId, accountId, CommunicationType.Email, "Email Verification");
        await _communications.SaveChanges(communication, stoppingToken);

        if (await _client.SendEmailAsync(emailMessage, stoppingToken) is { IsSuccessStatusCode: false } response)
        {
            var responseBody = await response.Body.ReadAsStringAsync(stoppingToken);
            _logger.LogError("Failed to send email: {Response}", responseBody);
            return new InvalidOperationException("Failed to send email.");
        }

        _logger.LogInformation("Email verification email sent to {Email}", email);

        return communicationId;
    }
    public async Task<Result<Guid>> SendAccountLocked(int accountId, string email, string recipientName, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Sending emails is disabled because SendGrid ApiKey is not set");
            return Guid.Empty;
        }

        var communicationId = Guid.CreateVersion7();
        var emailMessage = new SendGridMessage
        {
            From = new EmailAddress(options.Value.SenderEmail, options.Value.SenderName),
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
                """,
            CustomArgs = new()
            {
                ["communication_id"] = communicationId.ToString(),
                ["account_id"] = accountId.ToString()
            }
        };
        emailMessage.AddTo(new EmailAddress(email, recipientName));
        var communication = Communication.Create(communicationId, accountId, CommunicationType.Email, "Account Locked");
        await _communications.SaveChanges(communication, stoppingToken);

        if (await _client.SendEmailAsync(emailMessage, stoppingToken) is { IsSuccessStatusCode: false } response)
        {
            var responseBody = await response.Body.ReadAsStringAsync(stoppingToken);
            _logger.LogError("Failed to send email: {Response}", responseBody);
            return new InvalidOperationException("Failed to send email.");
        }

        _logger.LogInformation("Account locked email sent to {Email}", email);

        return communicationId;
    }
    public async Task<Result<Guid>> SendPasswordReset(int accountId, string email, string recipientName, string token, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Sending emails is disabled because SendGrid ApiKey is missing. Token not sent: {Token}", token);
            return Guid.Empty;
        }

        var communicationId = Guid.CreateVersion7();
        var emailMessage = new SendGridMessage
        {
            From = new EmailAddress(options.Value.SenderEmail, options.Value.SenderName),
            Subject = "Reset Your Password 🔑",
            PlainTextContent = $"To reset your password, navigate to the following url in your browser: {_options.ResetPasswordUrl}?token={token}",
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
                  <p style="text-align: center;"><a href="{_options.ResetPasswordUrl}?token={token}" style="display: inline-block; padding: 12px 24px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 4px;">Reset My Password</a></p>
                  <p>If the button above doesn't work, you can also copy and paste the following link into your browser:</p>
                  <p>{_options.ResetPasswordUrl}?token={token}</p>
                  <p>If you didn't request a password reset, please ignore this email.</p>
                  <p>Best regards,<br>The Beseler dotNET Team</p>
                </div>
                </body>
                </html>
                """,
            CustomArgs = new()
            {
                ["communication_id"] = communicationId.ToString(),
                ["account_id"] = accountId.ToString()
            }
        };
        emailMessage.AddTo(new EmailAddress(email, recipientName));
        var communication = Communication.Create(communicationId, accountId, CommunicationType.Email, "Password Reset");
        await _communications.SaveChanges(communication, stoppingToken);

        if (await _client.SendEmailAsync(emailMessage, stoppingToken) is { IsSuccessStatusCode: false } response)
        {
            var responseBody = await response.Body.ReadAsStringAsync(stoppingToken);
            _logger.LogError("Failed to send email: {Response}", responseBody);
            return new InvalidOperationException("Failed to send email.");
        }

        _logger.LogInformation("Password reset email sent to {Email}", email);

        return communicationId;
    }
}
