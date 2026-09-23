using DTO;

namespace Application.Contracts;

/// <summary>
/// Handles filesystem and database access to files and metadata.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Store a file and add its metadata to database.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<Result<Guid>> StoreFileAsync(FileUploadInfoDto fileInfo, EncryptedFile file);
    /// <summary>
    /// Get a list containing info for each of the user's files.
    /// </summary>
    /// <returns></returns>
    Task<List<FileInfoDto>> GetFileListAsync();
    /// <summary>
    /// Get a file's content as bytes and its metadata.
    /// </summary>
    /// <param name="id">The file's ID</param>
    /// <returns></returns>
    Task<Result<FileDto>> GetFileAsync(Guid id);
    /// <summary>
    /// Get a file's content as a readable stream and its metadata.
    /// </summary>
    /// <param name="id">The file's ID</param>
    /// <returns></returns>
    Task<Result<(Stream Stream, FileInfoDto Info)>> GetFileStreamAsync(Guid id);
    /// <summary>
    /// Attempt to delete a file.
    /// </summary>
    /// <param name="id">The file's ID</param>
    /// <returns></returns>
    Task<Result> DeleteFileAsync(Guid id);
}
