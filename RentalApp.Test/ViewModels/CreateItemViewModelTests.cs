using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class CreateItemViewModelTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private CreateItemViewModel CreateSut() => new(_api, _locationService, _nav);

    private static CategoryResponse MakeCategory(int id = 1, string name = "Tools", string slug = "tools") =>
        new(id, name, slug, 5);

    // ── LoadCategoriesCommand ──────────────────────────────────────────

    [Fact]
    public async Task LoadCategoriesCommand_Success_PopulatesCategories()
    {
        var cats = new List<CategoryResponse>
        {
            MakeCategory(1, "Tools", "tools"),
            MakeCategory(2, "Electronics", "electronics"),
        };
        _api.GetCategoriesAsync().Returns(new CategoriesResponse(cats));
        var sut = CreateSut();

        await sut.LoadCategoriesCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Categories.Count);
    }

    // ── CreateItemCommand ──────────────────────────────────────────────

    [Fact]
    public async Task CreateItemCommand_ValidInput_CallsServiceAndNavigatesBack()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.CreateItemAsync(Arg.Any<CreateItemRequest>())
            .Returns(
                new CreateItemResponse(
                    1,
                    "My Drill",
                    "desc",
                    10.0,
                    1,
                    "Tools",
                    1,
                    "Owner",
                    55.9533,
                    -3.1883,
                    true,
                    DateTime.UtcNow
                )
            );
        var sut = CreateSut();
        sut.ItemTitle = "My Drill";
        sut.Description = "desc";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = MakeCategory(1, "Tools", "tools");

        await sut.CreateItemCommand.ExecuteAsync(null);

        await _api.Received(1)
            .CreateItemAsync(
                Arg.Is<CreateItemRequest>(r =>
                    r.Title == "My Drill"
                    && r.Description == "desc"
                    && r.DailyRate == 10.0
                    && r.CategoryId == 1
                    && r.Latitude == 55.9533
                    && r.Longitude == -3.1883
                )
            );
        await _nav.Received(1).NavigateBackAsync();
    }

    [Fact]
    public async Task CreateItemCommand_NoCategory_SetsError()
    {
        var sut = CreateSut();
        sut.ItemTitle = "My Drill";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = null;

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        await _api
            .DidNotReceive()
            .CreateItemAsync(Arg.Any<CreateItemRequest>());
    }

    [Fact]
    public async Task CreateItemCommand_InvalidRate_SetsError()
    {
        var sut = CreateSut();
        sut.ItemTitle = "My Drill";
        sut.DailyRate = "not-a-number";
        sut.SelectedCategory = MakeCategory();

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
        sut.ItemTitle = "My Drill";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = MakeCategory();

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Contains("Location unavailable", sut.ErrorMessage);
    }

    [Fact]
    public async Task CreateItemCommand_ServiceValidationFails_SetsError()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.CreateItemAsync(Arg.Any<CreateItemRequest>())
            .ThrowsAsync(new ArgumentException("Title must be between 5 and 100 characters."));
        var sut = CreateSut();
        sut.ItemTitle = "Hi";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = MakeCategory();

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.False(sut.IsBusy);
    }
}
