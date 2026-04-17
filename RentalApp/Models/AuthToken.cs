namespace RentalApp.Models;

public sealed record AuthToken(string Token, DateTime ExpiresAt, int UserId);
