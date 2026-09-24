using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.EF.OperationProcessing;

public class DataOperationProcessor : IDataOperationProcessor
{
    private readonly IEnumerable<IEntityActionStrategy> _entityActionStrategies;

    public DataOperationProcessor(IEnumerable<IEntityActionStrategy> entityActionStrategies)
    {
        _entityActionStrategies = entityActionStrategies;
    }

    public void Process(EntityEntry entry)
    {
        foreach (var strategy in _entityActionStrategies.Where(e => e.ForEntityState == entry.State))
        {
            strategy.Execute(entry);
        }
    }

    public async Task ProcessAsync(EntityEntry entry, CancellationToken cancellationToken)
    {
        foreach (var strategy in _entityActionStrategies.Where(e => e.ForEntityState == entry.State))
        {
            await strategy.ExecuteAsync(entry, cancellationToken);
        }
    }
}
