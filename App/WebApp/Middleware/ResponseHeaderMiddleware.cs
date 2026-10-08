namespace WebApp.Middleware;

public class ResponseHeaderMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
            {
                // Content-Security-Policy
                // * By default, only load resources that are same-origin with the document
                // * <object> and <embed> are blocked
                // * Allow inline CSS (otherwise some styling breaks - maybe TEMP)
                // * Don't allow the document to be embedded (clickjacking prevention)
                // * Upgrade to HTTPS (complements Strict-Transport-Security header)
                // NOTE: If JS is used, use Strict CSP (a random nonce for each response (has to be added to every <script> element's nonce attribute) or script hashes)
                // The nonce could easily just be a scoped service that generates a random GUID.
                context.Response.Headers.Append(
                    "Content-Security-Policy",
                    "default-src 'self'; object-src 'none'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; upgrade-insecure-requests"
                );
                return Task.FromResult(0);
            });

        await _next(context);
    }
}

public static class ResponseHeaderMiddlewareExtensions
{
    /// <summary>
    /// Add custom default headers to every response:
    /// <list type="bullet">
    /// Content-Security-Policy 
    /// </list>
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseResponseHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseHeaderMiddleware>();
    }
}
