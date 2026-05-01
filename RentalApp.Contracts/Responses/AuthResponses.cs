namespace RentalApp.Contracts.Responses;

public record LoginResponse(string Token, DateTime ExpiresAt, int UserId);

public record RegisterResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt
);
