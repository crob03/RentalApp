using RentalApp.Http;

namespace RentalApp.Test.Http;

public class AuthTokenStateTests
{
    [Fact]
    public void HasSession_WhenTokenIsNull_ReturnsFalse()
    {
        var sut = new AuthTokenState();
        Assert.False(sut.HasSession);
    }

    [Fact]
    public void HasSession_WhenTokenIsSet_ReturnsTrue()
    {
        var sut = new AuthTokenState { CurrentToken = "eyJ..." };
        Assert.True(sut.HasSession);
    }

    [Fact]
    public void HasSession_AfterClearingToken_ReturnsFalse()
    {
        var sut = new AuthTokenState { CurrentToken = "eyJ..." };
        sut.CurrentToken = null;
        Assert.False(sut.HasSession);
    }
}
