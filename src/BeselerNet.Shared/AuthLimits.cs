namespace BeselerNet.Shared;

public static class AuthLimits
{
    public static readonly TimeSpan ConfirmEmail = TimeSpan.FromHours(24);
    public static readonly TimeSpan ResetPassword = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan Idle = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan PersistCookie = TimeSpan.FromDays(14);

    public const int PasswordMinLength = 8;
    public const int FailedLoginsBeforeLock = 5;
    public const string AccountLockedTitle = "Account Locked";

    public static string ConfirmEmailExpiryText => $"{(int)ConfirmEmail.TotalHours} hours";
    public static string ResetPasswordExpiryText => $"{(int)ResetPassword.TotalMinutes} minutes";
}
