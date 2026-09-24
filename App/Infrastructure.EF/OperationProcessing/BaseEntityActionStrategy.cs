using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.EF.OperationProcessing;

public abstract class BaseEntityActionStrategy<TEntity> : IEntityActionStrategy
{
    public virtual EntityState ForEntityState { get; set; }

    public void Execute(EntityEntry entry)
    {
        if (typeof(TEntity).IsAssignableFrom(entry.Metadata.ClrType))
        {
            ExecuteForType(entry, (TEntity)entry.Entity);
        }
    }

    public async Task ExecuteAsync(EntityEntry entry, CancellationToken cancellationToken)
    {
        if (typeof(TEntity).IsAssignableFrom(entry.Metadata.ClrType))
        {
            ExecuteForType(entry, (TEntity)entry.Entity);
        }
    }

    protected virtual void ExecuteForType(EntityEntry entry, TEntity entity)
    {
        throw new NotImplementedException();
    }
}
