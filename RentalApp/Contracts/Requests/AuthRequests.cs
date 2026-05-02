namespace RentalApp.Contracts.Requests;

/// <summary>Request payload for authenticating an existing user.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Request payload for registering a new user account.</summary>
public record RegisterRequest(string FirstName, string LastName, string Email, string Password);
