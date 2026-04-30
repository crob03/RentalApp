using Microsoft.Maui.Devices.Sensors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class LocationServiceTests
{
    private readonly IGeolocation _geolocation = Substitute.For<IGeolocation>();

    private LocationService CreateSut() => new(_geolocation);

    [Fact]
    public async Task GetCurrentLocationAsync_Success_ReturnsLatLon()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Location(55.9533, -3.1883));
        var sut = CreateSut();

        var (lat, lon) = await sut.GetCurrentLocationAsync();

        Assert.Equal(55.9533, lat, precision: 4);
        Assert.Equal(-3.1883, lon, precision: 4);
    }

    [Fact]
    public async Task GetCurrentLocationAsync_NullLocation_ThrowsInvalidOperationException()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .Returns((Location?)null);
        var sut = CreateSut();

        var act = () => sut.GetCurrentLocationAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task GetCurrentLocationAsync_PermissionDenied_ThrowsInvalidOperationException()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PermissionException("denied"));
        var sut = CreateSut();

        var act = () => sut.GetCurrentLocationAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task GetCurrentLocationAsync_FeatureNotEnabled_ThrowsInvalidOperationException()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FeatureNotEnabledException());
        var sut = CreateSut();

        var act = () => sut.GetCurrentLocationAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }
}
