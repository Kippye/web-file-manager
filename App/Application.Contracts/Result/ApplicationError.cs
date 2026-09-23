namespace Application.Contracts;

public class ApplicationError
{
    public EApplicationErrorCode Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Field { get; init; }
}
