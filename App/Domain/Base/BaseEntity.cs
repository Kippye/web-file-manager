namespace Domain.Base;

public abstract class BaseEntity : IBaseEntity, IEntityMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ChangedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? ChangedById { get; set; }
}
