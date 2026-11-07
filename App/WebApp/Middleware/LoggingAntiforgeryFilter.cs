using Infrastructure.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApp.Middleware;

/// <summary>
/// <para>An antiforgery filter that only applies to unsafe HTTP methods and logs token validation failures.</para>
/// NOTE: This is not compatible with the <c>IgnoreAntiforgeryToken</c> attribute.
/// </summary>
/// <param name="antiforgery"></param>
/// <param name="logger"></param>
public sealed class LoggingAntiforgeryFilter(
    IAntiforgery antiforgery,
    IAppLogger<LoggingAntiforgeryFilter> logger)
    : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS", "TRACE"
    };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var request = http.Request;

        if (SafeMethods.Contains(request.Method))
        {
            return;
        }

        // Let ASP.NET do the actual validation
        if (await antiforgery.IsRequestValidAsync(http))
        {
            return;
        }

        var endpoint = http.GetEndpoint()?.DisplayName;
        var origin = request.Headers.Origin.ToString();
        var referer = request.Headers.Referer.ToString();

        // We do NOT log the cookie / token. Just some details (that can easily be faked).
        logger.LogWarning(
            new EventId(3001, "Antiforgery validation failed"),
            "Antiforgery validation failed, origin: {Origin}, referer: {Referer}, endpoint: {Endpoint}",
            origin, referer, endpoint
        );

        context.Result = new RedirectToActionResult(actionName: "AntiforgeryError", controllerName: "Error", null);
    }
}
