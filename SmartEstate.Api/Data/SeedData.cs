using SmartEstate.App.Common.Abstractions;
using SmartEstate.Domain.Entities;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Time;
using Microsoft.EntityFrameworkCore;
using SmartEstate.Domain.Enums;

namespace SmartEstate.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("SeedData");
        try
        {
            var ctx = services.GetRequiredService<SmartEstateDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var clock = services.GetRequiredService<IClock>();

            // Apply migrations automatically
            await ctx.Database.MigrateAsync();

            // if there are already users, assume seeded
            if (ctx.Users.Any())
            {
                logger?.LogInformation("Database already contains data - skipping seeding.");
                return;
            }

            logger?.LogInformation("Seeding initial data...");

            // Roles
            if (!ctx.Roles.Any())
            {
                ctx.Roles.AddRange(
                    new Role { Id = 1, Name = "User", Description = "Regular user" },
                    new Role { Id = 2, Name = "Seller", Description = "Seller" },
                    new Role { Id = 3, Name = "Broker", Description = "Broker" },
                    new Role { Id = 4, Name = "Admin", Description = "Administrator" }
                );
                await ctx.SaveChangesAsync();
            }

            // minimal permissions
            if (!ctx.Permissions.Any())
            {
                ctx.Permissions.AddRange(
                    new Permission { Id = 1, Code = "LISTING_CREATE", Description = "Create listing" },
                    new Permission { Id = 2, Code = "MODERATION_REVIEW", Description = "Moderation" },
                    new Permission { Id = 3, Code = "POINT_BUY", Description = "Buy points" }
                );
                await ctx.SaveChangesAsync();
            }

            if (!ctx.RolePermissions.Any())
            {
                ctx.RolePermissions.AddRange(
                    new RolePermission { RoleId = 2, PermissionId = 1 },
                    new RolePermission { RoleId = 3, PermissionId = 1 },
                    new RolePermission { RoleId = 4, PermissionId = 2 },
                    new RolePermission { RoleId = 1, PermissionId = 3 },
                    new RolePermission { RoleId = 2, PermissionId = 3 },
                    new RolePermission { RoleId = 3, PermissionId = 3 }
                );
                await ctx.SaveChangesAsync();
            }

            // Users
            var admin = User.Create("admin@local", "Admin", 4);
            admin.SetPasswordHash(hasher.Hash("Admin123!"));

            var seller = User.Create("seller@local", "Seller One", 2);
            seller.SetPasswordHash(hasher.Hash("Seller123!"));

            var broker = User.Create("broker@local", "Broker Joe", 3);
            broker.SetPasswordHash(hasher.Hash("Broker123!"));

            var user1 = User.Create("user1@local", "User One", 1);
            user1.SetPasswordHash(hasher.Hash("User123!"));
            var user2 = User.Create("user2@local", "User Two", 1);
            user2.SetPasswordHash(hasher.Hash("User123!"));

            ctx.Users.AddRange(admin, seller, broker, user1, user2);
            await ctx.SaveChangesAsync();

            // Broker profile
            var bp = new BrokerProfile
            {
                UserId = broker.Id,
                CompanyName = "Acme Realty",
                LicenseNo = "LIC-001",
                Bio = "Experienced broker serving local market.",
                RatingAvg = 4.7m,
                RatingCount = 42
            };
            ctx.BrokerProfiles.Add(bp);
            await ctx.SaveChangesAsync();

            var listingUpdate = new ListingUpdate(
                Title: "Cozy 2BR apartment near city center",
                Description: "Bright, newly renovated 2-bedroom apartment. Close to transport and shops.",
                PropertyType: PropertyType.Apartment,
                PriceAmount: 1200000000m,
                PriceCurrency: "VND",
                AreaM2: 65.0,
                Bedrooms: 2,
                Bathrooms: 2,
                FullAddress: "123 Nguyen Trai, Ward 5, District 3",
                City: "Hanoi",
                District: "District 3",
                Ward: "Ward 5",
                Street: "Nguyen Trai",
                Lat: 21.0285,
                Lng: 105.8542,
                VirtualTourUrl: null
            );

            var listing = Listing.Create(seller.Id, listingUpdate);
            ctx.Listings.Add(listing);
            await ctx.SaveChangesAsync();
            ctx.ListingImages.Add(new ListingImage
            {
                ListingId = listing.Id,
                Url = "/uploads/sample1.jpg",
                SortOrder = 1,
                Caption = "Living room"
            });
            ctx.ListingImages.Add(new ListingImage
            {
                ListingId = listing.Id,
                Url = "/uploads/sample2.jpg",
                SortOrder = 2,
                Caption = "Kitchen"
            });
            await ctx.SaveChangesAsync();

            // Seed initial points for convenience
            var now = clock.UtcNow;
            var monthKey = now.ToString("yyyy-MM");

            var up1 = UserPoints.Create(user1.Id, monthKey);
            up1.AddPermanent(50);
            ctx.UserPoints.Add(up1);
            ctx.PointLedgerEntries.Add(new PointLedgerEntry
            {
                UserId = user1.Id,
                Delta = 50,
                Reason = "PURCHASE_POINTS",
                RefType = "Seed",
                RefId = null,
                IsMonthlyBucket = false,
                BalanceMonthlyAfter = up1.MonthlyPoints,
                BalancePermanentAfter = up1.PermanentPoints,
                Bucket = "PERMANENT",
                MonthKey = null,
                TxType = "PURCHASE_POINTS",
                Note = "Seed"
            });

            var up2 = UserPoints.Create(user2.Id, monthKey);
            up2.AddPermanent(5);
            ctx.UserPoints.Add(up2);
            ctx.PointLedgerEntries.Add(new PointLedgerEntry
            {
                UserId = user2.Id,
                Delta = 5,
                Reason = "PURCHASE_POINTS",
                RefType = "Seed",
                RefId = null,
                IsMonthlyBucket = false,
                BalanceMonthlyAfter = up2.MonthlyPoints,
                BalancePermanentAfter = up2.PermanentPoints,
                Bucket = "PERMANENT",
                MonthKey = null,
                TxType = "PURCHASE_POINTS",
                Note = "Seed"
            });
            await ctx.SaveChangesAsync();

            if (!ctx.PointPackages.Any())
            {
                ctx.PointPackages.Add(new PointPackage
                {
                    Name = "Starter 30",
                    Points = 30,
                    PriceAmount = 30000m,
                    PriceCurrency = "VND",
                    IsActive = true
                });
                ctx.PointPackages.Add(new PointPackage
                {
                    Name = "Pro 60",
                    Points = 60,
                    PriceAmount = 50000m,
                    PriceCurrency = "VND",
                    IsActive = true
                });
                await ctx.SaveChangesAsync();
            }

            logger?.LogInformation("Seeding completed.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
