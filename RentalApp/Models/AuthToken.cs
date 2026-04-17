namespace RentalApp.Models;

/// <summary>
/// Deserialization target for the <c>POST /auth/token</c> response.
/// Used internally by <see cref="RentalApp.Services.RemoteApiService"/> only.
/// </summary>
/// <param name="Token">Bearer token to attach to subsequent authenticated requests.</param>
/// <param name="ExpiresAt">Timestamp at which the token expires.</param>
/// <param name="UserId">Identifier of the authenticated user.</param>
internal sealed record AuthToken(string Token, DateTime ExpiresAt, int UserId);
