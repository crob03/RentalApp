namespace RentalApp.Models;

internal sealed record AuthToken(string Token, DateTime ExpiresAt, int UserId);
