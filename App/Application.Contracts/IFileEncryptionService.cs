using DTO;

namespace Application.Contracts;

public interface IFileEncryptionService
{
    /// <summary>
    /// Get key, generate an unique nonce, and encrypt the content with them.
    /// </summary>
    /// <param name="content"></param>
    /// <returns>DTO containing encrypted file content, the nonce used, and the authentication tag</returns>
    Task<Result<EncryptedFile>> EncryptAsync(byte[] content);
    /// <summary>
    /// Get key and decrypt the encrypted file with it.
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="encryptedFile"></param>
    /// <returns>Decrypted content</returns>
    Task<Result<byte[]>> DecryptAsync(EncryptedFile encryptedFile);
}