using Domain;
using Domain.Identity;
using Infrastructure.EF.OperationProcessing;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EF;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public DbSet<StoredFile> StoredFiles { get; set; }

    private readonly IDataOperationProcessor _dataOperationProcessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, IDataOperationProcessor dataOperationProcessor)
        : base(options)
    {
        _dataOperationProcessor = dataOperationProcessor;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<StoredFile>()
            .HasOne(f => f.OwnerUser)
            .WithMany(u => u.StoredFiles)
            .HasForeignKey(f => f.CreatedById);
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
