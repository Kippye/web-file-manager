using Domain.Base;
using Infrastructure.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.EF.OperationProcessing;

public class AuditEntityUpdatedStrategy(IUserResolverService userResolver)
    : BaseEntityActionStrategy<IEntityMetadata>
{
    private readonly IUserResolverService _userResolver = userResolver;

    public override EntityState ForEntityState => EntityState.Modified;

    protected override void ExecuteForType(EntityEntry entry, IEntityMetadata entity)
    {
        entry.CurrentValues[nameof(IEntityMetadata.ChangedAt)] = DateTime.UtcNow;
        var userGuid = _userResolver.GetCurrentUserGuid();
        // TODO: Do i need to look up user from DB?
        // Or is the claim verified automatically?
        entry.CurrentValues[nameof(IEntityMetadata.ChangedById)] = userGuid;

        // Do not allow any update to modify the creation info
        entry.CurrentValues[nameof(IEntityMetadata.CreatedAt)] = entry.OriginalValues[nameof(IEntityMetadata.CreatedAt)];
        entry.Property(nameof(IEntityMetadata.CreatedAt)).IsModified = false;
        entry.CurrentValues[nameof(IEntityMetadata.CreatedById)] = entry.OriginalValues[nameof(IEntityMetadata.CreatedById)];
        entry.Property(nameof(IEntityMetadata.CreatedById)).IsModified = false;
    }
}
