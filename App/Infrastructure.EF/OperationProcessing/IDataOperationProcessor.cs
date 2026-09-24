using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.EF.OperationProcessing;

public interface IDataOperationProcessor
{
    void Process(EntityEntry entry);
    Task ProcessAsync(EntityEntry entry, CancellationToken cancellationToken);
}
