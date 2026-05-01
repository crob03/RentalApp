using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ItemDetailsViewModelTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();

    private ItemDetailsViewModel CreateSut() => new(_api, _authService);

    private static ItemDetailResponse MakeItem(int id, int ownerId, string title = "Drill") =>
        new(
            id,
            title,
            "desc",
            10.0,
            1,
            "Tools",
            ownerId,
            "Owner",
            null,
            55.9,
            -3.2,
            true,
            null,
            0,
            DateTime.UtcNow,
            []
        );

    private static CurrentUserResponse MakeUser(int id) =>
        new(id, "jane@example.com", "Jane", "Doe", null, 0, 0, DateTime.UtcNow);

    // ── LoadItemCommand ────────────────────────────────────────────────

    [Fact]
    public async Task LoadItemCommand_Success_PopulatesCurrentItem()
    {
        var item = MakeItem(1, 2);
        _api.GetItemAsync(1).Returns(item);
        _authService.CurrentUser.Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.Equal("Drill", sut.CurrentItem!.Title);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadItemCommand_ServiceThrows_SetsError()
    {
        _api.GetItemAsync(1).ThrowsAsync(new InvalidOperationException("Not found"));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.False(sut.IsBusy);
    }

    // ── IsOwner ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadItemCommand_CurrentUserIsOwner_SetsIsOwnerTrue()
    {
        _api.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.True(sut.IsOwner);
    }

    [Fact]
    public async Task LoadItemCommand_CurrentUserIsNotOwner_SetsIsOwnerFalse()
    {
        _api.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.False(sut.IsOwner);
    }

    // ── ToggleEditCommand ──────────────────────────────────────────────

    [Fact]
    public async Task ToggleEditCommand_PopulatesEditFields()
    {
        _api.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);

        sut.ToggleEditCommand.Execute(null);

        Assert.True(sut.IsEditing);
        Assert.Equal("Drill", sut.EditTitle);
        Assert.Equal("10", sut.EditDailyRate);
    }

    [Fact]
    public async Task ToggleEditCommand_WhenAlreadyEditing_ExitsEditMode()
    {
        _api.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);

        sut.ToggleEditCommand.Execute(null);

        Assert.False(sut.IsEditing);
    }

    // ── SaveChangesCommand ─────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesCommand_ValidInput_UpdatesCurrentItemAndExitsEditMode()
    {
        var updated = MakeItem(1, ownerId: 5, title: "Updated Drill");
        _api.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5), updated);
        _authService.CurrentUser.Returns(MakeUser(5));
        _api.UpdateItemAsync(1, Arg.Any<UpdateItemRequest>())
            .Returns(new UpdateItemResponse(1, "Updated Drill", "desc", 10.0, true));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);
        sut.EditTitle = "Updated Drill";

        await sut.SaveChangesCommand.ExecuteAsync(null);

        Assert.Equal("Updated Drill", sut.CurrentItem!.Title);
        Assert.False(sut.IsEditing);
    }

    [Fact]
    public async Task SaveChangesCommand_InvalidDailyRate_SetsError()
    {
        _api.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);
        sut.EditDailyRate = "not-a-number";

        await sut.SaveChangesCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.True(sut.IsEditing);
    }

    // ── CancelEditCommand ──────────────────────────────────────────────

    [Fact]
    public async Task CancelEditCommand_ExitsEditModeWithoutSaving()
    {
        _api.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);

        sut.CancelEditCommand.Execute(null);

        Assert.False(sut.IsEditing);
        await _api
            .DidNotReceive()
            .UpdateItemAsync(Arg.Any<int>(), Arg.Any<UpdateItemRequest>());
    }
}
