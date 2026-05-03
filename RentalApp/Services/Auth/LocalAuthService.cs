using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Repositories;

namespace RentalApp.Services.Auth;

/// <summary>
/// Repository-backed implementation of <see cref="IAuthService"/> for local/offline development.
/// Authenticates against the local database using BCrypt password verification.
/// </summary>
internal class LocalAuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IItemRepository _itemRepository;
    private readonly AuthTokenState _tokenState;
    private CurrentUserResponse? _currentUserCache;

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

    /// <inheritdoc/>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        _currentUserCache = null;
        return new LoginResponse(
            Token: user.Id.ToString(),
            ExpiresAt: DateTime.MaxValue,
            UserId: user.Id
        );
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        if (_currentUserCache is not null)
            return _currentUserCache;

        if (!_tokenState.HasSession)
            throw new InvalidOperationException("No user is currently authenticated");

        var userId = int.Parse(_tokenState.CurrentToken!);
        var user =
            await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Authenticated user not found");

        var itemsListed = await _itemRepository.CountItemsByOwnerAsync(userId);

        _currentUserCache = new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            AverageRating: null,
            ItemsListed: itemsListed,
            RentalsCompleted: 0,
            user.CreatedAt ?? DateTime.UtcNow
        );
        return _currentUserCache;
    }

    /// <inheritdoc/>
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
