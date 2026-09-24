using Domain.Base;
using Microsoft.AspNetCore.Identity;

namespace Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public ICollection<StoredFile> StoredFiles { get; set; } = [];
}
