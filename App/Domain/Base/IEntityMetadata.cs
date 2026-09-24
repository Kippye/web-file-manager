namespace Domain.Base;

public interface IEntityMetadata
{
    public DateTime CreatedAt { get; set; }

    public DateTime? ChangedAt { get; set; }

    public Guid? CreatedById { get; set; }

    public Guid? ChangedById { get; set; }
}