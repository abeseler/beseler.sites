using Microsoft.AspNetCore.Mvc;

namespace BeselerNet.Api.Accounts;

internal static class AccountProblems
{
    public static ProblemDetails Locked { get; } = new()
    {
        Title = "Account Locked",
        Detail = "Your account is locked. Please contact support.",
        Status = StatusCodes.Status403Forbidden
    };
    public static ProblemDetails Disabled { get; } = new()
    {
        Title = "Account Disabled",
        Detail = "Your account is disabled. Please contact support.",
        Status = StatusCodes.Status403Forbidden
    };
    public static ProblemDetails EmailAlreadyVerified { get; } = new()
    {
        Title = "Email Already Verified",
        Detail = "Your email address has already been verified.",
        Status = StatusCodes.Status400BadRequest
    };
}
