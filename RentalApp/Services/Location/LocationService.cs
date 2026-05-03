using Microsoft.Maui.Devices.Sensors;

namespace RentalApp.Services.Location;

/// <summary>
/// MAUI implementation of <see cref="ILocationService"/>. Wraps platform-specific
/// <see cref="PermissionException"/> and <see cref="FeatureNotEnabledException"/> into
/// <see cref="InvalidOperationException"/> so callers have a single error type to handle.
/// </summary>
public class LocationService : ILocationService
{
    private readonly IGeolocation _geolocation;

    public LocationService(IGeolocation geolocation)
    {
        _geolocation = geolocation;
    }

    /// <inheritdoc/>
    public async Task<(double Lat, double Lon)> GetCurrentLocationAsync()
    {
        try
        {
            var location = await _geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium)
            );

            if (location == null)
                throw new InvalidOperationException("Unable to determine current location.");

            return (location.Latitude, location.Longitude);
        }
        catch (PermissionException)
        {
            throw new InvalidOperationException("Location permission denied.");
        }
        catch (FeatureNotEnabledException)
        {
            throw new InvalidOperationException("Location services are disabled on this device.");
        }
    }
}
