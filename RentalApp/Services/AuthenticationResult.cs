using RentalApp.Database.Models;

namespace RentalApp.Services;

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public User? User { get; set; }

    public static AuthenticationResult Success(User user) =>
        new AuthenticationResult { IsSuccess = true, User = user };

    public static AuthenticationResult Failure(string errorMessage) =>
        new AuthenticationResult { IsSuccess = false, ErrorMessage = errorMessage };
}
