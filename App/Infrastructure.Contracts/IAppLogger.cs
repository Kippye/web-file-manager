using Microsoft.Extensions.Logging;

namespace Infrastructure.Contracts;

public enum EventOutcome
{
    /// <summary>
    /// Operation completed
    /// </summary>
    Success,
    /// <summary>
    /// Operation failed
    /// </summary>
    Failure,
    /// <summary>
    /// Authorization or policy rejected the operation
    /// </summary>
    Denied,
    /// <summary>
    /// Requested resource was not found
    /// </summary>
    NotFound
}

/// <summary>
/// A logging wrapper to add outcomes to logs.
/// </summary>
/// <typeparam name="TCategoryName"></typeparam>
public interface IAppLogger<TCategoryName>
{
    void Log(LogLevel logLevel, EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogTrace(EventId eventId, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogDebug(EventId eventId, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogDebug(EventId eventId, EventOutcome? outcome, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogInformation(EventId eventId, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogInformation(EventId eventId, EventOutcome? outcome, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogWarning(EventId eventId, EventOutcome? outcome, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="exception"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogWarning(EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogError(EventId eventId, EventOutcome? outcome, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="exception"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogError(EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogCritical(EventId eventId, EventOutcome? outcome, string? message, params object?[] args);

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="outcome"></param>
    /// <param name="exception"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogCritical(EventId eventId, EventOutcome? outcome, Exception? exception, string? message, params object?[] args);
}
