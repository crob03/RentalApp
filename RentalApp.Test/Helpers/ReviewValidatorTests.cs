using RentalApp.Helpers;

namespace RentalApp.Test.Helpers;

public class ReviewValidatorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Validate_ValidRating_ReturnsNull(int rating)
    {
        var result = ReviewValidator.Validate(rating, null);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Validate_RatingOutOfRange_ReturnsError(int rating)
    {
        var result = ReviewValidator.Validate(rating, null);
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_CommentExact500Chars_ReturnsNull()
    {
        var comment = new string('a', 500);
        var result = ReviewValidator.Validate(3, comment);
        Assert.Null(result);
    }

    [Fact]
    public void Validate_Comment501Chars_ReturnsError()
    {
        var comment = new string('a', 501);
        var result = ReviewValidator.Validate(3, comment);
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_NullComment_ReturnsNull()
    {
        var result = ReviewValidator.Validate(3, null);
        Assert.Null(result);
    }
}
