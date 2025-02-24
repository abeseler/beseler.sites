using Microsoft.AspNetCore.Mvc;

namespace BeselerNet.Api.Core;

internal sealed class Problems
{
    public static ProblemDetails TooManyRequests { get; } = new()
    {
        Title = "Too Many Requests",
        Detail = "The request could not be processed at this time. Please try again later.",
        Status = StatusCodes.Status429TooManyRequests
    };
}
