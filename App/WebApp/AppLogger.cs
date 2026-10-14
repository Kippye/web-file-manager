using Infrastructure.Contracts;
namespace WebApp;

public class AppLogger<TCategoryName>(ILogger<TCategoryName> logger, IHttpContextAccessor httpContext, IUserResolverService userResolverService) : IAppLogger<TCategoryName>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return logger.BeginScope(state);
    }

    public void Log(LogLevel logLevel, EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args)
    {
        if (!logger.IsEnabled(logLevel))
        {
            return;
        }

        string? userId = userResolverService.GetCurrentUserGuid()?.ToString();
        string? requestTrace = null;
        System.Net.IPAddress? sourceAddress = null;
        if (httpContext.HttpContext != null)
        {
            sourceAddress = httpContext.HttpContext.Connection.RemoteIpAddress;
            requestTrace = httpContext.HttpContext.TraceIdentifier;
        }

        var combinedArgs = args
            .Concat([userId, requestTrace, sourceAddress?.MapToIPv4(), outcome])
            .ToArray();

        logger.Log(
            logLevel,
            eventId,
            exception,
            (message ?? "") + " UserId={UserId} RequestTrace={RequestTrace} SourceIp={SourceIp} Outcome={Outcome}",
            combinedArgs
        );
    }

    public void LogTrace(EventId eventId, string? message, params object?[] args)
    {
        Log(LogLevel.Trace, eventId, outcome: null, exception: null, message, args);
    }

    public void LogDebug(EventId eventId, string? message, params object?[] args)
    {
        Log(LogLevel.Debug, eventId, outcome: null, exception: null, message, args);
    }

    public void LogDebug(EventId eventId, EventOutcome? outcome, string? message, params object?[] args)
    {
        Log(LogLevel.Debug, eventId, outcome, exception: null, message, args);
    }

    public void LogInformation(EventId eventId, string? message, params object?[] args)
    {
        Log(LogLevel.Information, eventId, outcome: null, exception: null, message, args);
    }

    public void LogInformation(EventId eventId, EventOutcome? outcome, string? message, params object?[] args)
    {
        Log(LogLevel.Information, eventId, outcome, exception: null, message, args);
    }

    public void LogWarning(EventId eventId, EventOutcome? outcome, string? message, params object?[] args)
    {
        Log(LogLevel.Warning, eventId, outcome, exception: null, message, args);
    }

    public void LogWarning(EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args)
    {
        Log(LogLevel.Warning, eventId, outcome, exception, message, args);
    }

    public void LogError(EventId eventId, EventOutcome? outcome, string? message, params object?[] args)
    {
        Log(LogLevel.Error, eventId, outcome, exception: null, message, args);
    }

    public void LogError(EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args)
    {
        Log(LogLevel.Error, eventId, outcome, exception, message, args);
    }

    public void LogCritical(EventId eventId, EventOutcome? outcome, string? message, params object?[] args)
    {
        Log(LogLevel.Critical, eventId, outcome, exception: null, message, args);
    }

    public void LogCritical(EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args)
    {
        Log(LogLevel.Critical, eventId, outcome, exception, message, args);
    }
}
