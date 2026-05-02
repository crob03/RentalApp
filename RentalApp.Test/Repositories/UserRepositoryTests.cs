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
        Assert.NotNull(user.CreatedAt);
        Assert.True(user.CreatedAt.Value > DateTime.UtcNow.AddSeconds(-5));
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

    [Fact]
    public async Task GetByEmailAsync_MixedCaseInput_ReturnsUser()
    {
        var sut = CreateSut();
        await sut.CreateAsync("Bob", "Jones", "bob@example.com", "hash", "salt");

        var user = await sut.GetByEmailAsync("BOB@EXAMPLE.COM");

        Assert.NotNull(user);
        Assert.Equal("bob@example.com", user!.Email);
    }

    [Fact]
    public async Task CreateAsync_MixedCaseEmail_StoresLowercase()
    {
        var sut = CreateSut();

        var user = await sut.CreateAsync("Dave", "Hill", "Dave@Example.COM", "hash", "salt");

        Assert.Equal("dave@example.com", user.Email);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsUser()
    {
        var sut = CreateSut();
        var created = await sut.CreateAsync("Carol", "White", "carol@example.com", "hash", "salt");

        var user = await sut.GetByIdAsync(created.Id);

        Assert.NotNull(user);
        Assert.Equal(created.Id, user!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = CreateSut();

        var user = await sut.GetByIdAsync(999);

        Assert.Null(user);
    }
}
