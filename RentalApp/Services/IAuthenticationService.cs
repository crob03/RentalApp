using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IAuthenticationService
{
    event EventHandler<bool>? AuthenticationStateChanged;
    bool IsAuthenticated { get; }
    CurrentUserResponse? CurrentUser { get; }

    Task<AuthenticationResult> LoginAsync(string email, string password, bool rememberMe = false);
    Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    );
    Task LogoutAsync();
}
