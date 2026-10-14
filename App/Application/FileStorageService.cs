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
    IUserResolverService userResolverService,
    IAppLogger<FileStorageService> logger,
    AppDbContext dbContext
) : IFileStorageService
{
    internal static EventId WriteFileEvent = new(100, "File written");
    internal static EventId ReadFileEvent = new(101, "File read");
    // internal static EventId UpdateFileEvent = new(102, "File updated");
    internal static EventId DeleteFileEvent = new(103, "File deleted");
    // internal static EventId DeleteUserFilesEvent = new(104, "User files deleted");
    internal static EventId DeleteAllFilesEvent = new(104, "All files deleted");

    private readonly string StorageDir = Path.Combine(hostEnv.ContentRootPath, "FileStorage");

    private static void LogFileStore(IAppLogger<FileStorageService> logger, LogLevel logLevel, EventOutcome? outcome, string message, string? filePath = default!, Guid? fileId = default!, Exception? exception = default!)
    {
        logger.Log(logLevel, WriteFileEvent, outcome, exception, message + " Path={FilePath} FileId={FileId}", filePath, fileId);
    }

    private static void LogFileRead(IAppLogger<FileStorageService> logger, LogLevel logLevel, EventOutcome? outcome, string message, string? filePath = default!, Guid? fileId = default!, Exception? exception = default!)
    {
        logger.Log(logLevel, ReadFileEvent, outcome, exception, message + " Path={FilePath} FileId={FileId}", filePath, fileId);
    }

    private static void LogFileDelete(IAppLogger<FileStorageService> logger, LogLevel logLevel, EventOutcome? outcome, string message, string? filePath = default!, Guid? fileId = default!, Exception? exception = default!)
    {
        logger.Log(logLevel, DeleteFileEvent, outcome, exception, message + " Path={FilePath} FileId={FileId}", filePath, fileId);
    }

    public async Task<Result<Guid>> StoreFileAsync(FileUploadInfoDto fileInfo, EncryptedFile file)
    {
        static Result<Guid> GetGenericFailure(EApplicationErrorCode errorCode) => Result<Guid>.Failure(errorCode, "Failed to store file.");

        // Ensure the storage directory exists
        try
        {
            Directory.CreateDirectory(StorageDir);
        }
        catch (UnauthorizedAccessException e)
        {
            LogFileStore(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "File permission error when creating file storage directory.",
                filePath: StorageDir,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (Exception e)
        {
            LogFileStore(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "Exception when creating file storage directory.",
                filePath: StorageDir,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        // Ensure the user subdirectory exists
        string userDirName = userResolverService.GetCurrentUserGuid()!.Value.ToString();
        string userDirPath = Path.Combine(StorageDir, userDirName);
        try
        {
            Directory.CreateDirectory(userDirPath);
        }
        catch (UnauthorizedAccessException e)
        {
            LogFileStore(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "File permission error when creating user directory in storage.",
                filePath: userDirPath,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (Exception e)
        {
            LogFileStore(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "Exception when creating user directory in storage.",
                filePath: userDirPath,
                exception: e
            );
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
        catch (Exception e)
        {
            LogFileStore(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "Failed to create file.",
                filePath: fileAbsolutePath,
                exception: e
            );
            // TODO: Handle each File.Create exception
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            LogFileStore(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "Failed to save file metadata",
                filePath: fileAbsolutePath,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.DatabaseError);
        }

        LogFileStore(
            logger,
            LogLevel.Information,
            EventOutcome.Success,
            "Stored file.",
            filePath: fileAbsolutePath,
            fileId: addedFileEntry.Entity.Id
        );
        return Result.Success(addedFileEntry.Entity.Id);
    }

    public async Task<List<FileInfoDto>> GetFileListAsync()
    {
        return await dbContext.StoredFiles
            .Where(f => f.CreatedById.HasValue && f.CreatedById.Equals(userResolverService.GetCurrentUserGuid()))
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

    public async Task<Result<FileDto>> GetFileAsync(Guid id)
    {
        static Result<FileDto> GetGenericFailure(EApplicationErrorCode errorCode) => Result<FileDto>.Failure(errorCode, "Failed to read file.");

        var fileMetadata = await dbContext.StoredFiles.FirstOrDefaultAsync(f =>
            f.Id.Equals(id) &&
            f.CreatedById.HasValue &&
            f.CreatedById.Equals(userResolverService.GetCurrentUserGuid())
        );

        if (fileMetadata is null)
        {
            // Try to get without filters. If it exists - access was unauthorized
            // This shouldn't enable enumeration by timing because the query also runs when there is no file
            var unfilteredFileMetadata = await dbContext.StoredFiles.IgnoreQueryFilters().FirstOrDefaultAsync(
                f => f.Id.Equals(id)
            );

            LogFileRead(
                logger,
                LogLevel.Error,
                unfilteredFileMetadata is null ? EventOutcome.NotFound : EventOutcome.Denied,
                unfilteredFileMetadata is null ? "File metadata not found." : "Access to file metadata not authorized.",
                fileId: id
            );
            return GetGenericFailure(EApplicationErrorCode.ResourceNotFound);
        }

        if (!Directory.Exists(StorageDir))
        {
            LogFileRead(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "File storage directory does not exist.",
                fileId: id
            );
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
                catch (EndOfStreamException e)
                {
                    LogFileRead(
                        logger,
                        LogLevel.Error,
                        EventOutcome.Failure,
                        "Stream from file ended unexpectedly.",
                        filePath: filePath,
                        fileId: id,
                        exception: e
                    );
                    return GetGenericFailure(EApplicationErrorCode.IOError);
                }

                LogFileRead(
                    logger,
                    LogLevel.Information,
                    EventOutcome.Success,
                    "Read file.",
                    filePath: filePath,
                    fileId: id
                );
                return Result.Success(file);
            }
        }
        catch (UnauthorizedAccessException e)
        {
            LogFileRead(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "File permission error when reading file.",
                filePath: filePath,
                fileId: id,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (FileNotFoundException e)
        {
            LogFileRead(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "File not found when reading file.",
                filePath: filePath,
                fileId: id,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.ResourceNotFound);
        }
        catch (Exception e)
        {
            LogFileRead(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "Exception when reading file.",
                filePath: filePath,
                fileId: id,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }
    }

    public async Task<Result<(Stream Stream, FileInfoDto Info)>> GetFileStreamAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteFileAsync(Guid id)
    {
        static Result GetGenericFailure(EApplicationErrorCode errorCode) => Result.Failure(errorCode, "Failed to delete file.");

        var fileMetadata = await dbContext.StoredFiles.FirstOrDefaultAsync(f =>
            f.Id.Equals(id) &&
            f.CreatedById != null &&
            f.CreatedById.Equals(userResolverService.GetCurrentUserGuid())
        );

        if (fileMetadata is null)
        {
            // Try to get without filters. If it exists - access was unauthorized
            var unfilteredFileMetadata = await dbContext.StoredFiles.IgnoreQueryFilters().FirstOrDefaultAsync(
                f => f.Id.Equals(id)
            );

            LogFileDelete(
                logger,
                LogLevel.Error,
                unfilteredFileMetadata is null ? EventOutcome.NotFound : EventOutcome.Denied,
                unfilteredFileMetadata is null ? "File metadata not found." : "Access to file metadata not authorized.",
                fileId: id
            );
            return GetGenericFailure(EApplicationErrorCode.ResourceNotFound);
        }

        if (!Directory.Exists(StorageDir))
        {
            LogFileDelete(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "File storage directory does not exist.",
                fileId: id
            );
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
        catch (UnauthorizedAccessException e)
        {
            LogFileDelete(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "File permission error when deleting file.",
                fileId: id,
                filePath: filePath,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.FilePermissionError);
        }
        catch (Exception e)
        {
            LogFileDelete(
                logger,
                LogLevel.Error,
                EventOutcome.Failure,
                "Exception when deleting file.",
                fileId: id,
                filePath: filePath,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.IOError);
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            LogFileDelete(
                logger,
                LogLevel.Critical,
                EventOutcome.Failure,
                "Exception when removing file metadata. There may be lingering database entries!",
                fileId: id,
                filePath: filePath,
                exception: e
            );
            return GetGenericFailure(EApplicationErrorCode.DatabaseError);
        }

        LogFileDelete(
            logger,
            LogLevel.Information,
            EventOutcome.Success,
            "Deleted file.",
            fileId: id,
            filePath: filePath
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
            catch (Exception e)
            {
                // TEMP: Currently, file name == file ID
                var fileId = Guid.Parse(Path.GetFileNameWithoutExtension(filePath));
                FailedDeletes.Add((fileId, e.Message));
                FailedFileIds.Add(fileId);
                logger.LogWarning(DeleteAllFilesEvent, EventOutcome.Failure, exception: e, "Failed to delete file with ID '{FileId}' at '{FilePath}'.", fileId, filePath);
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
            "Deleted {DeletedFileCount} files and {DeletedEntryCount} Db entries.",
            SuccessfulDeletes.Count,
            dbDeleteCount
        );
        return Result.Success();
    }
}
