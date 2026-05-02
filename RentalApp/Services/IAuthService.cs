using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<CurrentUserResponse> GetCurrentUserAsync();
    Task<UserProfileResponse> GetUserProfileAsync(int userId);
}
