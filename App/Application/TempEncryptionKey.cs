using System.Security.Cryptography;

namespace Application;

public record TempEncryptionKey()
{
    public byte[] Key { get; set; } = RandomNumberGenerator.GetBytes(32);
}