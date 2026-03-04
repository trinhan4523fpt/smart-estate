using Microsoft.Extensions.DependencyInjection;
using SmartEstate.App.Features.Auth;
using SmartEstate.App.Features.BrokerRequests;
using SmartEstate.App.Features.Favorites;
using SmartEstate.App.Features.Listings;
using SmartEstate.App.Features.Messages;
using SmartEstate.App.Features.Search;
using SmartEstate.App.Features.Moderation;
using SmartEstate.App.Features.Points;
using SmartEstate.App.Features.BrokerApplications;
using SmartEstate.App.Features.ListingBoosts;
using SmartEstate.App.Features.Reports;

namespace SmartEstate.App;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartEstateApp(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ListingService>();
        services.AddScoped<FavoritesService>();
        services.AddScoped<SearchService>();
        services.AddScoped<MessageService>();
        services.AddScoped<BrokerRequestService>();
        services.AddScoped<ModerationService>();
        services.AddScoped<PointsService>();
        services.AddScoped<PointTransactionService>();
        services.AddScoped<BrokerApplicationService>();
        services.AddScoped<ListingBoostService>();
        services.AddScoped<PaymentReportingService>();
        return services;
    }
}
