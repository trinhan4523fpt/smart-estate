using SmartEstate.App.Common.Abstractions;

namespace SmartEstate.Api.Integrations;

public sealed class VnPayGateway : IPaymentGateway
{
    public Task<PaymentInitResult> CreatePaymentAsync(Guid payerUserId, decimal amount, string currency, string description, CancellationToken ct = default)
    {
        var provider = "VNPAY";
        var providerRef = Guid.NewGuid().ToString("N");
        var payUrl = $"/mock/vnpay/{providerRef}";
        return Task.FromResult(new PaymentInitResult(provider, providerRef, payUrl));
    }
}
