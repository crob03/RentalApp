// RentalApp.Test/Helpers/ItemValidatorTests.cs
using RentalApp.Helpers;

namespace RentalApp.Test.Helpers;

public class ItemValidatorTests
{
    // ── ValidateCreate ─────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_MissingTitle_ReturnsError(string? title)
    {
        var result = ItemValidator.ValidateCreate(title, null, "10.0", 1);
        Assert.Equal("Title is required", result);
    }

    [Fact]
    public void ValidateCreate_TitleTooShort_ReturnsError()
    {
        var result = ItemValidator.ValidateCreate("Hi", null, "10.0", 1);
        Assert.Equal("Title must be at least 5 characters", result);
    }

    [Fact]
    public void ValidateCreate_TitleTooLong_ReturnsError()
    {
        var result = ItemValidator.ValidateCreate(new string('A', 101), null, "10.0", 1);
        Assert.Equal("Title must be 100 characters or fewer", result);
    }

    [Fact]
    public void ValidateCreate_DescriptionTooLong_ReturnsError()
    {
        var result = ItemValidator.ValidateCreate("Valid Title", new string('X', 1001), "10.0", 1);
        Assert.Equal("Description must be 1000 characters or fewer", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_MissingRate_ReturnsError(string? rate)
    {
        var result = ItemValidator.ValidateCreate("Valid Title", null, rate, 1);
        Assert.Equal("Daily rate is required", result);
    }

    [Fact]
    public void ValidateCreate_NonNumericRate_ReturnsError()
    {
        var result = ItemValidator.ValidateCreate("Valid Title", null, "foo", 1);
        Assert.Equal("Daily rate must be a valid number", result);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void ValidateCreate_RateNotPositive_ReturnsError(string rate)
    {
        var result = ItemValidator.ValidateCreate("Valid Title", null, rate, 1);
        Assert.Equal("Daily rate must be greater than zero", result);
    }

    [Fact]
    public void ValidateCreate_RateTooHigh_ReturnsError()
    {
        var result = ItemValidator.ValidateCreate("Valid Title", null, "1001", 1);
        Assert.Equal("Daily rate cannot exceed £1000", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateCreate_InvalidCategoryId_ReturnsError(int categoryId)
    {
        var result = ItemValidator.ValidateCreate("Valid Title", null, "10", categoryId);
        Assert.Equal("A category must be selected", result);
    }

    [Fact]
    public void ValidateCreate_AllValid_ReturnsNull()
    {
        var result = ItemValidator.ValidateCreate("Valid Title", "A description", "10.0", 1);
        Assert.Null(result);
    }

    // ── ValidateUpdate ─────────────────────────────────────────────────

    [Fact]
    public void ValidateUpdate_AllNullInputs_ReturnsNull()
    {
        var result = ItemValidator.ValidateUpdate(null, null, null);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_EmptyRate_ReturnsError(string rate)
    {
        var result = ItemValidator.ValidateUpdate(null, null, rate);
        Assert.Equal("Daily rate is required", result);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void ValidateUpdate_RateNotPositive_ReturnsError(string rate)
    {
        var result = ItemValidator.ValidateUpdate(null, null, rate);
        Assert.Equal("Daily rate must be greater than zero", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_EmptyTitle_ReturnsError(string title)
    {
        var result = ItemValidator.ValidateUpdate(title, null, null);
        Assert.Equal("Title is required", result);
    }

    [Fact]
    public void ValidateUpdate_TitleTooShort_ReturnsError()
    {
        var result = ItemValidator.ValidateUpdate("Hi", null, null);
        Assert.Equal("Title must be at least 5 characters", result);
    }

    [Fact]
    public void ValidateUpdate_TitleTooLong_ReturnsError()
    {
        var result = ItemValidator.ValidateUpdate(new string('A', 101), null, null);
        Assert.Equal("Title must be 100 characters or fewer", result);
    }

    [Fact]
    public void ValidateUpdate_DescriptionTooLong_ReturnsError()
    {
        var result = ItemValidator.ValidateUpdate(null, new string('X', 1001), null);
        Assert.Equal("Description must be 1000 characters or fewer", result);
    }

    [Fact]
    public void ValidateUpdate_NonNumericRate_ReturnsError()
    {
        var result = ItemValidator.ValidateUpdate(null, null, "foo");
        Assert.Equal("Daily rate must be a valid number", result);
    }

    [Fact]
    public void ValidateUpdate_RateOutOfRange_ReturnsError()
    {
        var result = ItemValidator.ValidateUpdate(null, null, "1001");
        Assert.Equal("Daily rate cannot exceed £1000", result);
    }

    [Fact]
    public void ValidateUpdate_AllNonNull_Valid_ReturnsNull()
    {
        var result = ItemValidator.ValidateUpdate("Valid Title", "description", "50");
        Assert.Null(result);
    }
}
