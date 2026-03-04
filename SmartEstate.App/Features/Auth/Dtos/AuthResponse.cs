namespace SmartEstate.App.Features.Auth.Dtos;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    string Token
);
