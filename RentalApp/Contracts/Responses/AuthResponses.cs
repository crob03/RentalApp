namespace RentalApp.Contracts.Responses;

/// <summary>Response returned after a successful login.</summary>
/// <param name="Token">Bearer token to include in subsequent authenticated requests.</param>
public record LoginResponse(string Token, DateTime ExpiresAt, int UserId);

/// <summary>Response returned after a successful user registration.</summary>
public record RegisterResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt
);
