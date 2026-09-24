using Domain.Base;
using Infrastructure.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.EF.OperationProcessing;

public class AuditEntityCreatedStrategy(IUserResolverService userResolver)
    : BaseEntityActionStrategy<IEntityMetadata>
{
    private readonly IUserResolverService _userResolver = userResolver;

    public override EntityState ForEntityState => EntityState.Added;

    protected override void ExecuteForType(EntityEntry entry, IEntityMetadata entity)
    {
        entry.CurrentValues[nameof(IEntityMetadata.CreatedAt)] = DateTime.UtcNow;

        var userGuid = _userResolver.GetCurrentUserGuid();
        // TODO: Do i need to look up user from DB?
        // Or is the claim verified automatically?
        // NOTE: CreatedById is null if not created by an user (e.g. seeded data)
        entry.CurrentValues[nameof(IEntityMetadata.CreatedById)] = userGuid;
    }
}
