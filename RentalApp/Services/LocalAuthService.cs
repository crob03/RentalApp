using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Repositories;
using RentalApp.Http;

namespace RentalApp.Services;

internal class LocalAuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IItemRepository _itemRepository;
    private readonly AuthTokenState _tokenState;

    public LocalAuthService(
        IUserRepository userRepository,
        IItemRepository itemRepository,
        AuthTokenState tokenState
    )
    {
        _userRepository = userRepository;
        _itemRepository = itemRepository;
        _tokenState = tokenState;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
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
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists");

        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var newUser = await _userRepository.CreateAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            BCrypt.Net.BCrypt.HashPassword(request.Password, salt),
            salt
        );

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
        var user =
            await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Authenticated user not found");

        var itemsListed = await _itemRepository.CountItemsByOwnerAsync(userId);

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
        var user =
            await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var itemsListed = await _itemRepository.CountItemsByOwnerAsync(userId);

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
