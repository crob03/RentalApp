using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class ItemServiceTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();

    private ItemService CreateSut() => new(_api);

    private static Item MakeItem(int id) =>
        new(
            id,
            $"Item {id}",
            null,
            10.0,
            1,
            "Tools",
            1,
            "Owner",
            null,
            null,
            null,
            null,
            true,
            null,
            null,
            null,
            null
        );

    // ── CreateItemAsync — validation ───────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("Hi")]
    [InlineData("    ")]
    public async Task CreateItemAsync_InvalidTitle_ThrowsArgumentException(string title)
    {
        var sut = CreateSut();

        var act = () => sut.CreateItemAsync(title, null, 10.0, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task CreateItemAsync_TitleTooLong_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var longTitle = new string('A', 101);

        var act = () => sut.CreateItemAsync(longTitle, null, 10.0, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task CreateItemAsync_DescriptionTooLong_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var longDesc = new string('X', 1001);

        var act = () => sut.CreateItemAsync("Valid Title", longDesc, 10.0, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(1001.0)]
    public async Task CreateItemAsync_InvalidDailyRate_ThrowsArgumentException(double rate)
    {
        var sut = CreateSut();

        var act = () => sut.CreateItemAsync("Valid Title", null, rate, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateItemAsync_InvalidCategoryId_ThrowsArgumentException(int categoryId)
    {
        var sut = CreateSut();

        var act = () => sut.CreateItemAsync("Valid Title", null, 10.0, categoryId, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task CreateItemAsync_ValidInput_DelegatesToApi()
    {
        var expected = MakeItem(1) with { Title = "Valid Title" };
        _api.CreateItemAsync("Valid Title", null, 10.0, 1, 55.9, -3.2).Returns(expected);
        var sut = CreateSut();

        var result = await sut.CreateItemAsync("Valid Title", null, 10.0, 1, 55.9, -3.2);

        Assert.Equal(expected, result);
        await _api.Received(1).CreateItemAsync("Valid Title", null, 10.0, 1, 55.9, -3.2);
    }

    // ── UpdateItemAsync — validation ───────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_TitleTooShort_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = () => sut.UpdateItemAsync(1, "Hi", null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task UpdateItemAsync_NullTitle_DoesNotThrow()
    {
        var expected = MakeItem(1);
        _api.UpdateItemAsync(1, null, null, null, null).Returns(expected);
        var sut = CreateSut();

        var result = await sut.UpdateItemAsync(1, null, null, null, null);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UpdateItemAsync_RateOutOfRange_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = () => sut.UpdateItemAsync(1, null, null, 1001.0, null);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    // ── GetItemsAsync — delegates ──────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_DelegatesToApi()
    {
        _api.GetItemsAsync(null, null, 1, 20).Returns(new List<Item>());
        var sut = CreateSut();

        await sut.GetItemsAsync();

        await _api.Received(1).GetItemsAsync(null, null, 1, 20);
    }

    // ── GetCategoriesAsync — delegates ────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_DelegatesToApi()
    {
        _api.GetCategoriesAsync().Returns(new List<Category>());
        var sut = CreateSut();

        await sut.GetCategoriesAsync();

        await _api.Received(1).GetCategoriesAsync();
    }
}
