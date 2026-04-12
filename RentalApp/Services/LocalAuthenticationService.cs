using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Services;

/// <summary>
/// Implements <see cref="IAuthenticationService"/> by authenticating directly against the local
/// PostgreSQL database via <see cref="AppDbContext"/>. Passwords are verified using BCrypt.
/// </summary>
public class LocalAuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _context;
    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<LocalAuthenticationService> _logger;
    private User? _currentUser;

    /// <inheritdoc/>
    public event EventHandler<bool>? AuthenticationStateChanged;

    /// <inheritdoc/>
    public bool IsAuthenticated => _currentUser != null;

    /// <inheritdoc/>
    public User? CurrentUser => _currentUser;

    /// <summary>
    /// Initialises a new instance of <see cref="LocalAuthenticationService"/>.
    /// </summary>
    /// <param name="context">The database context used to query users.</param>
    /// <param name="credentialStore">The credential store used to persist credentials when remember-me is enabled.</param>
    /// <param name="logger">The logger for this service.</param>
    public LocalAuthenticationService(
        AppDbContext context,
        ICredentialStore credentialStore,
        ILogger<LocalAuthenticationService> logger
    )
    {
        _context = context;
        _credentialStore = credentialStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Looks up the user by email and verifies the supplied password against the stored BCrypt hash.
    /// </remarks>
    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe = false
    )
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for {Email}: invalid email or password", email);
                return AuthenticationResult.Failure("Invalid email or password");
            }

            if (rememberMe)
                await _credentialStore.SaveAsync(email, password);

            _currentUser = user;
            AuthenticationStateChanged?.Invoke(this, true);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", email);
            return AuthenticationResult.Failure($"Login failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Checks for an existing account with the same email before creating the new user.
    /// The password is salted and hashed with BCrypt before being stored.
    /// </remarks>
    public async Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
    {
        try
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed for {Email}: email already in use", email);
                return AuthenticationResult.Failure("User with this email already exists");
            }

            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", email);
            return AuthenticationResult.Failure($"Registration failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync()
    {
        _currentUser = null;
        await _credentialStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }
}
