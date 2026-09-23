namespace WebApp.Models;

public class FileInfoViewModel
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string UploadedAt { get; set; } = default!;
    public long FileSize { get; set; }
}
