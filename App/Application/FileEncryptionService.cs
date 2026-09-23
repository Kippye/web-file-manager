using System.Security.Cryptography;
using Application.Contracts;
using DTO;

namespace Application;

public class FileEncryptionService(TempEncryptionKey encryptionKey) : IFileEncryptionService
{
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

        // TEMP - will be received from secrets
        // 256-bit key though
        var key = encryptionKey.Key;

        var encryptedFile = new EncryptedFile(
            contentSize: content.Length,
            nonceSize: nonceSize,
            tagSize: tagSize
        );

        var aesGcm = new AesGcm(encryptionKey.Key, tagSize);

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
        var aesGcm = new AesGcm(encryptionKey.Key, AesGcm.TagByteSizes.MaxSize);

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
