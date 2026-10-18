using Infrastructure.Contracts;
using Serilog.Events;
namespace WebApp;

public class AppLogger<TCategoryName>() : IAppLogger<TCategoryName>
{
    public void Log(LogLevel logLevel, EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args)
    {
        var contextLogger = Serilog.Log
            .ForContext<TCategoryName>()
            .ForContext("EventId", eventId, destructureObjects: true);

        if (outcome.HasValue)
        {
            contextLogger = contextLogger.ForContext("Outcome", outcome.Value);
        }

        contextLogger.Write(
            (LogEventLevel)logLevel,
            exception,
            message ?? "",
            args
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
