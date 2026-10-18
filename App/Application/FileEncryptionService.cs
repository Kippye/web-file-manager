using System.Security.Cryptography;
using System.Text;
using Application.Contracts;
using DTO;
using Infrastructure.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application;

public class FileEncryptionService(IConfiguration configuration, IAppLogger<FileEncryptionService> logger) : IFileEncryptionService
{
    private static readonly EventId EncryptFileEvent = new(100, "File encrypted");
    private static readonly EventId DecryptFileEvent = new(101, "File decrypted");

    private byte[] Key { get => Encoding.UTF8.GetBytes(configuration.GetValue<string>("EncryptionKey")!); }

    public async Task<Result<EncryptedFile>> EncryptAsync(byte[] content)
    {
        static Result<EncryptedFile> GetGenericFailure(EApplicationErrorCode errorCode) => Result<EncryptedFile>.Failure(errorCode, "Failed to encrypt.");

        if (!AesGcm.IsSupported)
        {
            logger.LogCritical(
                EncryptFileEvent,
                EventOutcome.Failure,
                "The current platform does not support the AES-GCM algorithm"
            );
            return GetGenericFailure(EApplicationErrorCode.InvalidOperation);
        }
        var key = Key;
        if (key.Length < 32)
        {
            logger.LogCritical(
                EncryptFileEvent,
                EventOutcome.Failure,
                "The encryption key is too short to encrypt with. It must be 32 bytes or longer"
            );
            return GetGenericFailure(EApplicationErrorCode.CryptographyError);
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
        catch (ArgumentException exception)
        {
            logger.LogError(EncryptFileEvent, EventOutcome.Failure, exception, "AES-GCM encrypt argument exception");
            return GetGenericFailure(EApplicationErrorCode.CryptographyError);
        }
        catch (CryptographicException exception)
        {
            logger.LogError(EncryptFileEvent, EventOutcome.Failure, exception, "Failed to encrypt file");
            return GetGenericFailure(EApplicationErrorCode.CryptographyError);
        }

        logger.LogInformation(EncryptFileEvent, EventOutcome.Success, "Encrypted file");
        return Result.Success(encryptedFile);
    }

    public async Task<Result<byte[]>> DecryptAsync(EncryptedFile encryptedFile, Guid? fileId)
    {
        static Result<byte[]> GetGenericFailure(EApplicationErrorCode errorCode) => Result<byte[]>.Failure(errorCode, "Failed to decrypt.");

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
        catch (ArgumentException exception)
        {
            logger.LogError(
                DecryptFileEvent,
                EventOutcome.Failure,
                exception,
                "AES decrypt argument exception when decrypting file with ID '{FileId}'",
                fileId
            );
            return GetGenericFailure(EApplicationErrorCode.CryptographyError);
        }
        catch (AuthenticationTagMismatchException exception)
        {
            logger.LogError(
                DecryptFileEvent,
                EventOutcome.Failure,
                exception,
                "Authentication tag mismatch when decrypting file with ID '{FileId}' - the file might have been modified",
                fileId
            );
            return Result<byte[]>.Failure(
                EApplicationErrorCode.AuthenticationTagError,
                "File has been modified and can't be decrypted."
            );
        }
        catch (CryptographicException exception)
        {
            logger.LogError(DecryptFileEvent, EventOutcome.Failure, exception, "Failed to decrypt file with ID '{FileId}'", fileId);
            return GetGenericFailure(EApplicationErrorCode.CryptographyError);
        }

        logger.LogInformation(DecryptFileEvent, EventOutcome.Success, "Decrypted file with ID '{FileId}'", fileId);
        return Result.Success(plaintext);
    }
}
