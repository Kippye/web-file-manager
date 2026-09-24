using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.EF.OperationProcessing;

public interface IEntityActionStrategy
{
    public EntityState ForEntityState { get; set; }

    void Execute(EntityEntry entry);

    Task ExecuteAsync(EntityEntry entry, CancellationToken cancellationToken);
}
