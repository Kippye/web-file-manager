using DTO;

namespace Application.Contracts;

public interface IFileValidator
{
    /// <summary>
    /// Validate a file's info.
    /// The returned result is successful if the file is valid and contains all validation errors otherwise.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    public Result Validate(FileUploadInfoDto fileInfo);
}
