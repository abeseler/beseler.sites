using BeselerNet.Shared;
using BeselerNet.Shared.Core;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.OAuth;

internal static class Authorizer
{
    public static Result Authorize<TResource>(ClaimsPrincipal principal, TResource resource, string action, string? requiredScope) where TResource : IAuthorizableResource
    {
        if (principal.Identity is not { IsAuthenticated: true })
        {
            return new UnauthorizedAccessException("Authorization failed: User is not authenticated.");
        }

        var claimType = $"{TResource.ResourceName}:{action}";
        var claimValue = principal.FindFirstValue(claimType);
        if (claimValue is null)
        {
            return new UnauthorizedAccessException($"Authorization failed: Missing required claim [{claimType}].");
        }

        var scopes = claimValue.Split(' ') ?? [];

        if (requiredScope == Scopes.Owned)
        {
            if (!scopes.Contains(Scopes.Owned))
            {
                return new UnauthorizedAccessException($"Authorization failed: User does not have the required scope [{requiredScope}]. Found [{claimValue}].");
            }
            if (resource is not IOwnedResource ownedResource)
            {
                return new UnauthorizedAccessException($"Authorization failed: Resource is not owned.");
            }
            if (!ownedResource.IsOwnedBy(principal))
            {
                return new UnauthorizedAccessException($"Authorization failed: User does not own the resource.");
            }
            return Result.Success;
        }
        else if (requiredScope == Scopes.Shared)
        {
            if (!scopes.Contains(Scopes.Shared))
            {
                return new UnauthorizedAccessException($"Authorization failed: User does not have the required scope [{requiredScope}]. Found [{claimValue}].");
            }
            if (resource is not ISharedResource sharedResource)
            {
                return new UnauthorizedAccessException($"Authorization failed: Resource is not shared.");
            }
            if (!sharedResource.IsSharedWith(principal))
            {
                return new UnauthorizedAccessException($"Authorization failed: Resource has not been shared with the user.");
            }
            return Result.Success;
        }
        else if (requiredScope is not null && !scopes.Contains(requiredScope))
        {
            return new UnauthorizedAccessException($"Authorization failed: User does not have the required scope [{requiredScope}]. Found [{claimValue}].");
        }

        foreach (var scope in scopes)
        {
            var isAuthorized = scope switch
            {
                Scopes.Global => true,
                Scopes.Owned => resource is IOwnedResource owned && owned.IsOwnedBy(principal),
                Scopes.Shared => resource is ISharedResource shared && shared.IsSharedWith(principal),
                Scopes.Self => true,
                _ => false
            };
            if (isAuthorized)
            {
                return Result.Success;
            }
        }
        return new UnauthorizedAccessException($"Authorization failed: No supported scopes found in [{claimValue}] for action [{action}] on resource [{TResource.ResourceName}].");
    }
}

internal interface IAuthorizableResource
{
    public static abstract string ResourceName { get; }
}
internal enum ResourceAction
{
    create,
    read,
    update,
    delete
}
internal interface IOwnedResource
{
    bool IsOwnedBy(ClaimsPrincipal user);
}
internal interface ISharedResource
{
    bool IsSharedWith(ClaimsPrincipal user);
}
