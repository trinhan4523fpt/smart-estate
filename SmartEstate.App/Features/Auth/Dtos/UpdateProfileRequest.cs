namespace SmartEstate.App.Features.Auth.Dtos;

public sealed record UpdateProfileRequest(
    string? DisplayName,
    string? Phone,
    string? Address,
    string? Avatar,
    string? CurrentPassword,
    string? NewPassword
);
