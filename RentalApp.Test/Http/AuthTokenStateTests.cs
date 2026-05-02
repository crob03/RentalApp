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

    [Fact]
    public void AuthenticationStateChanged_WhenTokenSet_RaisesWithTrue()
    {
        var sut = new AuthTokenState();
        bool? raised = null;
        sut.AuthenticationStateChanged += (_, v) => raised = v;

        sut.CurrentToken = "eyJ...";

        Assert.True(raised);
    }

    [Fact]
    public void AuthenticationStateChanged_WhenTokenCleared_RaisesWithFalse()
    {
        var sut = new AuthTokenState { CurrentToken = "eyJ..." };
        bool? raised = null;
        sut.AuthenticationStateChanged += (_, v) => raised = v;

        sut.ClearToken();

        Assert.False(raised);
    }

    [Fact]
    public void ClearToken_SetsCurrentTokenToNull()
    {
        var sut = new AuthTokenState { CurrentToken = "eyJ..." };

        sut.ClearToken();

        Assert.Null(sut.CurrentToken);
    }

    [Fact]
    public void HasSession_AfterClearToken_ReturnsFalse()
    {
        var sut = new AuthTokenState { CurrentToken = "eyJ..." };

        sut.ClearToken();

        Assert.False(sut.HasSession);
    }
}
