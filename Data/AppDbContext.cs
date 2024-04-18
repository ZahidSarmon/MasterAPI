using Master.Data.Configurations;
using Master.Domain;
using Master.Entities;
using Microsoft.EntityFrameworkCore;

namespace Master.Data;

public class AppDbContext : DbContext
{
    public DbSet<Page> Pages { get; set; }
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
    {
        
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var entity in ChangeTracker.Entries<IAudit>())
        {
            if (entity.State is EntityState.Added or EntityState.Modified)
            {
                if (entity.State == EntityState.Modified)
                {
                    entity.Entity.SetModifiedOn(DateTime.UtcNow);
                }

                if (entity.State == EntityState.Added)
                {
                    entity.Entity.SetCreatedOn(DateTime.UtcNow);
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new PageConfiguration().Configure(modelBuilder.Entity<Page>());
    }
}
