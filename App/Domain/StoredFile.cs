using Domain.Base;
using Domain.Identity;

namespace Domain;

public class StoredFile : BaseEntity
{
    /// <summary>
    /// The user who created and owns this file.
    /// </summary>
    public AppUser? OwnerUser { get; set; }
    /// <summary>
    /// Original file name of the uploaded file (including extension).
    /// </summary>
    public string OriginalFileName { get; set; } = default!;
    /// <summary>
    /// The relative path of the file in storage.
    /// </summary>
    public string StoragePath { get; set; } = default!;
    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public long FileSize { get; set; }
    /// <summary>
    /// A unique 12-byte nonce for this file.
    /// </summary>
    public byte[] Nonce { get; set; } = default!;
    /// <summary>
    /// The 16-byte authentication tag of this file.
    /// </summary>
    public byte[] AuthenticationTag { get; set; } = default!;
}
