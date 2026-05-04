using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class CreateReviewViewModelTests
{
    private readonly IReviewService _reviewService = Substitute.For<IReviewService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private CreateReviewViewModel CreateSut(int rentalId = 10)
    {
        var sut = new CreateReviewViewModel(_reviewService, _nav, _tokenState, _credentialStore);
        sut.ApplyQueryAttributes(new Dictionary<string, object> { { "rentalId", rentalId } });
        return sut;
    }

    private static CreateReviewResponse MakeReviewResponse() =>
        new(1, 10, 2, "Borrower User", 5, "Great!", DateTime.UtcNow);

    [Fact]
    public async Task SubmitReviewCommand_ValidInput_CallsServiceWithCorrectParameters()
    {
        _reviewService
            .CreateReviewAsync(Arg.Any<CreateReviewRequest>())
            .Returns(MakeReviewResponse());
        var sut = CreateSut(rentalId: 10);
        sut.Rating = 5;
        sut.Comment = "Great!";

        await sut.SubmitReviewCommand.ExecuteAsync(null);

        await _reviewService
            .Received(1)
            .CreateReviewAsync(
                Arg.Is<CreateReviewRequest>(r =>
                    r.RentalId == 10 && r.Rating == 5 && r.Comment == "Great!"
                )
            );
    }

    [Fact]
    public async Task SubmitReviewCommand_WhitespaceComment_PassesNullToService()
    {
        _reviewService
            .CreateReviewAsync(Arg.Any<CreateReviewRequest>())
            .Returns(MakeReviewResponse());
        var sut = CreateSut();
        sut.Rating = 3;
        sut.Comment = "   ";

        await sut.SubmitReviewCommand.ExecuteAsync(null);

        await _reviewService
            .Received(1)
            .CreateReviewAsync(Arg.Is<CreateReviewRequest>(r => r.Comment == null));
    }

    [Fact]
    public async Task SubmitReviewCommand_InvalidRating_SetsErrorAndDoesNotCallService()
    {
        var sut = CreateSut();
        sut.Rating = 0;

        await sut.SubmitReviewCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        await _reviewService.DidNotReceive().CreateReviewAsync(Arg.Any<CreateReviewRequest>());
    }

    [Fact]
    public async Task SubmitReviewCommand_ServiceThrows_SetsError()
    {
        _reviewService
            .CreateReviewAsync(Arg.Any<CreateReviewRequest>())
            .ThrowsAsync(new InvalidOperationException("Already reviewed"));
        var sut = CreateSut();
        sut.Rating = 4;

        await sut.SubmitReviewCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Already reviewed", sut.ErrorMessage);
    }

    [Fact]
    public async Task SubmitReviewCommand_Success_NavigatesBack()
    {
        _reviewService
            .CreateReviewAsync(Arg.Any<CreateReviewRequest>())
            .Returns(MakeReviewResponse());
        var sut = CreateSut();
        sut.Rating = 4;

        await sut.SubmitReviewCommand.ExecuteAsync(null);

        await _nav.Received(1).NavigateBackAsync();
    }
}
