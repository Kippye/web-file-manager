namespace DTO;

public class FileUploadInfoDto
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSize { get; set; }
}