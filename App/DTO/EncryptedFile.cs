using System.Security.Cryptography;

namespace DTO;

public class EncryptedFile
{
    public byte[] Content { get; set; }
    public byte[] Nonce { get; }
    public byte[] AuthenticationTag { get; set; }

    /// <summary>
    /// Create an EncryptedFile with properly sized byte arrays for input and a random nonce. 
    /// </summary>
    /// <param name="contentSize"></param>
    /// <param name="nonceSize"></param>
    /// <param name="tagSize"></param>
    public EncryptedFile(long contentSize, int nonceSize, int tagSize)
    {
        Content = new byte[contentSize];
        Nonce = RandomNumberGenerator.GetBytes(nonceSize);
        AuthenticationTag = new byte[tagSize];
    }

    /// <summary>
    /// Create an EncryptedFile with a correctly sized content array and specific nonce and tag.
    /// </summary>
    /// <param name="contentSize"></param>
    /// <param name="nonce"></param>
    /// <param name="authenticationTag"></param>
    public EncryptedFile(long contentSize, byte[] nonce, byte[] authenticationTag)
    {
        Content = new byte[contentSize];
        Nonce = nonce;
        AuthenticationTag = authenticationTag;
    }
}
