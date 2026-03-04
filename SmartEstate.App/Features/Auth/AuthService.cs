using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Auth.Dtos;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.Auth;

public sealed class AuthService
{
    private readonly SmartEstateDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;

    public AuthService(
        SmartEstateDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(x => x.Email == email && !x.IsDeleted, ct);
        if (exists)
            return Result<AuthResponse>.Fail(ErrorCodes.Conflict, "Email already exists.");

        var user = User.Create(email, req.DisplayName, UserRole.User);

        // domain set password hash
        user.SetPasswordHash(_hasher.Hash(req.Password));

        _db.Users.Add(user);
        await _db.SaveChangesAsync(true, ct);

        var roleName = user.Role.ToString();
        var token = _jwt.CreateToken(user.Id, user.Email, roleName);
        return Result<AuthResponse>.Ok(new AuthResponse(user.Id, user.Email, user.DisplayName, roleName, token));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Where(x => x.Email == email && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<AuthResponse>.Fail(ErrorCodes.Unauthorized, "Invalid credentials.");

        // domain checks
        try
        {
            user.EnsureCanLogin();
        }
        catch
        {
            return Result<AuthResponse>.Fail(ErrorCodes.Unauthorized, "Invalid credentials.");
        }

        // verify password
        if (!_hasher.Verify(req.Password, user.PasswordHash))
            return Result<AuthResponse>.Fail(ErrorCodes.Unauthorized, "Invalid credentials.");

        // domain update
        user.MarkLoggedIn(_clock.UtcNow);

        await _db.SaveChangesAsync(true, ct);

        var roleName = user.Role.ToString();
        var token = _jwt.CreateToken(user.Id, user.Email, roleName);
        return Result<AuthResponse>.Ok(new AuthResponse(user.Id, user.Email, user.DisplayName, roleName, token));
    }


    public async Task<Result<ProfileResponse>> GetMyProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);

        if (user is null)
            return Result<ProfileResponse>.Fail(ErrorCodes.NotFound, "User not found.");

        return Result<ProfileResponse>.Ok(ToProfile(user));
    }

    public async Task<Result<ProfileResponse>> UpdateMyProfileAsync(Guid userId, UpdateProfileRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);

        if (user is null)
            return Result<ProfileResponse>.Fail(ErrorCodes.NotFound, "User not found.");

        // update basic profile via domain methods
        user.UpdateProfile(req.DisplayName, req.Phone, req.Address, req.Avatar);

        // optional password change
        var wantsChangePassword =
            !string.IsNullOrWhiteSpace(req.CurrentPassword) ||
            !string.IsNullOrWhiteSpace(req.NewPassword);

        if (wantsChangePassword)
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Result<ProfileResponse>.Fail(ErrorCodes.Validation, "CurrentPassword and NewPassword are required.");

            if (!_hasher.Verify(req.CurrentPassword, user.PasswordHash))
                return Result<ProfileResponse>.Fail(ErrorCodes.Unauthorized, "Current password is incorrect.");

            if (req.NewPassword!.Length < 8)
                return Result<ProfileResponse>.Fail(ErrorCodes.Validation, "New password must be at least 8 characters.");

            user.SetPasswordHash(_hasher.Hash(req.NewPassword));
        }

        await _db.SaveChangesAsync(true, ct);

        return Result<ProfileResponse>.Ok(ToProfile(user));
    }

    public async Task<Result<List<ProfileResponse>>> GetUsersAsync(string? role, CancellationToken ct = default)
    {
        var q = _db.Users.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive);
        
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r))
        {
            q = q.Where(x => x.Role == r);
        }

        var users = await q.ToListAsync(ct);
        return Result<List<ProfileResponse>>.Ok(users.Select(ToProfile).ToList());
    }

    private ProfileResponse ToProfile(User u)
    {
        return new ProfileResponse(
            u.Id,
            u.Email,
            u.DisplayName,
            u.Phone,
            u.Address,
            u.Avatar,
            u.Role.ToString(),
            u.IsActive,
            u.LastLoginAt
        );
    }
}
