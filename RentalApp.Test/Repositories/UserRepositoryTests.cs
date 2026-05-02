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

    // ── GetByEmailAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var sut = CreateSut();
        await sut.CreateAsync("Bob", "Jones", "bob@example.com", "hash", "salt");

        var user = await sut.GetByEmailAsync("bob@example.com");

        Assert.NotNull(user);
        Assert.Equal("bob@example.com", user!.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        var sut = CreateSut();

        var user = await sut.GetByEmailAsync("nobody@example.com");

        Assert.Null(user);
    }
}
