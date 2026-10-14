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
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Result<Guid>> StoreFileAsync(FileUploadInfoDto fileInfo, EncryptedFile file, Guid userId);
    /// <summary>
    /// Get a list containing info for each of the user's files.
    /// </summary>'
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<FileInfoDto>> GetFileListAsync(Guid userId);
    /// <summary>
    /// Get a file's content as bytes and its metadata.
    /// </summary>
    /// <param name="id">The file's ID</param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Result<FileDto>> GetFileAsync(Guid id, Guid userId);
    /// <summary>
    /// Get a file's content as a readable stream and its metadata.
    /// </summary>
    /// <param name="id">The file's ID</param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Result<(Stream Stream, FileInfoDto Info)>> GetFileStreamAsync(Guid id, Guid userId);
    /// <summary>
    /// Attempt to delete a file.
    /// </summary>
    /// <param name="id">The file's ID</param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Result> DeleteFileAsync(Guid id, Guid userId);
    /// <summary>
    /// DANGER: Delete all of an user's files in storage.
    /// </summary>
    /// <returns></returns>
    Task<Result> DeleteUserFilesAsync(Guid userId);
    /// <summary>
    /// DANGER: Delete all files from storage and file metadata from database.
    /// User directories may or may not be deleted.
    /// </summary>
    /// <returns></returns>
    Task<Result> DeleteAllFilesAsync();
}
