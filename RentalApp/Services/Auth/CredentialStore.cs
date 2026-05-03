namespace RentalApp.Services.Auth;

/// <summary>
/// Implements <see cref="ICredentialStore"/> using MAUI's <see cref="SecureStorage"/> to
/// encrypt credentials on the device keychain/keystore.
/// </summary>
public class CredentialStore : ICredentialStore
{
    private const string EmailKey = "auth_email";
    private const string PasswordKey = "auth_password";

    /// <inheritdoc/>
    public async Task SaveAsync(string email, string password)
    {
        await SecureStorage.Default.SetAsync(EmailKey, email);
        await SecureStorage.Default.SetAsync(PasswordKey, password);
    }

    /// <inheritdoc/>
    public async Task<(string Email, string Password)?> GetAsync()
    {
        var email = await SecureStorage.Default.GetAsync(EmailKey);
        var password = await SecureStorage.Default.GetAsync(PasswordKey);

        if (email is null || password is null)
            return null;

        return (email, password);
    }

    /// <inheritdoc/>
    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(PasswordKey);
        return Task.CompletedTask;
    }
}
