using Application.Contracts;
using Domain;
using DTO;
using Infrastructure.EF;
using Microsoft.Extensions.Hosting;

namespace Application;

public class FileStorageService(IHostEnvironment hostEnv, AppDbContext dbContext) : IFileStorageService
{
    public async Task<Result<Guid>> StoreFileAsync(FileInfoDto fileInfo, EncryptedFile file)
    {
        var storageDir = Path.Combine(hostEnv.ContentRootPath, "FileStorage");
        // Ensure the storage directory exists
        // TODO: Handle its exceptions
        Directory.CreateDirectory(storageDir);

        // TODO: Per-user directories? Would make it a bit harder for malicious files to access other users' files

        // Generate and use random safe file name + extension
        var fileName = Guid.NewGuid() + Path.GetExtension(fileInfo.FileName);
        var filePath = Path.Combine(storageDir, fileName);

        var addedFileEntry = dbContext.StoredFiles.Add(new StoredFile()
        {
            AppUserId = Guid.Empty, // TEMP
            FileSize = fileInfo.FileSize,
            OriginalFileName = fileInfo.FileName,
            Nonce = file.Nonce,
            AuthenticationTag = file.AuthenticationTag,
            StoragePath = filePath
        });

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
        throw new NotImplementedException();
    }

    public async Task<Result<(byte[] Content, FileInfoDto Info)>> GetFileAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<(Stream Stream, FileInfoDto Info)>> GetFileStreamAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteFileAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
