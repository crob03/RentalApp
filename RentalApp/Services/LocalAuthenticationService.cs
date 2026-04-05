using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Services;

public class LocalAuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _context;
    private readonly ICredentialStore _credentialStore;
    private User? _currentUser;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public bool IsAuthenticated => _currentUser != null;

    public User? CurrentUser => _currentUser;

    public LocalAuthenticationService(AppDbContext context, ICredentialStore credentialStore)
    {
        _context = context;
        _credentialStore = credentialStore;
    }

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
                return AuthenticationResult.Failure("Invalid email or password");

            if (rememberMe)
                await _credentialStore.SaveAsync(email, password);

            _currentUser = user;
            AuthenticationStateChanged?.Invoke(this, true);
            return AuthenticationResult.Success(user);
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure($"Login failed: {ex.Message}");
        }
    }

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
                return AuthenticationResult.Failure("User with this email already exists");

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
            return AuthenticationResult.Failure($"Registration failed: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        await _credentialStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }
}
