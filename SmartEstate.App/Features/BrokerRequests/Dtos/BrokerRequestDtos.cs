namespace SmartEstate.App.Features.BrokerRequests.Dtos;

public sealed record RespondBrokerRequestRequest(string Status);
public sealed record CreateBrokerRequestPayload(Guid ListingId, Guid BrokerId);
