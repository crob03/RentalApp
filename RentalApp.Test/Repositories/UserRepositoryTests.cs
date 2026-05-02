using RentalApp.Database.Repositories;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Repositories;

public class UserRepositoryTests
    : IClassFixture<DatabaseFixture<UserRepositoryTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<UserRepositoryTests> _fixture;

    public UserRepositoryTests(DatabaseFixture<UserRepositoryTests> fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private UserRepository CreateSut() => new(_fixture.ContextFactory);

    // ── CreateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidInput_PersistsAndReturnsUserWithId()
    {
        var sut = CreateSut();

        var user = await sut.CreateAsync("Alice", "Smith", "alice@example.com", "hash", "salt");

        Assert.True(user.Id > 0);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("Alice", user.FirstName);
    }
}
