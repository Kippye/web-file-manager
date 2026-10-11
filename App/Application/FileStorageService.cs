using Application.Contracts;
using Domain;
using DTO;
using Infrastructure.Contracts;
using Infrastructure.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application;

public class FileStorageService(IHostEnvironment hostEnv, IUserResolverService userResolverService, ILogger<FileStorageService> logger, AppDbContext dbContext) : IFileStorageService
{
    private readonly string StorageDir = Path.Combine(hostEnv.ContentRootPath, "FileStorage");

    public async Task<Result<Guid>> StoreFileAsync(FileUploadInfoDto fileInfo, EncryptedFile file)
    {
        // Ensure the storage directory exists
        try
        {
            Directory.CreateDirectory(StorageDir);
        }
        catch (UnauthorizedAccessException e)
        {
            logger.LogError("Not authorized to create file storage directory. Error:" + e.Message);
            return Result<Guid>.Failure(EApplicationErrorCode.FilePermissionError, "Not authorized to create storage directory.");
        }
        catch (Exception e)
        {
            logger.LogError("Exception when creating file storage directory. Error:" + e.Message);
            return Result<Guid>.Failure(EApplicationErrorCode.IOError, "Exception when creating storage directory.");
        }

        // Ensure the user subdirectory exists
        var userDirName = userResolverService.GetCurrentUserGuid()!.Value.ToString();
        try
        {
            Directory.CreateDirectory(Path.Combine(StorageDir, userDirName));
        }
        catch (UnauthorizedAccessException e)
        {
            logger.LogError("Not authorized to create directory for user '{UserID}' in storage. Error:" + e.Message, userDirName);
            return Result<Guid>.Failure(EApplicationErrorCode.FilePermissionError, "Not authorized to create user directory.");
        }
        catch (Exception e)
        {
            logger.LogError("Exception when creating directory for user '{UserID}' in storage. Error:" + e.Message, userDirName);
            return Result<Guid>.Failure(EApplicationErrorCode.IOError, "Exception when creating user directory.");
        }

        // Generate and use random safe file name
        // TODO: Central configuration for file extension
        var fileRelativePath = Path.Combine(userDirName, Path.ChangeExtension(Guid.NewGuid().ToString(), ".bin"));

        // Note: Owner user is automatically set at EF-level from HTTP context
        var addedFileEntry = dbContext.StoredFiles.Add(new StoredFile()
        {
            FileSize = fileInfo.FileSize,
            OriginalFileName = fileInfo.FileName,
            Nonce = file.Nonce,
            AuthenticationTag = file.AuthenticationTag,
            StoragePath = fileRelativePath
        });

        var fileAbsolutePath = Path.Combine(StorageDir, fileRelativePath);

        try
        {
            await using (var stream = File.Create(fileAbsolutePath))
            {
                await stream.WriteAsync(file.Content, 0, file.Content.Length);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Failed to create file at '{FilePath}'. Error: " + e.Message, fileAbsolutePath);
            // TODO: Handle each File.Create exception
            return Result<Guid>.Failure(
                EApplicationErrorCode.IOError,
                "Failed to create file."
            );
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError("Error when saving metadata for file at '{FilePath}'. Error: " + e.Message, fileAbsolutePath);
            return Result<Guid>.Failure(EApplicationErrorCode.DatabaseError, "Failed to save file metadata.");
        }

        logger.LogInformation("Stored file {Path}.", fileRelativePath);
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
        var fileMetadata = await dbContext.StoredFiles.FirstOrDefaultAsync(f =>
            f.Id.Equals(id) &&
            f.CreatedById.HasValue &&
            f.CreatedById.Equals(userResolverService.GetCurrentUserGuid())
        );

        if (fileMetadata is null)
        {
            // TODO: Probably log here, somebody might be trying to access unauthorized files
            return Result<FileDto>.Failure(
                EApplicationErrorCode.ResourceNotFound,
                "File metadata not found."
            );
        }

        if (!Directory.Exists(StorageDir))
        {
            return Result<FileDto>.Failure(
                EApplicationErrorCode.IOError,
                "File storage directory does not exist."
            );
        }

        string filePath = Path.Combine(StorageDir, fileMetadata.StoragePath);

        // TODO: Handle File.OpenRead exceptions
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
                // TODO: Handle end-of-stream exception
                try
                {
                    await fileStream.ReadExactlyAsync(file.File.Content);
                }
                catch (EndOfStreamException e)
                {
                    logger.LogError("Stream from file at '{FilePath}' ended unexpectedly. Error:" + e.Message, filePath);
                    return Result<FileDto>.Failure(EApplicationErrorCode.IOError, "File stream ended unexpectedly.");
                }
                return Result.Success(file);
            }
        }
        catch (UnauthorizedAccessException e)
        {
            logger.LogError("Not authorized to read file at '{FilePath}'. Error:" + e.Message, filePath);
            return Result<FileDto>.Failure(EApplicationErrorCode.FilePermissionError, "Not authorized to read file.");
        }
        catch (Exception e)
        {
            logger.LogError("Exception when reading file from '{FilePath}'. Error:" + e.Message, filePath);
            return Result<FileDto>.Failure(EApplicationErrorCode.IOError, "Failed to read file.");
        }
    }

    public async Task<Result<(Stream Stream, FileInfoDto Info)>> GetFileStreamAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteFileAsync(Guid id)
    {
        var fileMetadata = await dbContext.StoredFiles.FirstOrDefaultAsync(f =>
            f.Id.Equals(id) &&
            f.CreatedById != null &&
            f.CreatedById.Equals(userResolverService.GetCurrentUserGuid())
        );

        if (fileMetadata is null)
        {
            // TODO: Probably log here, somebody might be trying to access unauthorized files
            return Result.Failure(
                EApplicationErrorCode.ResourceNotFound,
                "File metadata not found."
            );
        }

        if (!Directory.Exists(StorageDir))
        {
            return Result.Failure(
                EApplicationErrorCode.IOError,
                "File storage directory does not exist."
            );
        }

        dbContext.StoredFiles.Remove(fileMetadata);

        string filePath = Path.Combine(StorageDir, fileMetadata.StoragePath);

        try
        {
            File.Delete(filePath);
        }
        catch (UnauthorizedAccessException e)
        {
            logger.LogError("Not authorized to delete file at '{FilePath}'. Error:" + e.Message, filePath);
            return Result.Failure(EApplicationErrorCode.FilePermissionError, "Not authorized to delete file.");
        }
        catch (Exception e)
        {
            logger.LogError("Exception when deleting file at '{FilePath}'. Error:" + e.Message, filePath);
            return Result.Failure(EApplicationErrorCode.IOError, "Failed to delete file.");
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError("Error when removing metadata for file at '{FilePath}'. Error: " + e.Message, filePath);
            return Result.Failure(EApplicationErrorCode.DatabaseError, "Failed to remove file metadata.");
        }

        logger.LogInformation("Deleted file at '{FilePath}", filePath);
        return Result.Success();
    }

    public async Task<Result> DeleteUserFilesAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteAllFilesAsync()
    {
        logger.LogInformation("Deleting all files and file metadata.");

        if (!Directory.Exists(StorageDir))
        {
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
                logger.LogDebug("Path '{Path}' was enumerated but is not an existing file.", filePath);

                continue;
            }
            try
            {
                logger.LogDebug("Deleting file at '{Path}'.", filePath);
                File.Delete(filePath);
                SuccessfulDeletes.Add(filePath);
            }
            catch (Exception e)
            {
                // TEMP: Currently, file name == file ID
                var fileId = Guid.Parse(Path.GetFileNameWithoutExtension(filePath));
                FailedDeletes.Add((fileId, e.Message));
                FailedFileIds.Add(fileId);
            }
        }

        // Now we delete every file metadata entry
        // Except files that we failed to delete (might be useful to know who uploaded them and when)
        var dbDeleteCount = await dbContext.StoredFiles.IgnoreQueryFilters()
            .Where(f => !FailedFileIds.Contains(f.Id))
            .ExecuteDeleteAsync();

        logger.LogInformation("Deleted {FileCount} files and {EntryCount} DB entries.", SuccessfulDeletes.Count, dbDeleteCount);

        // If any deletes failed - fail with a result containing errors for each file
        if (FailedDeletes.Count > 0)
        {
            if (FailedDeletes.Count <= 10)
            {
                FailedDeletes.ForEach(fail =>
                {
                    logger.LogWarning(
                        "Failed to delete file at '{FilePath}'; error: '{Error}'.",
                        fail.Item1,
                        fail.Item2
                    );
                });
            }
            else
            {
                logger.LogWarning("Failed to delete '{FailCount}' files.", FailedDeletes.Count);
            }

            return Result.Failure(
                FailedDeletes.Select(f => new ApplicationError()
                {
                    Code = EApplicationErrorCode.UnknownError,
                    Message = $"Failed to delete file at '{f.Item1}'."
                })
            );
        }

        return Result.Success();
    }
}
