namespace WebApp;

public static class DateExtensions
{
    /// <summary>
    /// Get UTC datetime string in a format that JavaScript Date can parse.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ToUtcString(this DateTime dateTime, string format = "yyyy-MM-dd HH:mm")
    {
        if (dateTime.Kind == DateTimeKind.Local)
        {
            return dateTime.ToUniversalTime().ToString(format) + " UTC";
        }
        return dateTime.ToString(format) + " UTC";
    }

    /// <summary>
    /// Get UTC date string in a format that JavaScript Date can parse.
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ToUtcString(this DateOnly date, string format = "yyyy-MM-dd")
    {
        return date.ToString(format) + " UTC";
    }

    /// <summary>
    /// Get UTC time string in a format that JavaScript Date can parse.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static string ToUtcString(this TimeOnly time, string format = "HH:mm")
    {
        return time.ToString(format) + " UTC";
    }
}
