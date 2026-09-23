namespace DTO;

public class FileDto
{
    public FileInfoDto Info { get; set; } = default!;
    public EncryptedFile File { get; set; } = default!;
}
