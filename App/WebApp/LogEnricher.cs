using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;

namespace WebApp;

public class LogEnricher(IHttpContextAccessor httpContext) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string? userId = null;
        // string? requestTrace = null;
        System.Net.IPAddress? sourceAddress = null;
        if (httpContext.HttpContext != null)
        {
            userId = httpContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            // requestTrace = httpContext.HttpContext.TraceIdentifier;
            sourceAddress = httpContext.HttpContext.Connection.RemoteIpAddress;
        }

        if (userId is not null)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserId", userId));
        }
        // I think this isn't needed? Serilog's default @tr can be used to correlate based on request
        // if (requestTrace is not null)
        // {
        //     logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("RequestTrace", requestTrace));
        // }
        if (sourceAddress is not null)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SourceIp", sourceAddress));
        }
    }
}
