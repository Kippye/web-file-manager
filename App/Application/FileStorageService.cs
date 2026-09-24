using Application.Contracts;
using Domain;
using DTO;
using Infrastructure.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application;

public class FileStorageService(IHostEnvironment hostEnv, AppDbContext dbContext) : IFileStorageService
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

        return Result.Success(addedFileEntry.Entity.Id);
    }

    public async Task<List<FileInfoDto>> GetFileListAsync()
    {
        return await dbContext.StoredFiles
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
        var fileMetadata = await dbContext.StoredFiles.FindAsync(id);

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
        var fileMetadata = await dbContext.StoredFiles.FindAsync(id);

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
}
