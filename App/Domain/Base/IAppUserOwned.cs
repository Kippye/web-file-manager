namespace Domain.Base;

/// <summary>
/// Entity is owned by one particular AppUser and contains its ID in the AppUserId property.
/// </summary>
public interface IAppUserOwned
{
    public Guid AppUserId { get; set; }
}