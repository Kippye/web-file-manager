using Domain;
using Domain.Identity;
using Infrastructure.Contracts;
using Infrastructure.EF.OperationProcessing;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EF;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public DbSet<StoredFile> StoredFiles { get; set; }

    private readonly IDataOperationProcessor _dataOperationProcessor;
    private readonly IUserResolverService _userResolverService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IUserResolverService userResolverService, IDataOperationProcessor dataOperationProcessor)
        : base(options)
    {
        _dataOperationProcessor = dataOperationProcessor;
        _userResolverService = userResolverService;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<StoredFile>()
            .HasOne(f => f.OwnerUser)
            .WithMany(u => u.StoredFiles)
            .HasForeignKey(f => f.CreatedById);

        builder.Entity<StoredFile>().HasQueryFilter(f =>
            f.CreatedById.HasValue && f.CreatedById.Equals(_userResolverService.GetCurrentUserGuid())
        );

        base.OnModelCreating(builder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            await _dataOperationProcessor.ProcessAsync(entry, cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
