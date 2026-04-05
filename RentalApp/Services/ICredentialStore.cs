namespace RentalApp.Services;

public interface ICredentialStore
{
    Task SaveAsync(string email, string password);
    Task<(string Email, string Password)?> GetAsync();
    Task ClearAsync();
}
