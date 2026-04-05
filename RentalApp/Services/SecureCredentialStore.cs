namespace RentalApp.Services;

public class SecureCredentialStore : ICredentialStore
{
    private const string EmailKey = "auth_email";
    private const string PasswordKey = "auth_password";

    public async Task SaveAsync(string email, string password)
    {
        await SecureStorage.Default.SetAsync(EmailKey, email);
        await SecureStorage.Default.SetAsync(PasswordKey, password);
    }

    public async Task<(string Email, string Password)?> GetAsync()
    {
        var email = await SecureStorage.Default.GetAsync(EmailKey);
        var password = await SecureStorage.Default.GetAsync(PasswordKey);

        if (email is null || password is null)
            return null;

        return (email, password);
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(PasswordKey);
        return Task.CompletedTask;
    }
}
