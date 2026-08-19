using BeselerNet.Api.Accounts.OAuth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeselerNet.Api.Budget;

internal static class BudgetProblems
{
    public static ProblemDetails YearNotFound { get; } = new()
    {
        Title = "Year Not Found",
        Detail = "That budget year has not been started.",
        Status = StatusCodes.Status404NotFound
    };

    public static ProblemDetails YearExists { get; } = new()
    {
        Title = "Year Exists",
        Detail = "That budget year has already been started.",
        Status = StatusCodes.Status409Conflict
    };

    public static ProblemDetails PastYear { get; } = new()
    {
        Title = "Past Year",
        Detail = "A past budget year cannot be started or deleted.",
        Status = StatusCodes.Status400BadRequest
    };

    public static ProblemDetails MonthNotFound { get; } = new()
    {
        Title = "Month Not Found",
        Detail = "That month is not part of a started budget year.",
        Status = StatusCodes.Status404NotFound
    };

    public static ProblemDetails LineNotFound { get; } = new()
    {
        Title = "Line Not Found",
        Detail = "That budget line does not exist.",
        Status = StatusCodes.Status404NotFound
    };

    public static ProblemDetails UnknownTimeZone { get; } = new()
    {
        Title = "Unknown Time Zone",
        Detail = "Send a valid IANA time zone in the X-Time-Zone header, or omit it to use UTC.",
        Status = StatusCodes.Status400BadRequest
    };

    public static ProblemDetails TemplateNotFound { get; } = new()
    {
        Title = "Template Not Found",
        Detail = "That budget template does not exist.",
        Status = StatusCodes.Status404NotFound
    };

    public static IResult? Forbid(ClaimsPrincipal user, int accountId, string action)
    {
        var auth = Authorizer.Authorize(user, new BudgetResource(accountId), action, requiredScope: null);
        if (auth.Failed(out var exception))
        {
            return TypedResults.Problem(new()
            {
                Title = "Forbidden",
                Detail = exception.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }

        return null;
    }
}
