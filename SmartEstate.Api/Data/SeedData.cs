using SmartEstate.App.Common.Abstractions;
using SmartEstate.Domain.Entities;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Time;
using Microsoft.EntityFrameworkCore;
using SmartEstate.Domain.Enums;

namespace SmartEstate.Api.Data;

public static class SeedData
{
    private static readonly Random _random = new();

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
            logger?.LogWarning("Resetting database...");
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.MigrateAsync();

            logger?.LogInformation("Seeding initial data...");

            // 1. Roles
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

            // 2. Permissions
            if (!ctx.Permissions.Any())
            {
                ctx.Permissions.AddRange(
                    new Permission { Id = 1, Code = "LISTING_CREATE", Description = "Create listing" },
                    new Permission { Id = 2, Code = "MODERATION_REVIEW", Description = "Moderation" },
                    new Permission { Id = 3, Code = "POINT_BUY", Description = "Buy points" },
                    new Permission { Id = 4, Code = "BROKER_PROFILE", Description = "Manage broker profile" }
                );
                await ctx.SaveChangesAsync();
            }

            // 3. RolePermissions
            if (!ctx.RolePermissions.Any())
            {
                ctx.RolePermissions.AddRange(
                    new RolePermission { RoleId = 2, PermissionId = 1 }, // Seller can create listing
                    new RolePermission { RoleId = 3, PermissionId = 1 }, // Broker can create listing
                    new RolePermission { RoleId = 3, PermissionId = 4 }, // Broker can manage profile
                    new RolePermission { RoleId = 4, PermissionId = 2 }, // Admin can moderate
                    new RolePermission { RoleId = 1, PermissionId = 3 }, // All can buy points
                    new RolePermission { RoleId = 2, PermissionId = 3 },
                    new RolePermission { RoleId = 3, PermissionId = 3 }
                );
                await ctx.SaveChangesAsync();
            }

            // 4. Users
            var users = new List<User>();

            // Admin
            var admin = User.Create("admin@local", "System Admin", 4);
            admin.SetPasswordHash(hasher.Hash("Admin123!"));
            users.Add(admin);

            // Sellers (5)
            for (int i = 1; i <= 5; i++)
            {
                var seller = User.Create($"seller{i}@local", $"Seller {i}", 2);
                seller.SetPasswordHash(hasher.Hash("Seller123!"));
                seller.UpdatePhone($"090100000{i}");
                users.Add(seller);
            }

            // Brokers (5)
            for (int i = 1; i <= 5; i++)
            {
                var broker = User.Create($"broker{i}@local", $"Broker {i}", 3);
                broker.SetPasswordHash(hasher.Hash("Broker123!"));
                broker.UpdatePhone($"090200000{i}");
                users.Add(broker);
            }

            // Regular Users (10)
            for (int i = 1; i <= 10; i++)
            {
                var user = User.Create($"user{i}@local", $"User {i}", 1);
                user.SetPasswordHash(hasher.Hash("User123!"));
                user.UpdatePhone($"090300000{i}");
                users.Add(user);
            }

            ctx.Users.AddRange(users);
            await ctx.SaveChangesAsync();

            // 5. Broker Profiles
            var brokers = users.Where(u => u.RoleId == 3).ToList();
            foreach (var broker in brokers)
            {
                var profile = new BrokerProfile
                {
                    UserId = broker.Id,
                    CompanyName = $"Real Estate Company {_random.Next(1, 100)}",
                    LicenseNo = $"LIC-{_random.Next(1000, 9999)}",
                    Bio = "Experienced broker with over 10 years in the market, specializing in luxury apartments and villas.",
                    RatingAvg = (decimal)(3.5 + (_random.NextDouble() * 1.5)), // 3.5 to 5.0
                    RatingCount = _random.Next(5, 100)
                };
                ctx.BrokerProfiles.Add(profile);
            }
            await ctx.SaveChangesAsync();

            // 6. Listings
            var listingCreators = users.Where(u => u.RoleId == 2 || u.RoleId == 3).ToList(); // Sellers and Brokers
            var listings = new List<Listing>();

            foreach (var creator in listingCreators)
            {
                int listingCount = _random.Next(3, 8); // Each creator makes 3-8 listings
                for (int j = 0; j < listingCount; j++)
                {
                    var type = (PropertyType)_random.Next(0, 7); // Random property type
                    var (city, district, ward, street, lat, lng) = GetRandomLocation();
                    
                    var listingUpdate = new ListingUpdate(
                        Title: GetRandomTitle(type, district),
                        Description: GetRandomDescription(type),
                        PropertyType: type,
                        PriceAmount: (decimal)(_random.Next(10, 500) * 100000000.0), // 1B - 50B VND
                        PriceCurrency: "VND",
                        AreaM2: _random.Next(30, 200),
                        Bedrooms: _random.Next(1, 6),
                        Bathrooms: _random.Next(1, 4),
                        FullAddress: $"{_random.Next(1, 999)} {street}, {ward}, {district}, {city}",
                        City: city,
                        District: district,
                        Ward: ward,
                        Street: street,
                        Lat: lat + (_random.NextDouble() * 0.01 - 0.005), // Slight variation
                        Lng: lng + (_random.NextDouble() * 0.01 - 0.005),
                        VirtualTourUrl: _random.NextDouble() > 0.8 ? "https://my.matterport.com/show/?m=example" : null
                    );

                    var listing = Listing.Create(creator.Id, listingUpdate);

                    // Randomly assign status
                    if (_random.NextDouble() > 0.2)
                    {
                        listing.Approve("Auto-approved by seed");
                        listing.Activate();
                    }
                    else
                    {
                        // Keep as NeedReview
                    }

                    // Assign broker logic (if creator is seller, maybe assign to a broker?)
                    // For now, simple: if creator is broker, they are assigned.
                    if (creator.RoleId == 3)
                    {
                        listing.AssignBroker(creator.Id);
                    }

                    listings.Add(listing);
                    ctx.Listings.Add(listing);
                }
            }
            await ctx.SaveChangesAsync();

            // 7. Listing Images
            foreach (var listing in listings)
            {
                int imageCount = _random.Next(2, 6);
                for (int k = 1; k <= imageCount; k++)
                {
                    ctx.ListingImages.Add(new ListingImage
                    {
                        ListingId = listing.Id,
                        Url = $"https://picsum.photos/seed/{listing.Id}_{k}/800/600", // Random placeholder image
                        SortOrder = k,
                        Caption = k == 1 ? "Main View" : (k == 2 ? "Living Room" : "Bedroom")
                    });
                }
            }
            await ctx.SaveChangesAsync();

            // 8. User Points & Ledger
            var now = clock.UtcNow;
            var monthKey = now.ToString("yyyy-MM");

            foreach (var user in users)
            {
                var up = UserPoints.Create(user.Id, monthKey);
                int initialPoints = _random.Next(0, 100);
                if (initialPoints > 0)
                {
                    up.AddPermanent(initialPoints);
                    ctx.PointLedgerEntries.Add(new PointLedgerEntry
                    {
                        UserId = user.Id,
                        Delta = initialPoints,
                        Reason = "SEED_BONUS",
                        RefType = "Seed",
                        RefId = null,
                        IsMonthlyBucket = false,
                        BalanceMonthlyAfter = up.MonthlyPoints,
                        BalancePermanentAfter = up.PermanentPoints,
                        Bucket = "PERMANENT",
                        MonthKey = null,
                        TxType = "BONUS",
                        Note = "Welcome bonus"
                    });
                }
                ctx.UserPoints.Add(up);
            }
            await ctx.SaveChangesAsync();

            // 9. Point Packages
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
                 ctx.PointPackages.Add(new PointPackage
                {
                    Name = "Business 200",
                    Points = 200,
                    PriceAmount = 150000m,
                    PriceCurrency = "VND",
                    IsActive = true
                });
                await ctx.SaveChangesAsync();
            }

            logger?.LogInformation($"Seeding completed. Created {users.Count} users, {listings.Count} listings.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static (string City, string District, string Ward, string Street, double Lat, double Lng) GetRandomLocation()
    {
        var locations = new[]
        {
            ("Hanoi", "Hoan Kiem", "Hang Bai", "Trang Tien", 21.0255, 105.8550),
            ("Hanoi", "Ba Dinh", "Ngoc Ha", "Doi Can", 21.0360, 105.8285),
            ("Hanoi", "Cau Giay", "Dich Vong", "Xuan Thuy", 21.0365, 105.7830),
            ("Hanoi", "Tay Ho", "Quang An", "Xuan Dieu", 21.0650, 105.8200),
            ("Ho Chi Minh", "District 1", "Ben Nghe", "Nguyen Hue", 10.7760, 106.7010),
            ("Ho Chi Minh", "District 3", "Vo Thi Sau", "Pasteur", 10.7850, 106.6900),
            ("Ho Chi Minh", "Binh Thanh", "Ward 22", "Nguyen Huu Canh", 10.7950, 106.7200),
            ("Da Nang", "Hai Chau", "Thach Thang", "Bach Dang", 16.0750, 108.2230)
        };
        return locations[_random.Next(locations.Length)];
    }

    private static string GetRandomTitle(PropertyType type, string district)
    {
        var adjectives = new[] { "Beautiful", "Luxury", "Modern", "Cozy", "Spacious", "Prime", "Affordable", "Sunny" };
        var typeNames = new Dictionary<PropertyType, string>
        {
            { PropertyType.Apartment, "Apartment" },
            { PropertyType.House, "House" },
            { PropertyType.Townhouse, "Townhouse" },
            { PropertyType.Land, "Plot of Land" },
            { PropertyType.Office, "Office Space" },
            { PropertyType.Shophouse, "Shophouse" },
            { PropertyType.Warehouse, "Warehouse" },
            { PropertyType.Other, "Property" }
        };

        return $"{adjectives[_random.Next(adjectives.Length)]} {typeNames[type]} in {district}";
    }

    private static string GetRandomDescription(PropertyType type)
    {
        var descriptions = new[]
        {
            "Located in a prime area, this property offers great potential for investment or living. Close to schools, markets, and hospitals.",
            "Newly renovated with high-quality furniture. Ready to move in immediately. Great view and peaceful neighborhood.",
            "Spacious and bright, designed with modern architecture. Features a large garden and private parking.",
            "Perfect choice for young families or professionals. Convenient transportation and secure area.",
            "Exclusive offer! Don't miss this opportunity to own a premium property at a competitive price."
        };

        return descriptions[_random.Next(descriptions.Length)];
    }
}
