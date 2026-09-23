using System.Security.Cryptography;

namespace DTO;

public class EncryptedFile(int contentSize, int nonceSize, int tagSize)
{
    public byte[] Content { get; set; } = new byte[contentSize];
    public byte[] Nonce { get; } = RandomNumberGenerator.GetBytes(nonceSize);
    public byte[] AuthenticationTag { get; set; } = new byte[tagSize];
}
