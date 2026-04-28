using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class CreateItemViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private CreateItemViewModel CreateSut() => new(_itemService, _locationService, _nav);

    // ── LoadCategoriesCommand ──────────────────────────────────────────

    [Fact]
    public async Task LoadCategoriesCommand_Success_PopulatesCategories()
    {
        var cats = new List<Category>
        {
            new(1, "Tools", "tools", 5),
            new(2, "Electronics", "electronics", 3),
        };
        _itemService.GetCategoriesAsync().Returns(cats);
        var sut = CreateSut();

        await sut.LoadCategoriesCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Categories.Count);
    }

    // ── CreateItemCommand ──────────────────────────────────────────────

    [Fact]
    public async Task CreateItemCommand_ValidInput_CallsServiceAndNavigatesBack()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .CreateItemAsync("My Drill", "desc", 10.0, 1, 55.9533, -3.1883)
            .Returns(
                new Item(
                    1,
                    "My Drill",
                    "desc",
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
                )
            );
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.Description = "desc";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        await _itemService
            .Received(1)
            .CreateItemAsync("My Drill", "desc", 10.0, 1, 55.9533, -3.1883);
        await _nav.Received(1).NavigateBackAsync();
    }

    [Fact]
    public async Task CreateItemCommand_NoCategory_SetsError()
    {
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = null;

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        await _itemService
            .DidNotReceive()
            .CreateItemAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<double>(),
                Arg.Any<double>()
            );
    }

    [Fact]
    public async Task CreateItemCommand_InvalidRate_SetsError()
    {
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.DailyRate = "not-a-number";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
    }

    [Fact]
    public async Task CreateItemCommand_LocationFails_SetsError()
    {
        _locationService
            .GetCurrentLocationAsync()
            .ThrowsAsync(
                new InvalidOperationException(
                    "Location unavailable. Please enable GPS and try again."
                )
            );
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Contains("Location unavailable", sut.ErrorMessage);
    }

    [Fact]
    public async Task CreateItemCommand_ServiceValidationFails_SetsError()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .CreateItemAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<double>(),
                Arg.Any<double>()
            )
            .ThrowsAsync(new ArgumentException("Title must be between 5 and 100 characters."));
        var sut = CreateSut();
        sut.Title2 = "Hi";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.False(sut.IsBusy);
    }
}
