using System.Security.Cryptography;
using System.Text;
using Application.Contracts;
using DTO;
using Microsoft.Extensions.Configuration;

namespace Application;

public class FileEncryptionService(IConfiguration configuration) : IFileEncryptionService
{
    // TEMP - will be received from secrets and probably not stored in memory
    private readonly byte[] Key = Encoding.UTF8.GetBytes(configuration.GetValue<string>("TempEncryptionKey")!);

    public async Task<Result<EncryptedFile>> EncryptAsync(byte[] content)
    {
        if (!AesGcm.IsSupported)
        {
            return Result<EncryptedFile>.Failure(
                EApplicationErrorCode.InvalidOperation,
                "The current platform does not support the AES-GCM algorithm."
            );
        }
        // TODO: Explicitly use the actual values specified in docs (even though they happen to be the same)
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var encryptedFile = new EncryptedFile(
            contentSize: content.Length,
            nonceSize: nonceSize,
            tagSize: tagSize
        );

        var aesGcm = new AesGcm(Key, tagSize);

        try
        {
            aesGcm.Encrypt(
                nonce: encryptedFile.Nonce,
                plaintext: content,
                ciphertext: encryptedFile.Content,
                tag: encryptedFile.AuthenticationTag
            );
        }
        catch
        {
            return Result<EncryptedFile>.Failure(EApplicationErrorCode.CryptographyError, "Failed to encrypt.");
        }

        return Result.Success(encryptedFile);
    }

    public async Task<Result<byte[]>> DecryptAsync(EncryptedFile encryptedFile)
    {
        var aesGcm = new AesGcm(Key, AesGcm.TagByteSizes.MaxSize);

        var plaintext = new byte[encryptedFile.Content.Length];
        try
        {
            aesGcm.Decrypt(
                nonce: encryptedFile.Nonce,
                ciphertext: encryptedFile.Content,
                tag: encryptedFile.AuthenticationTag,
                plaintext: plaintext
            );
        }
        catch (AuthenticationTagMismatchException e)
        {
            return Result<byte[]>.Failure(
                EApplicationErrorCode.AuthenticationTagError,
                "File has been modified."
            );
        }
        catch
        {
            return Result<byte[]>.Failure(
                EApplicationErrorCode.CryptographyError,
                "Failed to decrypt"
            );
        }
        return Result.Success(plaintext);
    }
}
