using Application.Contracts;
using Domain;
using DTO;
using Infrastructure.Contracts;
using Infrastructure.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application;

public class FileStorageService(
    IHostEnvironment hostEnv,
    IAppLogger<FileStorageService> logger,
    AppDbContext dbContext
) : IFileStorageService
{
    private static readonly EventId WriteFileEvent = new(100, "File written");
    private static readonly EventId ReadFileEvent = new(101, "File read");
    private static readonly EventId DeleteFileEvent = new(103, "File deleted");
    private static readonly EventId DeleteAllFilesEvent = new(104, "All files deleted");

    private readonly string StorageDir = Path.Combine(hostEnv.ContentRootPath, "FileStorage");

    private bool AssertStorageDirExists(EventId eventId)
    {
        if (!Directory.Exists(StorageDir))
        {
            logger.LogError(
                eventId,
                EventOutcome.Failure,
                "File storage directory does not exist."
            );
            return false;
        }
        return true;
    }

    public async Task<Result<Guid>> StoreFileAsync(FileUploadInfoDto fileInfo, EncryptedFile file, Guid userId)
    {
        static Result<Guid> GetGenericFailure(EApplicationErrorCode errorCode) => Result<Guid>.Failure(errorCode, "Failed to store file.");

        // Ensure the storage directory exists
        try
        {
            Directory.CreateDirectory(StorageDir);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogError(WriteFileEvent, EventOutcome.Failure, exception, "File permission error when creating file storage directory at '{FilePath}'.", StorageDir);
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (Exception exception)
        {
            logger.LogError(WriteFileEvent, EventOutcome.Failure, exception, "Exception when creating file storage directory at '{FilePath}'.", StorageDir);
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        // Ensure the user subdirectory exists
        string userDirName = userId.ToString();
        string userDirPath = Path.Combine(StorageDir, userDirName);
        try
        {
            Directory.CreateDirectory(userDirPath);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogError(WriteFileEvent, EventOutcome.Failure, exception, "File permission error when creating user directory in storage at '{FilePath}'.", userDirPath);
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (Exception exception)
        {
            logger.LogError(WriteFileEvent, EventOutcome.Failure, exception: exception, "Exception when creating user directory in storage at '{FilePath}'.", userDirPath);
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        // Generate and use random safe file name
        // TODO: Central configuration for file extension
        var fileRelativePath = Path.Combine(userDirName, Path.ChangeExtension(Guid.NewGuid().ToString(), ".bin"));
        string fileAbsolutePath = Path.Combine(StorageDir, fileRelativePath);

        // Note: Owner user is automatically set at EF-level from HTTP context
        var addedFileEntry = dbContext.StoredFiles.Add(new StoredFile()
        {
            FileSize = fileInfo.FileSize,
            OriginalFileName = fileInfo.FileName,
            Nonce = file.Nonce,
            AuthenticationTag = file.AuthenticationTag,
            StoragePath = fileRelativePath
        });

        try
        {
            await using (var stream = File.Create(fileAbsolutePath))
            {
                await stream.WriteAsync(file.Content, 0, file.Content.Length);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(WriteFileEvent, EventOutcome.Failure, exception: exception, "Failed to create file at '{FilePath}'.", fileAbsolutePath);
            // TODO: Handle each File.Create exception
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(WriteFileEvent, EventOutcome.Failure, exception: exception, "Failed to save file metadata for file at '{FilePath}'.", fileAbsolutePath);
            return GetGenericFailure(EApplicationErrorCode.DatabaseError);
        }

        logger.LogInformation(WriteFileEvent, EventOutcome.Success, "Stored file at '{FilePath}' with ID '{FileId}'.", fileAbsolutePath, addedFileEntry.Entity.Id);
        return Result.Success(addedFileEntry.Entity.Id);
    }

    public async Task<List<FileInfoDto>> GetFileListAsync(Guid userId)
    {
        return await dbContext.StoredFiles
            .Where(f => f.CreatedById.HasValue && f.CreatedById.Equals(userId))
            .OrderByDescending(f => f.CreatedAt)
            .Select(f =>
                new FileInfoDto()
                {
                    Id = f.Id,
                    FileName = f.OriginalFileName,
                    FileSize = f.FileSize,
                    UploadedAt = f.CreatedAt,
                    ContentType = "N/A" // TEMP
                }
            )
            .ToListAsync();
    }

    public async Task<Result<FileDto>> GetFileAsync(Guid id, Guid userId)
    {
        static Result<FileDto> GetGenericFailure(EApplicationErrorCode errorCode) => Result<FileDto>.Failure(errorCode, "Failed to read file.");

        var fileMetadata = await dbContext.StoredFiles.FirstOrDefaultAsync(f =>
            f.Id.Equals(id) &&
            f.CreatedById.HasValue &&
            f.CreatedById.Equals(userId)
        );

        if (fileMetadata is null)
        {
            // Try to get without filters. If it exists - access was unauthorized
            // This shouldn't enable enumeration by timing because the query also runs when there is no file
            var unfilteredFileMetadata = await dbContext.StoredFiles.IgnoreQueryFilters().FirstOrDefaultAsync(
                f => f.Id.Equals(id)
            );

            logger.LogError(ReadFileEvent, unfilteredFileMetadata is null ? EventOutcome.NotFound : EventOutcome.Denied, "File metadata not found for file with ID '{FileId}'.", id);
            return GetGenericFailure(EApplicationErrorCode.ResourceNotFound);
        }

        if (!AssertStorageDirExists(ReadFileEvent))
        {
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        string filePath = Path.Combine(StorageDir, fileMetadata.StoragePath);

        try
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var streamLength = fileStream.Length;
                var file = new FileDto()
                {
                    Info = new FileInfoDto()
                    {
                        Id = id,
                        FileName = fileMetadata.OriginalFileName,
                        FileSize = fileMetadata.FileSize,
                        UploadedAt = fileMetadata.CreatedAt,
                        ContentType = "N/A" // TEMP
                    },
                    File = new EncryptedFile(streamLength, fileMetadata.Nonce, fileMetadata.AuthenticationTag)
                };
                try
                {
                    await fileStream.ReadExactlyAsync(file.File.Content);
                }
                catch (EndOfStreamException exception)
                {
                    logger.LogError(ReadFileEvent, EventOutcome.Failure, exception: exception, "Stream from file ended unexpectedly for file at '{FilePath}' with ID '{FileId}'.", filePath, id);
                    return GetGenericFailure(EApplicationErrorCode.IOError);
                }

                logger.LogInformation(ReadFileEvent, EventOutcome.Success, "Read file with FileId '{FileId}' at '{FilePath}'.", id, filePath);
                return Result.Success(file);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogError(ReadFileEvent, EventOutcome.Failure, exception: exception, "File permission error when reading file with ID '{FileId}' at '{FilePath}'.", id, filePath);
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (FileNotFoundException exception)
        {
            logger.LogError(
                ReadFileEvent,
                EventOutcome.Failure,
                exception,
                "File with ID '{FileId}' not found at path '{FilePath}'.",
                id,
                filePath
            );
            return GetGenericFailure(EApplicationErrorCode.ResourceNotFound);
        }
        catch (Exception exception)
        {
            logger.LogError(
                ReadFileEvent,
                EventOutcome.Failure,
                exception,
                "Exception when reading file with ID '{FileId}' at path '{FilePath}'.",
                id,
                filePath
            );
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }
    }

    public async Task<Result<(Stream Stream, FileInfoDto Info)>> GetFileStreamAsync(Guid id, Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteFileAsync(Guid id, Guid userId)
    {
        static Result GetGenericFailure(EApplicationErrorCode errorCode) => Result.Failure(errorCode, "Failed to delete file.");

        var fileMetadata = await dbContext.StoredFiles.FirstOrDefaultAsync(f =>
            f.Id.Equals(id) &&
            f.CreatedById != null &&
            f.CreatedById.Equals(userId)
        );

        if (fileMetadata is null)
        {
            // Try to get without filters. If it exists - access was unauthorized
            var unfilteredFileMetadata = await dbContext.StoredFiles.IgnoreQueryFilters().FirstOrDefaultAsync(
                f => f.Id.Equals(id)
            );

            logger.LogError(
                DeleteFileEvent,
                unfilteredFileMetadata is null ? EventOutcome.NotFound : EventOutcome.Denied,
                unfilteredFileMetadata is null ? "Metadata not found for file ID '{FileId}'." : "Access to metadata of file with ID '{FileId}' not authorized.",
                id
            );
            return GetGenericFailure(EApplicationErrorCode.ResourceNotFound);
        }

        if (!AssertStorageDirExists(DeleteFileEvent))
        {
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        dbContext.StoredFiles.Remove(fileMetadata);

        string filePath = Path.Combine(StorageDir, fileMetadata.StoragePath);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                return GetGenericFailure(EApplicationErrorCode.ResourceNotFound);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogError(
                DeleteFileEvent,
                EventOutcome.Failure,
                exception,
                "File permission error when deleting file with ID '{FileId}' at '{FilePath}'.",
                id,
                filePath
            );
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (Exception exception)
        {
            logger.LogError(
                DeleteFileEvent,
                EventOutcome.Failure,
                exception,
                "Exception when deleting file with ID '{FileId}' at '{FilePath}'.",
                id,
                filePath
            );
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                DeleteFileEvent,
                EventOutcome.Failure,
                exception,
                "Exception when removing metadata of file with ID '{FileId}'. There may be lingering database entries!.",
                id
            );
            return GetGenericFailure(EApplicationErrorCode.DatabaseError);
        }

        logger.LogInformation(
            DeleteFileEvent,
            EventOutcome.Success,
            "Deleted file with ID '{FileId}' at '{FilePath}'.",
            id,
            filePath
        );
        return Result.Success();
    }

    public async Task<Result> DeleteUserFilesAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteAllFilesAsync()
    {
        if (!Directory.Exists(StorageDir))
        {
            logger.LogInformation(DeleteAllFilesEvent, EventOutcome.Success, "Storage directory doesn't exist - no files to delete.");
            // Storage dir doesn't exist so no files exist - done
            return Result.Success();
        }

        List<(Guid, string)> FailedDeletes = [];
        HashSet<Guid> FailedFileIds = [];
        HashSet<string> SuccessfulDeletes = [];

        // NOTE: This loops recursively over every .bin file in the file storage
        // Maybe it would be better to just loop through all users and delete their files with DeleteUserFilesAsync
        foreach (string filePath in Directory.EnumerateFiles(
            StorageDir,
            "*.bin",
            new EnumerationOptions()
            {
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false
            }
        ))
        {
            // Ensure that the path points to a *file*, not a directory
            if (!File.Exists(filePath))
            {
                logger.LogWarning(DeleteAllFilesEvent, outcome: null, "Path '{FilePath}' was enumerated but is not an existing file.", filePath);
                continue;
            }
            try
            {
                logger.LogDebug(DeleteAllFilesEvent, "Deleting file at '{FilePath}'.", filePath);
                File.Delete(filePath);
                SuccessfulDeletes.Add(filePath);
            }
            catch (Exception exception)
            {
                // TEMP: Currently, file name == file ID
                var fileId = Guid.Parse(Path.GetFileNameWithoutExtension(filePath));
                FailedDeletes.Add((fileId, exception.Message));
                FailedFileIds.Add(fileId);
                logger.LogWarning(DeleteAllFilesEvent, EventOutcome.Failure, exception: exception, "Failed to delete file with ID '{FileId}' at '{FilePath}'.", fileId, filePath);
            }
        }

        // Now we delete every file metadata entry
        // Except files that we failed to delete (might be useful to know who uploaded them and when)
        var dbDeleteCount = await dbContext.StoredFiles.IgnoreQueryFilters()
            .Where(f => !FailedFileIds.Contains(f.Id))
            .ExecuteDeleteAsync();

        // If any deletes failed - fail with a result containing errors for each file
        if (FailedDeletes.Count > 0)
        {
            logger.LogError(
                DeleteAllFilesEvent,
                EventOutcome.Failure,
                "Failed to delete all files. Deleted {DeletedFileCount} files and {DeletedEntryCount} DB entries; Failed to delete {FailedDeleteCount} files.",
                SuccessfulDeletes.Count,
                dbDeleteCount,
                FailedDeletes.Count
            );
            return Result.Failure(
                FailedDeletes.Select(f => new ApplicationError()
                {
                    Code = EApplicationErrorCode.UnknownError,
                    Message = $"Failed to delete file at '{f.Item1}'."
                })
            );
        }

        logger.LogInformation(
            DeleteAllFilesEvent,
            EventOutcome.Success,
            "Deleted {DeletedFileCount} files and {DeletedEntryCount} DB entries.",
            SuccessfulDeletes.Count,
            dbDeleteCount
        );
        return Result.Success();
    }
}
