using Microsoft.EntityFrameworkCore;
using SmartEstate.Domain.Common;
using SmartEstate.Domain.Entities;
using SmartEstate.Shared.Time;
using System.Reflection;

namespace SmartEstate.Infrastructure.Persistence;

public class SmartEstateDbContext : DbContext
{
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    public SmartEstateDbContext(
        DbContextOptions<SmartEstateDbContext> options,
        IClock clock,
        ICurrentUser currentUser
    ) : base(options)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<BrokerProfile> BrokerProfiles => Set<BrokerProfile>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<ListingReport> ListingReports => Set<ListingReport>();
    public DbSet<ModerationReport> ModerationReports => Set<ModerationReport>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<TakeoverRequest> TakeoverRequests => Set<TakeoverRequest>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserListingFavorite> UserListingFavorites => Set<UserListingFavorite>();
    public DbSet<UserPoints> UserPoints => Set<UserPoints>();
    public DbSet<PointLedgerEntry> PointLedgerEntries => Set<PointLedgerEntry>();
    public DbSet<PointPackage> PointPackages => Set<PointPackage>();
    public DbSet<PointPurchase> PointPurchases => Set<PointPurchase>();
    public DbSet<BrokerApplication> BrokerApplications => Set<BrokerApplication>();
    public DbSet<ListingBoost> ListingBoosts => Set<ListingBoost>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartEstateDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SmartEstateDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder)
    where TEntity : AuditableEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
    private void ApplyAudit()
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId ?? Guid.Empty;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = userId;
                    break;
            }
        }
    }

}
