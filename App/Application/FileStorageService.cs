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
        // TODO: Handle its exceptions
        Directory.CreateDirectory(StorageDir);

        // TODO: Per-user directories? Would make it a bit harder for malicious files to access other users' files

        // Generate and use random safe file name
        // TODO: Central configuration for file extension
        var filePathInStorage = Path.ChangeExtension(Guid.NewGuid().ToString(), ".bin");

        // Note: Owner user is automatically set at EF-level from HTTP context
        var addedFileEntry = dbContext.StoredFiles.Add(new StoredFile()
        {
            FileSize = fileInfo.FileSize,
            OriginalFileName = fileInfo.FileName,
            Nonce = file.Nonce,
            AuthenticationTag = file.AuthenticationTag,
            StoragePath = filePathInStorage
        });

        var filePath = Path.Combine(StorageDir, filePathInStorage);

        try
        {
            await using (var stream = File.Create(filePath))
            {
                await stream.WriteAsync(file.Content, 0, file.Content.Length);
            }
        }
        catch
        {
            // TODO: Handle each File.Create exception
            return Result<Guid>.Failure(
                EApplicationErrorCode.UnknownError,
                "Failed to create file."
            );
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Stored file {Path}.", filePathInStorage);

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
            return Result<FileDto>.Failure(
                EApplicationErrorCode.ResourceNotFound,
                "File metadata not found."
            );
            // TODO: Probably log here, somebody might be trying to access unauthorized files
        }

        if (!Directory.Exists(StorageDir))
        {
            // TODO: Return correct error code
            return Result<FileDto>.Failure(
                EApplicationErrorCode.UnknownError,
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
                await fileStream.ReadExactlyAsync(file.File.Content);
                return Result.Success(file);
            }
        }
        catch
        {
            return Result<FileDto>.Failure(
                EApplicationErrorCode.UnknownError,
                "Failed to read file."
            );
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
            return Result.Failure(
                EApplicationErrorCode.ResourceNotFound,
                "File metadata not found."
            );
            // TODO: Probably log here, somebody might be trying to access unauthorized files
        }

        if (!Directory.Exists(StorageDir))
        {
            // TODO: Return correct error code
            return Result.Failure(
                EApplicationErrorCode.UnknownError,
                "File storage directory does not exist."
            );
        }

        dbContext.StoredFiles.Remove(fileMetadata);

        string filePath = Path.Combine(StorageDir, fileMetadata.StoragePath);

        try
        {
            File.Delete(filePath);
        }
        catch
        {
            return Result.Failure(EApplicationErrorCode.UnknownError, "Failed to delete file");
        }

        await dbContext.SaveChangesAsync();
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

        foreach (string filePath in Directory.EnumerateFiles(StorageDir))
        {
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
