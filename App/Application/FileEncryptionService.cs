using System.Security.Cryptography;
using System.Text;
using Application.Contracts;
using DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application;

public class FileEncryptionService(IConfiguration configuration, ILogger<FileEncryptionService> logger) : IFileEncryptionService
{
    private byte[] Key { get => Encoding.UTF8.GetBytes(configuration.GetValue<string>("EncryptionKey")!); }

    public async Task<Result<EncryptedFile>> EncryptAsync(byte[] content)
    {
        if (!AesGcm.IsSupported)
        {
            return Result<EncryptedFile>.Failure(
                EApplicationErrorCode.InvalidOperation,
                "The current platform does not support the AES-GCM algorithm."
            );
        }
        var key = Key;
        if (key.Length < 32)
        {
            return Result<EncryptedFile>.Failure(
                EApplicationErrorCode.CryptographyError,
                "The encryption key must be 32 bytes or longer."
            );
        }

        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var encryptedFile = new EncryptedFile(
            contentSize: content.Length,
            nonceSize: nonceSize,
            tagSize: tagSize
        );

        try
        {
            var aesGcm = new AesGcm(key, tagSize);

            aesGcm.Encrypt(
                nonce: encryptedFile.Nonce,
                plaintext: content,
                ciphertext: encryptedFile.Content,
                tag: encryptedFile.AuthenticationTag
            );
        }
        catch (ArgumentException e)
        {
            logger.LogError("AES encrypt argument error. Error: " + e.Message);
            return Result<EncryptedFile>.Failure(EApplicationErrorCode.CryptographyError, "Failed to encrypt.");
        }
        catch (CryptographicException e)
        {
            logger.LogError("Failed to encrypt file. Error: " + e.Message);
            return Result<EncryptedFile>.Failure(EApplicationErrorCode.CryptographyError, "Failed to encrypt.");
        }

        return Result.Success(encryptedFile);
    }

    public async Task<Result<byte[]>> DecryptAsync(EncryptedFile encryptedFile)
    {

        var plaintext = new byte[encryptedFile.Content.Length];
        try
        {
            var aesGcm = new AesGcm(Key, AesGcm.TagByteSizes.MaxSize);

            aesGcm.Decrypt(
                nonce: encryptedFile.Nonce,
                ciphertext: encryptedFile.Content,
                tag: encryptedFile.AuthenticationTag,
                plaintext: plaintext
            );
        }
        catch (ArgumentException e)
        {
            logger.LogError("AES decrypt argument error. Error: " + e.Message);
            return Result<byte[]>.Failure(EApplicationErrorCode.CryptographyError, "Failed to decrypt.");
        }
        catch (AuthenticationTagMismatchException e)
        {
            logger.LogError("Authentication tag mismatch when decrypting. The file might have been modified! Error: " + e.Message);

            return Result<byte[]>.Failure(
                EApplicationErrorCode.AuthenticationTagError,
                "File has been modified."
            );
        }
        catch (CryptographicException e)
        {
            logger.LogError("Failed to decrypt file. Error: " + e.Message);
            return Result<byte[]>.Failure(EApplicationErrorCode.CryptographyError, "Failed to decrypt.");
        }

        return Result.Success(plaintext);
    }
}
