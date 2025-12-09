using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Beseler.ServiceDefaults;

public static class HttpExtensions
{
    extension(IEndpointRouteBuilder builder)
    {
        public RouteHandlerBuilder MapOptions(string pattern, Delegate handler) => builder.MapMethods(pattern, [HttpMethod.Options.Method], handler);
        public RouteHandlerBuilder MapQuery(string pattern, Delegate handler) => builder.MapMethods(pattern, [HttpMethod.Query.Method], handler);
    }
}
