using SmartEstate.App.Common.Abstractions;
using SmartEstate.Domain.Entities;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Time;
using Microsoft.EntityFrameworkCore;
using SmartEstate.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace SmartEstate.Api.Data;

public static class SeedData
{
    private static readonly Dictionary<string, Guid> _idMap = new();

    private static Guid GetGuid(string originalId)
    {
        if (_idMap.TryGetValue(originalId, out var guid)) return guid;
        
        // Deterministic GUID from string
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(originalId));
        guid = new Guid(hash);
        _idMap[originalId] = guid;
        return guid;
    }

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

            logger?.LogWarning("Resetting database...");
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.MigrateAsync();

            logger?.LogInformation("Seeding initial data...");

            // 1. Users
            if (!ctx.Users.Any())
            {
                var users = new List<User>();

                // Helper to create user
                User CreateUser(string id, string email, string name, UserRole role, string phone, string address, string avatar = null)
                {
                    var u = User.Create(email, name, role);
                    u.SetPasswordHash(hasher.Hash("Password123!")); // Default password
                    u.UpdateProfile(name, phone, address, avatar);
                    
                    // Force specific ID if possible, but EF Core might override if not careful. 
                    // However, we are adding new entities. 
                    // We can't easily set ID on creation as it's protected set in BaseEntity.
                    // But we can use reflection or just let EF generate and we map it?
                    // Actually, BaseEntity<TKey> has protected set. 
                    // Let's rely on our deterministic GUIDs and set them via reflection or just create new Guids and map.
                    // Better: Use reflection to set ID to match our deterministic GetGuid(id).
                    
                    var guid = GetGuid(id);
                    typeof(SmartEstate.Domain.Common.BaseEntity<Guid>).GetProperty("Id")!.SetValue(u, guid);
                    
                    return u;
                }

                users.Add(CreateUser("user-admin-1", "admin@smartestate.vn", "Admin SmartEstate", UserRole.Admin, "0901000001", "Hà Nội"));
                users.Add(CreateUser("user-broker-1", "broker1@smartestate.vn", "Nguyễn Văn Broker", UserRole.Broker, "0912345678", "TP. Hồ Chí Minh"));
                users.Add(CreateUser("user-broker-2", "broker2@smartestate.vn", "Trần Thị Môi Giới", UserRole.Broker, "0923456789", "Hà Nội"));
                users.Add(CreateUser("user-seller-1", "seller1@smartestate.vn", "Lê Hoàng Seller", UserRole.Seller, "0934567890", "Đà Nẵng"));
                users.Add(CreateUser("user-seller-2", "seller2@smartestate.vn", "Phạm Thị Hoa", UserRole.Seller, "0945678901", "TP. Hồ Chí Minh"));
                users.Add(CreateUser("user-buyer-1", "buyer1@smartestate.vn", "Hoàng Minh Tuấn", UserRole.User, "0956789012", "Hà Nội"));

                ctx.Users.AddRange(users);
                await ctx.SaveChangesAsync();

                // 2. Listings
                var listings = new List<Listing>();

                void AddListing(
                    string id, 
                    string createdById, 
                    string responsibleId, 
                    string title, 
                    PropertyType type, 
                    TransactionType trans, 
                    decimal price, 
                    double area, 
                    string city, 
                    string district, 
                    string address, 
                    string desc, 
                    double lat, 
                    double lng, 
                    string[] images,
                    ModerationStatus modStatus,
                    ListingLifecycleStatus lifeStatus,
                    int? beds = null,
                    int? baths = null,
                    string tourUrl = null,
                    string sellerName = null,
                    string sellerPhone = null,
                    bool isBrokerManaged = false,
                    DateTimeOffset? approvedAt = null,
                    DateTimeOffset? completedAt = null,
                    string modDecision = null,
                    int modScore = 0,
                    string[] modFlags = null,
                    string modReason = null
                )
                {
                    var creatorId = GetGuid(createdById);
                    
                    var update = new ListingUpdate(
                        Title: title,
                        Description: desc,
                        PropertyType: type,
                        TransactionType: trans,
                        Price: price,
                        AreaM2: area,
                        Bedrooms: beds,
                        Bathrooms: baths,
                        City: city,
                        District: district,
                        Address: address,
                        Lat: (decimal)lat,
                        Lng: (decimal)lng,
                        VirtualTourUrl: tourUrl,
                        SellerName: sellerName,
                        SellerPhone: sellerPhone
                    );

                    var l = Listing.Create(creatorId, update);
                    
                    // Set ID
                    typeof(SmartEstate.Domain.Common.BaseEntity<Guid>).GetProperty("Id")!.SetValue(l, GetGuid(id));

                    // Override status
                    if (modStatus == ModerationStatus.Approved) l.Approve(null, approvedAt);
                    else if (modStatus == ModerationStatus.Rejected) l.Reject(modReason ?? "Rejected");
                    else l.NeedReview(modReason);

                    l.SetLifecycle(lifeStatus);
                    if (lifeStatus == ListingLifecycleStatus.Done && completedAt.HasValue) l.MarkDone(completedAt);

                    // Assignment
                    if (isBrokerManaged)
                    {
                        var brokerId = GetGuid(responsibleId);
                        l.AssignBroker(brokerId);
                    }
                    else
                    {
                        // Ensure responsible is set correctly if different from creator (though usually same if not broker managed)
                        // In mock data: responsibleUserId is sometimes set.
                        var respId = GetGuid(responsibleId);
                        l.ResponsibleUserId = respId;
                    }

                    // Images
                    int sort = 1;
                    foreach (var img in images)
                    {
                        l.Images.Add(new ListingImage
                        {
                            ListingId = l.Id,
                            Url = img,
                            SortOrder = sort++,
                            Caption = $"Image {sort}"
                        });
                    }

                    // Moderation Report (if any)
                    if (modDecision != null)
                    {
                        var report = ModerationReport.CreateFromAiDecision(
                            l.Id,
                            modScore,
                            modDecision,
                            modReason,
                            modFlags != null ? System.Text.Json.JsonSerializer.Serialize(modFlags) : null
                        );
                        report.MarkReviewed(GetGuid("user-admin-1"), DateTimeOffset.UtcNow, modStatus);
                        ctx.ModerationReports.Add(report);
                    }

                    listings.Add(l);
                    ctx.Listings.Add(l);
                }

                // listing-1
                AddListing("listing-1", "user-seller-1", "user-seller-1", 
                    "Căn hộ cao cấp 2PN tại Quận 1, view sông Sài Gòn", 
                    PropertyType.Apartment, TransactionType.Buy, 4500000000m, 85, 
                    "TP. Hồ Chí Minh", "Quận 1", "123 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
                    "Căn hộ cao cấp tọa lạc ngay trung tâm Quận 1...", 
                    10.7769, 106.7009, 
                    new[] { "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800", "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800", "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800" },
                    ModerationStatus.Approved, ListingLifecycleStatus.Active, 
                    2, 2, null, "Lê Hoàng Seller", "0934567890", false, DateTimeOffset.Parse("2024-03-02T10:00:00Z"));

                // listing-2
                AddListing("listing-2", "user-seller-1", "user-broker-1", 
                    "Nhà phố 4 tầng mặt tiền đường lớn Đà Nẵng", 
                    PropertyType.House, TransactionType.Buy, 7800000000m, 120, 
                    "Đà Nẵng", "Hải Châu", "45 Nguyễn Văn Linh, Hải Châu, Đà Nẵng",
                    "Nhà phố 4 tầng mặt tiền đường lớn...", 
                    16.0544, 108.2022, 
                    new[] { "https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=800", "https://images.unsplash.com/photo-1568605114967-8130f3a36994?w=800" },
                    ModerationStatus.Approved, ListingLifecycleStatus.Active, 
                    4, 3, "https://my.matterport.com/show/?m=SxQL3iGyoDo", "Lê Hoàng Seller", "0934567890", true, DateTimeOffset.Parse("2024-03-06T09:00:00Z"));

                // listing-3
                AddListing("listing-3", "user-seller-2", "user-seller-2", 
                    "Đất nền khu đô thị mới Hà Nội, sổ đỏ trao tay", 
                    PropertyType.Land, TransactionType.Buy, 3200000000m, 200, 
                    "Hà Nội", "Hoài Đức", "Khu đô thị Vinhomes Smart City, Hoài Đức, Hà Nội",
                    "Lô đất nền đẹp tại khu đô thị mới hiện đại...", 
                    21.0245, 105.7412, 
                    new[] { "https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=800", "https://images.unsplash.com/photo-1455587734955-081b22074882?w=800" },
                    ModerationStatus.Approved, ListingLifecycleStatus.Active, 
                    0, 0, null, "Phạm Thị Hoa", "0945678901", false, DateTimeOffset.Parse("2024-03-11T08:00:00Z"));

                // listing-4
                AddListing("listing-4", "user-seller-2", "user-seller-2", 
                    "Văn phòng cho thuê hạng A tại Cầu Giấy, Hà Nội", 
                    PropertyType.Office, TransactionType.Rent, 45000000m, 150, 
                    "Hà Nội", "Cầu Giấy", "Tòa nhà Keangnam, Cầu Giấy, Hà Nội",
                    "Văn phòng hạng A tại tòa nhà Keangnam...", 
                    21.0122, 105.7872, 
                    new[] { "https://images.unsplash.com/photo-1497366216548-37526070297c?w=800", "https://images.unsplash.com/photo-1497366811353-6870744d04b2?w=800", "https://images.unsplash.com/photo-1524758631624-e2822e304c36?w=800" },
                    ModerationStatus.Approved, ListingLifecycleStatus.Active, 
                    0, 0, null, "Phạm Thị Hoa", "0945678901", false, DateTimeOffset.Parse("2024-03-13T09:00:00Z"));

                // listing-5
                AddListing("listing-5", "user-seller-1", "user-seller-1", 
                    "Căn hộ studio giá rẻ cho thuê gần ĐH Bách Khoa HCM", 
                    PropertyType.Apartment, TransactionType.Rent, 8000000m, 35, 
                    "TP. Hồ Chí Minh", "Quận 10", "28/5 Nguyễn Gia Trí, Quận 10, TP. Hồ Chí Minh",
                    "Căn hộ studio hiện đại...", 
                    10.7721, 106.6583, 
                    new[] { "https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=800" },
                    ModerationStatus.Approved, ListingLifecycleStatus.Active, 
                    1, 1, null, "Lê Hoàng Seller", "0934567890", false, DateTimeOffset.Parse("2024-03-16T08:00:00Z"));

                // listing-6
                AddListing("listing-6", "user-seller-2", "user-seller-2", 
                    "Nhà vườn biệt thự sân rộng tại Quận 9", 
                    PropertyType.House, TransactionType.Rent, 25000000m, 300, 
                    "TP. Hồ Chí Minh", "Quận 9", "15 Đường số 8, Khu dân cư Khang An, Quận 9, TP. Hồ Chí Minh",
                    "Biệt thự vườn rộng thoáng mát...", 
                    10.8415, 106.7852, 
                    new[] { "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=800", "https://images.unsplash.com/photo-1580587771525-78b9dba3b914?w=800" },
                    ModerationStatus.Approved, ListingLifecycleStatus.Active, 
                    5, 4, null, "Phạm Thị Hoa", "0945678901", false, DateTimeOffset.Parse("2024-03-19T10:00:00Z"));

                // listing-7
                AddListing("listing-7", "user-seller-1", "user-seller-1", 
                    "Căn hộ 3 phòng ngủ chờ duyệt", 
                    PropertyType.Apartment, TransactionType.Buy, 3800000000m, 95, 
                    "Hà Nội", "Hoàng Mai", "Khu đô thị Times City, Hoàng Mai, Hà Nội",
                    "Căn hộ 3 phòng ngủ tầng cao...", 
                    20.9924, 105.8697, 
                    new[] { "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=800" },
                    ModerationStatus.PendingReview, ListingLifecycleStatus.Active, 
                    3, 2, null, "Lê Hoàng Seller", "0934567890", false, null, null,
                    "NEED_REVIEW", 25, new[] { "too_short_description" }, "Thêm thông tin về tiện ích xung quanh");

                // listing-8
                AddListing("listing-8", "user-seller-2", "user-seller-2", 
                    "Nhà riêng đã bán", 
                    PropertyType.House, TransactionType.Buy, 2500000000m, 60, 
                    "TP. Hồ Chí Minh", "Bình Thạnh", "90 Xô Viết Nghệ Tĩnh, Bình Thạnh, TP. Hồ Chí Minh",
                    "Nhà riêng 3 tầng, sổ hồng chính chủ...", 
                    10.8142, 106.7138, 
                    new[] { "https://images.unsplash.com/photo-1554995207-c18c203602cb?w=800" },
                    ModerationStatus.Approved, ListingLifecycleStatus.Done, 
                    3, 2, null, "Phạm Thị Hoa", "0945678901", false, DateTimeOffset.Parse("2024-01-11T09:00:00Z"), DateTimeOffset.Parse("2024-03-01T00:00:00Z"));

                // listing-9
                AddListing("listing-9", "user-seller-1", "user-seller-1", 
                    "Bán gấp nhà", 
                    PropertyType.House, TransactionType.Buy, 0m, 40, 
                    "Cần Thơ", "Ninh Kiều", "Ninh Kiều, Cần Thơ",
                    "Bán", 
                    0, 0, 
                    Array.Empty<string>(),
                    ModerationStatus.Rejected, ListingLifecycleStatus.Cancelled, 
                    0, 0, null, "Lê Hoàng Seller", "0934567890", false, null, null,
                    "AUTO_REJECT", 70, new[] { "too_short_description", "suspicious_price", "missing_required_fields" }, "Bổ sung mô tả chi tiết");

                await ctx.SaveChangesAsync();

                // 3. Broker Requests
                void AddBrokerRequest(string id, string listingId, string sellerId, string brokerId, TakeoverStatus status, decimal fee, FeeStatus feeStatus)
                {
                    var req = BrokerRequest.Create(GetGuid(listingId), GetGuid(sellerId), GetGuid(brokerId), fee);
                    typeof(SmartEstate.Domain.Common.BaseEntity<Guid>).GetProperty("Id")!.SetValue(req, GetGuid(id));
                    
                    if (status == TakeoverStatus.Accepted) req.Accept(DateTimeOffset.UtcNow);
                    else if (status == TakeoverStatus.Rejected) req.Reject(DateTimeOffset.UtcNow);
                    
                    if (feeStatus == FeeStatus.Paid) req.ConfirmPayment(GetGuid(sellerId), DateTimeOffset.UtcNow);

                    ctx.BrokerRequests.Add(req);
                }

                AddBrokerRequest("br-1", "listing-2", "user-seller-1", "user-broker-1", TakeoverStatus.Accepted, 500000, FeeStatus.Paid);
                AddBrokerRequest("br-2", "listing-3", "user-seller-2", "user-broker-1", TakeoverStatus.Pending, 500000, FeeStatus.Unpaid);
                AddBrokerRequest("br-3", "listing-5", "user-seller-1", "user-broker-2", TakeoverStatus.Rejected, 500000, FeeStatus.Unpaid);

                await ctx.SaveChangesAsync();

                // 4. Conversations & Messages
                void AddConversation(string id, string listingId, string buyerId, string respId, string lastMsg, DateTimeOffset lastMsgAt, int unread, List<(string mid, string uid, string content, DateTimeOffset at)> msgs)
                {
                    var conv = Conversation.Create(GetGuid(listingId), GetGuid(buyerId), GetGuid(respId));
                    typeof(SmartEstate.Domain.Common.BaseEntity<Guid>).GetProperty("Id")!.SetValue(conv, GetGuid(id));
                    
                    conv.UpdateLastMessage(lastMsg, lastMsgAt);
                    if (unread == 0) conv.MarkRead(GetGuid(respId)); // Assume buyer read? Or resp read. Simplification.

                    ctx.Conversations.Add(conv);

                    foreach (var m in msgs)
                    {
                        var msg = Message.Create(conv.Id, GetGuid(m.uid), m.content);
                        typeof(SmartEstate.Domain.Common.BaseEntity<Guid>).GetProperty("Id")!.SetValue(msg, GetGuid(m.mid));
                        msg.CreatedAt = m.at; // Override created at
                        ctx.Messages.Add(msg);
                    }
                }

                AddConversation("conv-1", "listing-1", "user-buyer-1", "user-seller-1", "Anh ơi, căn hộ này còn trống không ạ?", DateTimeOffset.Parse("2024-03-20T14:00:00Z"), 1,
                    new List<(string, string, string, DateTimeOffset)> {
                        ("msg-1", "user-buyer-1", "Anh ơi, căn hộ này còn trống không ạ?", DateTimeOffset.Parse("2024-03-20T14:00:00Z"))
                    });

                AddConversation("conv-2", "listing-2", "user-buyer-1", "user-broker-1", "Được bạn ơi, thứ 7 hoặc Chủ nhật đều được nhé!", DateTimeOffset.Parse("2024-03-19T10:00:00Z"), 0,
                    new List<(string, string, string, DateTimeOffset)> {
                        ("msg-2", "user-buyer-1", "Tôi có thể xem nhà vào cuối tuần không?", DateTimeOffset.Parse("2024-03-19T09:05:00Z")),
                        ("msg-3", "user-broker-1", "Được bạn ơi, thứ 7 hoặc Chủ nhật đều được nhé!", DateTimeOffset.Parse("2024-03-19T10:00:00Z"))
                    });

                await ctx.SaveChangesAsync();

                // 5. Payments (Optional but good)
                // We'll skip detailed payment seeding as it involves more complex logic usually, 
                // but we can add basic records if needed. For now, we skip to save time as most logic relies on other entities.

                // 6. Point Packages
                if (!ctx.PointPackages.Any())
                {
                    ctx.PointPackages.AddRange(
                        new PointPackage { Name = "Starter 30", Points = 30, PriceAmount = 30000m, PriceCurrency = "VND", IsActive = true },
                        new PointPackage { Name = "Pro 60", Points = 60, PriceAmount = 50000m, PriceCurrency = "VND", IsActive = true },
                        new PointPackage { Name = "Business 200", Points = 200, PriceAmount = 150000m, PriceCurrency = "VND", IsActive = true }
                    );
                    await ctx.SaveChangesAsync();
                }

                // 7. Initial Points
                var now = clock.UtcNow;
                var monthKey = now.ToString("yyyy-MM");

                foreach (var user in users)
                {
                    var up = UserPoints.Create(user.Id, monthKey);
                    up.AddPermanent(100); // Give everyone 100 points
                    ctx.UserPoints.Add(up);
                    ctx.PointLedgerEntries.Add(new PointLedgerEntry
                    {
                        UserId = user.Id,
                        Delta = 100,
                        Reason = "SEED_BONUS",
                        RefType = RefType.Subscription.ToString(),
                        BalancePermanentAfter = 100,
                        Bucket = "PERMANENT",
                        TxType = "BONUS"
                    });
                }
                await ctx.SaveChangesAsync();

                logger?.LogInformation($"Seeding completed. Users: {users.Count}, Listings: {listings.Count}.");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
