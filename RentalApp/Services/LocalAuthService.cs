using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Data;
using RentalApp.Http;
using DbUser = RentalApp.Database.Models.User;

namespace RentalApp.Services;

internal class LocalAuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState;

    public LocalAuthService(
        IDbContextFactory<AppDbContext> contextFactory,
        AuthTokenState tokenState
    )
    {
        _contextFactory = contextFactory;
        _tokenState = tokenState;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        await using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        return new LoginResponse(
            Token: user.Id.ToString(),
            ExpiresAt: DateTime.MaxValue,
            UserId: user.Id
        );
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists");

        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var newUser = new DbUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt),
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        return new RegisterResponse(
            newUser.Id,
            newUser.Email,
            newUser.FirstName,
            newUser.LastName,
            newUser.CreatedAt ?? DateTime.UtcNow
        );
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        if (!_tokenState.HasSession)
            throw new InvalidOperationException("No user is currently authenticated");

        var userId = int.Parse(_tokenState.CurrentToken!);
        await using var context = _contextFactory.CreateDbContext();
        var user =
            await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Authenticated user not found");

        var itemsListed = await context.Items.CountAsync(i => i.OwnerId == userId);

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            AverageRating: null,
            ItemsListed: itemsListed,
            RentalsCompleted: 0,
            user.CreatedAt ?? DateTime.UtcNow
        );
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var user =
            await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var itemsListed = await context.Items.CountAsync(i => i.OwnerId == userId);

        return new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            AverageRating: null,
            ItemsListed: itemsListed,
            RentalsCompleted: 0,
            Reviews: []
        );
    }
}
