using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Items;
using RentalApp.Services.Navigation;
using RentalApp.Services.Rentals;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ItemDetailsViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IRentalService _rentalService = Substitute.For<IRentalService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private ItemDetailsViewModel CreateSut() =>
        new(_itemService, _authService, _rentalService, _nav, _tokenState, _credentialStore);

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
        _itemService.GetItemAsync(1).Returns(item);
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.Equal("Drill", sut.CurrentItem!.Title);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadItemCommand_ServiceThrows_SetsError()
    {
        _itemService.GetItemAsync(1).ThrowsAsync(new InvalidOperationException("Not found"));
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
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.GetCurrentUserAsync().Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.True(sut.IsOwner);
    }

    [Fact]
    public async Task LoadItemCommand_CurrentUserIsNotOwner_SetsIsOwnerFalse()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.False(sut.IsOwner);
    }

    // ── ToggleEditCommand ──────────────────────────────────────────────

    [Fact]
    public async Task ToggleEditCommand_PopulatesEditFields()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.GetCurrentUserAsync().Returns(MakeUser(5));
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
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.GetCurrentUserAsync().Returns(MakeUser(5));
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
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5), updated);
        _authService.GetCurrentUserAsync().Returns(MakeUser(5));
        _itemService
            .UpdateItemAsync(1, Arg.Any<UpdateItemRequest>())
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
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.GetCurrentUserAsync().Returns(MakeUser(5));
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
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.GetCurrentUserAsync().Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);

        sut.CancelEditCommand.Execute(null);

        Assert.False(sut.IsEditing);
        await _itemService
            .DidNotReceive()
            .UpdateItemAsync(Arg.Any<int>(), Arg.Any<UpdateItemRequest>());
    }

    // ── Rental form state ─────────────────────────────────────────────

    [Fact]
    public async Task ShowRentalForm_IsTrue_WhenNonOwnerAndItemAvailable()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2)); // IsAvailable = true
        _authService.GetCurrentUserAsync().Returns(MakeUser(99)); // different user
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.True(sut.ShowRentalForm);
    }

    [Fact]
    public async Task ShowRentalForm_IsFalse_WhenUserIsOwner()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.GetCurrentUserAsync().Returns(MakeUser(5)); // same user = owner
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.False(sut.ShowRentalForm);
    }

    [Fact]
    public async Task ShowRentalForm_IsFalse_WhenItemIsUnavailable()
    {
        var unavailableItem = new ItemDetailResponse(
            1,
            "Drill",
            "desc",
            10.0,
            1,
            "Tools",
            2,
            "Owner",
            null,
            55.9,
            -3.2,
            false, // IsAvailable = false
            null,
            0,
            DateTime.UtcNow,
            []
        );
        _itemService.GetItemAsync(1).Returns(unavailableItem);
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.False(sut.ShowRentalForm);
    }

    [Fact]
    public async Task TotalPrice_IsInitialisedOnLoad()
    {
        // Default dates are today (start) and today+1 (end) → 1 day × £10 = £10
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.Equal(10.0, sut.TotalPrice);
    }

    [Fact]
    public async Task TotalPrice_RecalculatesWhenDatesChange()
    {
        // MakeItem returns DailyRate = 10.0
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);

        sut.RentalStartDate = DateTime.Today.AddDays(1);
        sut.RentalEndDate = DateTime.Today.AddDays(4); // 3 days × £10 = £30

        Assert.Equal(30.0, sut.TotalPrice);
    }

    [Fact]
    public async Task RentalSuccessMessage_ClearsWhenStartDateChanges()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        _rentalService
            .CreateRentalAsync(Arg.Any<CreateRentalRequest>())
            .Returns(
                new CreateRentalResponse(
                    1,
                    1,
                    "Drill",
                    99,
                    "Jane",
                    2,
                    "Owner",
                    DateTime.Today.AddDays(1),
                    DateTime.Today.AddDays(3),
                    "Requested",
                    20.0,
                    DateTime.UtcNow
                )
            );
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.RentalStartDate = DateTime.Today.AddDays(1);
        sut.RentalEndDate = DateTime.Today.AddDays(3);
        await sut.RequestRentalCommand.ExecuteAsync(null);
        Assert.Equal("Rental requested successfully!", sut.RentalSuccessMessage);

        sut.RentalStartDate = DateTime.Today.AddDays(2); // change date

        Assert.Null(sut.RentalSuccessMessage);
    }

    // ── RequestRentalCommand ──────────────────────────────────────────

    [Fact]
    public async Task RequestRentalCommand_ValidDates_CallsServiceAndSetsSuccessMessage()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        _rentalService
            .CreateRentalAsync(Arg.Any<CreateRentalRequest>())
            .Returns(
                new CreateRentalResponse(
                    1,
                    1,
                    "Drill",
                    99,
                    "Jane",
                    2,
                    "Owner",
                    DateTime.Today.AddDays(1),
                    DateTime.Today.AddDays(3),
                    "Requested",
                    20.0,
                    DateTime.UtcNow
                )
            );
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.RentalStartDate = DateTime.Today.AddDays(1);
        sut.RentalEndDate = DateTime.Today.AddDays(3);

        await sut.RequestRentalCommand.ExecuteAsync(null);

        Assert.Equal("Rental requested successfully!", sut.RentalSuccessMessage);
        Assert.False(sut.HasError);
    }

    [Fact]
    public async Task RequestRentalCommand_ValidDates_PassesCorrectItemIdToService()
    {
        _itemService.GetItemAsync(7).Returns(MakeItem(7, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        _rentalService
            .CreateRentalAsync(Arg.Any<CreateRentalRequest>())
            .Returns(
                new CreateRentalResponse(
                    1,
                    7,
                    "Drill",
                    99,
                    "Jane",
                    2,
                    "Owner",
                    DateTime.Today.AddDays(1),
                    DateTime.Today.AddDays(3),
                    "Requested",
                    20.0,
                    DateTime.UtcNow
                )
            );
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 7 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.RentalStartDate = DateTime.Today.AddDays(1);
        sut.RentalEndDate = DateTime.Today.AddDays(3);

        await sut.RequestRentalCommand.ExecuteAsync(null);

        await _rentalService
            .Received(1)
            .CreateRentalAsync(Arg.Is<CreateRentalRequest>(r => r.ItemId == 7));
    }

    [Fact]
    public async Task RequestRentalCommand_StartDateInPast_SetsErrorAndDoesNotCallService()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.RentalStartDate = DateTime.Today.AddDays(-1);
        sut.RentalEndDate = DateTime.Today.AddDays(2);

        await sut.RequestRentalCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Start date cannot be in the past", sut.ErrorMessage);
        await _rentalService.DidNotReceive().CreateRentalAsync(Arg.Any<CreateRentalRequest>());
    }

    [Fact]
    public async Task RequestRentalCommand_EndDateNotAfterStartDate_SetsErrorAndDoesNotCallService()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.RentalStartDate = DateTime.Today.AddDays(2);
        sut.RentalEndDate = DateTime.Today.AddDays(1); // end before start

        await sut.RequestRentalCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("End date must be after start date", sut.ErrorMessage);
        await _rentalService.DidNotReceive().CreateRentalAsync(Arg.Any<CreateRentalRequest>());
    }

    [Fact]
    public async Task RequestRentalCommand_ServiceThrows_SetsError()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 2));
        _authService.GetCurrentUserAsync().Returns(MakeUser(99));
        _rentalService
            .CreateRentalAsync(Arg.Any<CreateRentalRequest>())
            .ThrowsAsync(new InvalidOperationException("Server error"));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.RentalStartDate = DateTime.Today.AddDays(1);
        sut.RentalEndDate = DateTime.Today.AddDays(3);

        await sut.RequestRentalCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Server error", sut.ErrorMessage);
    }
}
